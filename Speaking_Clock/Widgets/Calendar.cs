using System.Globalization;
using System.Numerics;
using Speaking_Clock;
using Speaking_clock.Widgets;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Vortice.WinForms;
using D2DColor = Vortice.Mathematics.Color4;
using DrawingColor = System.Drawing.Color;
using FontStyle = Vortice.DirectWrite.FontStyle;
using TextAntialiasMode = Vortice.Direct2D1.TextAntialiasMode;
using Timer = System.Windows.Forms.Timer;

public class CalendarWidget : RenderForm, IDisposable
{
    private readonly ID2D1SolidColorBrush _buttonBrush;
    private readonly ID2D1SolidColorBrush _buttonHoverBrush;
    private readonly ID2D1SolidColorBrush _buttonSymbolBrush;
    private readonly IDWriteTextFormat _buttonSymbolTextFormat;
    private readonly ID2D1SolidColorBrush _currentDayBrush;

    private readonly Timer _dailyUpdateTimer;
    private readonly IDWriteTextFormat _dayTextFormat;

    private readonly ID2D1SolidColorBrush _defaultTextBrush;
    private readonly ID2D1SolidColorBrush _highlightDayBrush;

    private readonly IDWriteTextFormat _monthYearTextFormat;
    private readonly ID2D1HwndRenderTarget _renderTarget;
    private readonly ID2D1SolidColorBrush _sundayBrush;

    private DateTime _displayDate;
    private Point _dragOffset;
    private HashSet<DateTime> _highlightedDays = [];

    // Dragging
    private bool _isDragging;
    private bool _isMouseOverNextButton;
    private bool _isMouseOverPrevButton;
    private RectangleF _nextButtonRect;

    private RectangleF _prevButtonRect;
    private int _yearOfLastHolidayCalculation;
    public DayOfWeek FirstDayOfWeek = DayOfWeek.Monday;

    public CalendarWidget(int startX = 100, int startY = 100)
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        FormBorderStyle = FormBorderStyle.None;
        Width = CalendarLook.DefaultWidth;
        Height = CalendarLook.DefaultHeight;
        Opacity = CalendarLook.WidgetOpacity;
        AllowTransparency = true;
        BackColor = DrawingColor.FromArgb(0, 0, 0, 0);
        TransparencyKey = BackColor;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(startX, startY);

        _displayDate = DateTime.Now;
        _yearOfLastHolidayCalculation = _displayDate.Year;

        var renderProperties = new HwndRenderTargetProperties
        {
            Hwnd = Handle,
            PixelSize = new SizeI(Width, Height),
            PresentOptions = PresentOptions.None
        };
        _renderTarget =
            GraphicsFactories.D2DFactory.CreateHwndRenderTarget(new RenderTargetProperties(), renderProperties);

        _defaultTextBrush = _renderTarget.CreateSolidColorBrush(new D2DColor(1, 1, 1, 0.9f)); // White
        _currentDayBrush = _renderTarget.CreateSolidColorBrush(new D2DColor(1, 0.6f, 0, 0.9f)); // Orange
        _sundayBrush = _renderTarget.CreateSolidColorBrush(new D2DColor(1, 0.2f, 0.2f, 0.9f)); // Red
        _highlightDayBrush = _renderTarget.CreateSolidColorBrush(new D2DColor(1, 0.2f, 0.2f, 0.9f)); // Red

        _buttonBrush = _renderTarget.CreateSolidColorBrush(new D2DColor(0.3f, 0.3f, 0.3f, 0.7f)); // Dark Gray
        _buttonHoverBrush = _renderTarget.CreateSolidColorBrush(new D2DColor(0.5f, 0.5f, 0.5f, 0.8f)); // Lighter Gray
        _buttonSymbolBrush = _renderTarget.CreateSolidColorBrush(new D2DColor(1f, 1f, 1f, 0.9f)); // White symbol

        _monthYearTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat("Arial", FontWeight.Bold,
            FontStyle.Normal,
            FontStretch.Normal, CalendarLook.HeaderFontSize);
        _dayTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat("Arial", FontWeight.Normal, FontStyle.Normal,
            FontStretch.Normal, CalendarLook.DayFontSize);
        _buttonSymbolTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat("Arial", FontWeight.Bold,
            FontStyle.Normal,
            FontStretch.Normal, CalendarLook.ButtonSymbolFontSize);
        _buttonSymbolTextFormat.TextAlignment = TextAlignment.Center;
        _buttonSymbolTextFormat.ParagraphAlignment = ParagraphAlignment.Center;


        MouseDown += OnWidgetMouseDown;
        MouseMove += OnWidgetMouseMove;
        MouseUp += OnWidgetMouseUp;
        MouseWheel += OnWidgetMouseWheel;
        MouseLeave += OnWidgetMouseLeave;
        Resize += OnWidgetResize;

        UpdateButtonRects();
        SetHighlightedDays(GetHolidaysForYear(_displayDate.Year));

        _dailyUpdateTimer = new Timer();
        _dailyUpdateTimer.Tick += (s, e) =>
        {
            Invalidate();
            UpdateDailyTimerInterval();
        };
        UpdateDailyTimerInterval();
        _dailyUpdateTimer.Start();

        Show();
    }


    public new void Dispose()
    {
        _dailyUpdateTimer?.Dispose();

        _defaultTextBrush?.Dispose();
        _currentDayBrush?.Dispose();
        _sundayBrush?.Dispose();
        _highlightDayBrush?.Dispose();
        _buttonBrush?.Dispose();
        _buttonHoverBrush?.Dispose();
        _buttonSymbolBrush?.Dispose();

        _renderTarget?.Dispose();

        _monthYearTextFormat?.Dispose();
        _dayTextFormat?.Dispose();
        _buttonSymbolTextFormat?.Dispose();

        base.Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void OnWidgetResize(object sender, EventArgs e)
    {
        if (_renderTarget != null)
            _renderTarget.Resize(new SizeI(ClientSize.Width, ClientSize.Height));
        UpdateButtonRects(); // Recalculate button positions based on new size
        Invalidate(); // Redraw the widget
    }

    private void NavigateMonth(int monthOffset)
    {
        _displayDate = _displayDate.AddMonths(monthOffset);
        if (_displayDate.Year != _yearOfLastHolidayCalculation)
        {
            SetHighlightedDays(GetHolidaysForYear(_displayDate.Year));
            _yearOfLastHolidayCalculation = _displayDate.Year;
        }

        Invalidate();
    }

    private void UpdateDailyTimerInterval()
    {
        var now = DateTime.Now;
        var nextMidnight = now.Date.AddDays(1);
        var timeUntilMidnight = nextMidnight - now;
        _dailyUpdateTimer.Interval = Math.Max(1000, (int)timeUntilMidnight.TotalMilliseconds);
    }

    public void SetHighlightedDays(IEnumerable<DateTime> days)
    {
        _highlightedDays = new HashSet<DateTime>(days.Select(d => d.Date));
        Invalidate();
    }

    private List<DateTime> GetHolidaysForYear(int year)
    {
        var holidays = new List<DateTime>
        {
            new(year, 1, 1), new(year, 3, 15), new(year, 5, 1),
            new(year, 8, 20), new(year, 10, 23), new(year, 11, 1),
            new(year, 12, 25), new(year, 12, 26)
        };
        var easterSunday = CalculateEasterSunday(year);
        holidays.Add(easterSunday.AddDays(-2)); // Good Friday
        holidays.Add(easterSunday.AddDays(1)); // Easter Monday
        holidays.Add(easterSunday.AddDays(50)); // Pentecost Monday
        return holidays;
    }

    private void UpdateButtonRects()
    {
        float prevButtonCenterX = CalendarLook.ButtonHorizontalMargin + CalendarLook.ButtonRadius;
        float buttonCenterY = CalendarLook.ButtonTopY + CalendarLook.ButtonRadius;

        _prevButtonRect = new RectangleF(
            prevButtonCenterX - CalendarLook.ButtonRadius,
            buttonCenterY - CalendarLook.ButtonRadius,
            CalendarLook.ButtonRadius * 2,
            CalendarLook.ButtonRadius * 2
        );

        float nextButtonCenterX = Width - CalendarLook.ButtonHorizontalMargin - CalendarLook.ButtonRadius;
        _nextButtonRect = new RectangleF(
            nextButtonCenterX - CalendarLook.ButtonRadius,
            buttonCenterY - CalendarLook.ButtonRadius,
            CalendarLook.ButtonRadius * 2,
            CalendarLook.ButtonRadius * 2
        );
    }

    private void OnWidgetMouseWheel(object sender, MouseEventArgs e)
    {
        if (e.Delta > 0) NavigateMonth(-1);
        else if (e.Delta < 0) NavigateMonth(1);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        RenderCalendar();
    }

    private void RenderCalendar()
    {
        _renderTarget.TextAntialiasMode = TextAntialiasMode.Cleartype;
        _renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

        _renderTarget.BeginDraw();
        _renderTarget.Clear(new D2DColor(0, 0, 0, 0));

        DrawNavigationButtons();
        DrawMonthYearHeader();
        DrawDayNamesHeader();
        DrawDaysOfMonth();

        _renderTarget.EndDraw();
    }

    private void DrawNavigationButtons()
    {
        // Previous Button
        var prevEllipse = new Ellipse(
            new Vector2(_prevButtonRect.Left + CalendarLook.ButtonRadius,
                _prevButtonRect.Top + CalendarLook.ButtonRadius),
            CalendarLook.ButtonRadius,
            CalendarLook.ButtonRadius
        );
        _renderTarget.FillEllipse(prevEllipse, _isMouseOverPrevButton ? _buttonHoverBrush : _buttonBrush);
        // _renderTarget.DrawEllipse(prevEllipse, _defaultTextBrush, 0.5f); //  thin border
        _renderTarget.DrawText(CalendarLook.PrevButtonSymbol, _buttonSymbolTextFormat, (Rect)_prevButtonRect,
            _buttonSymbolBrush);

        // Next Button
        var nextEllipse = new Ellipse(
            new Vector2(_nextButtonRect.Left + CalendarLook.ButtonRadius,
                _nextButtonRect.Top + CalendarLook.ButtonRadius),
            CalendarLook.ButtonRadius,
            CalendarLook.ButtonRadius
        );
        _renderTarget.FillEllipse(nextEllipse, _isMouseOverNextButton ? _buttonHoverBrush : _buttonBrush);
        // _renderTarget.DrawEllipse(nextEllipse, _defaultTextBrush, 0.5f); //thin border
        _renderTarget.DrawText(CalendarLook.NextButtonSymbol, _buttonSymbolTextFormat, (Rect)_nextButtonRect,
            _buttonSymbolBrush);
    }


    private void DrawMonthYearHeader()
    {
        var culture = CultureInfo.CurrentUICulture;
        var headerText = _displayDate.ToString("yyyy MMMM", culture);
        using (var textLayout = GraphicsFactories.DWriteFactory.CreateTextLayout(headerText, _monthYearTextFormat,
                   Width,
                   CalendarLook.MonthYearHeaderTextHeight))
        {
            textLayout.TextAlignment = TextAlignment.Center; // Center align the header text
            var textMetrics = textLayout.Metrics;
            var textRect = new Rect(
                0, // Start from the left edge of the widget for centered text
                CalendarLook.MonthYearHeaderTextTop,
                Width, // Span the full width for centering
                CalendarLook.MonthYearHeaderTextTop + textMetrics.Height // Use actual text height
            );
            _renderTarget.DrawTextLayout(new Vector2(textRect.X, textRect.Y), textLayout, _defaultTextBrush);
        }
    }

    private void DrawDayNamesHeader()
    {
        var culture = CultureInfo.CurrentUICulture;
        var dayNames = culture.DateTimeFormat.AbbreviatedDayNames;
        var startDayIndex = (int)FirstDayOfWeek;

        for (var i = 0; i < 7; i++)
        {
            var currentDayNameIndex = (startDayIndex + i) % 7;
            var dayNameToDraw = dayNames[currentDayNameIndex].ToUpper(); // Consistent look
            var dayOfWeekEnum = (DayOfWeek)currentDayNameIndex;

            float x = i * CalendarLook.DayCellWidth + CalendarLook.DayColumnStartX;
            var textRect = new Rect(x, CalendarLook.DayNamesRowTop, x + CalendarLook.DayCellWidth,
                CalendarLook.DayNamesRowTop + CalendarLook.DayCellHeight);

            var brush = dayOfWeekEnum == DayOfWeek.Sunday ? _sundayBrush : _defaultTextBrush;
            using (var textLayout = GraphicsFactories.DWriteFactory.CreateTextLayout(dayNameToDraw, _dayTextFormat,
                       CalendarLook.DayCellWidth, CalendarLook.DayCellHeight))
            {
                textLayout.TextAlignment = TextAlignment.Center;
                textLayout.ParagraphAlignment = ParagraphAlignment.Center;
                _renderTarget.DrawTextLayout(new Vector2(textRect.Left, textRect.Top), textLayout, brush);
            }
        }
    }

    private void DrawDaysOfMonth()
    {
        var firstDayCurrentMonth = new DateTime(_displayDate.Year, _displayDate.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(_displayDate.Year, _displayDate.Month);
        var startColumnOffset = ((int)firstDayCurrentMonth.DayOfWeek - (int)FirstDayOfWeek + 7) % 7;

        for (var dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth++)
        {
            var dateToDraw = new DateTime(_displayDate.Year, _displayDate.Month, dayOfMonth);
            var dayIndexInGrid = startColumnOffset + (dayOfMonth - 1);
            var row = dayIndexInGrid / 7;
            var col = dayIndexInGrid % 7;

            float x = col * CalendarLook.DayCellWidth + CalendarLook.DayColumnStartX;
            float y = row * CalendarLook.DayCellHeight + CalendarLook.NumbersGridYStart;
            var textRect = new Rect(x, y, x + CalendarLook.DayCellWidth, y + CalendarLook.DayCellHeight);

            ID2D1SolidColorBrush selectedBrush;
            if (dateToDraw.Date == DateTime.Today) selectedBrush = _currentDayBrush;
            else if (_highlightedDays.Contains(dateToDraw.Date)) selectedBrush = _highlightDayBrush;
            else if (dateToDraw.DayOfWeek == DayOfWeek.Sunday) selectedBrush = _sundayBrush;
            else selectedBrush = _defaultTextBrush;

            using (var textLayout = GraphicsFactories.DWriteFactory.CreateTextLayout(dayOfMonth.ToString(),
                       _dayTextFormat,
                       CalendarLook.DayCellWidth, CalendarLook.DayCellHeight))
            {
                textLayout.TextAlignment = TextAlignment.Center;
                textLayout.ParagraphAlignment = ParagraphAlignment.Center;
                _renderTarget.DrawTextLayout(new Vector2(textRect.Left, textRect.Top), textLayout, selectedBrush);
            }
        }
    }

    public static DateTime CalculateEasterSunday(int year)
    {
        var day = 0;
        var month = 0;
        var g = year % 19;
        var c = year / 100;
        var h = (c - c / 4 - (8 * c + 13) / 25 + 19 * g + 15) % 30;
        var i = h - h / 28 * (1 - h / 28 * (29 / (h + 1)) * ((21 - g) / 11));
        day = i - (year + year / 4 + i + 2 - c + c / 4) % 7 + 28;
        month = 3;
        if (day > 31)
        {
            month++;
            day -= 31;
        }

        return new DateTime(year, month, day);
    }

    private static class CalendarLook
    {
        public const int DefaultWidth = 380;
        public const int DefaultHeight = 280;
        public const float WidgetOpacity = 1;

        // Round Button Styling
        public const int ButtonRadius = 12;
        public const int ButtonTopY = 10;
        public const int ButtonHorizontalMargin = 10;
        public const string PrevButtonSymbol = "<";
        public const string NextButtonSymbol = ">";
        public const int ButtonSymbolFontSize = 16;

        public const int MonthYearHeaderTextTop = 10;
        public const int MonthYearHeaderTextHeight = 30;

        public const int DayNamesRowTop = MonthYearHeaderTextTop + MonthYearHeaderTextHeight + 5; // Y for day names
        public const int DayCellWidth = (DefaultWidth - DayColumnStartX * 2 + 10) / 7;
        public const int DayCellHeight = 28;

        public const int NumbersGridYStart = DayNamesRowTop + DayCellHeight + 5;
        public const int HeaderFontSize = 24;
        public const int DayFontSize = 20;
        public const int DayColumnStartX = 15;
    }

    #region Dragging Logic & Mouse Interaction for Buttons

    private void OnWidgetMouseDown(object sender, MouseEventArgs e)
    {
        var mousePoint = new PointF(e.X, e.Y);

        if (_prevButtonRect.Contains(mousePoint))
        {
            NavigateMonth(-1);
            return;
        }

        if (_nextButtonRect.Contains(mousePoint))
        {
            NavigateMonth(1);
            return;
        }

        if (e.Button == MouseButtons.Left && Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
        {
            _isDragging = true;
            _dragOffset = new Point(e.X, e.Y);
        }
    }

    private new void OnWidgetMouseMove(object sender, MouseEventArgs e)
    {
        var mousePoint = new PointF(e.X, e.Y);
        var needsRedraw = false;

        var oldHoverPrev = _isMouseOverPrevButton;
        _isMouseOverPrevButton = _prevButtonRect.Contains(mousePoint);
        if (oldHoverPrev != _isMouseOverPrevButton) needsRedraw = true;

        var oldHoverNext = _isMouseOverNextButton;
        _isMouseOverNextButton = _nextButtonRect.Contains(mousePoint);
        if (oldHoverNext != _isMouseOverNextButton) needsRedraw = true;

        if (_isDragging)
        {
            var newLocation = PointToScreen(new Point(e.X, e.Y));
            newLocation.Offset(-_dragOffset.X, -_dragOffset.Y);
            Location = newLocation;
        }
        else if (needsRedraw)
        {
            Invalidate(); // Redraw for hover effect
        }
    }

    private void OnWidgetMouseLeave(object sender, EventArgs e)
    {
        var needsRedraw = false;
        if (_isMouseOverPrevButton)
        {
            _isMouseOverPrevButton = false;
            needsRedraw = true;
        }

        if (_isMouseOverNextButton)
        {
            _isMouseOverNextButton = false;
            needsRedraw = true;
        }

        if (needsRedraw) Invalidate();
    }

    private void OnWidgetMouseUp(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            Beallitasok.WidgetSection["Naptár_X"].IntValue = Left;
            Beallitasok.WidgetSection["Naptár_Y"].IntValue = Top;
            Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
        }
    }

    #endregion
}