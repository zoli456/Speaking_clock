using System.Globalization;
using Speaking_Clock;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Vortice.WinForms;
using Color = System.Drawing.Color;
using FontStyle = Vortice.DirectWrite.FontStyle;
using TextAntialiasMode = Vortice.Direct2D1.TextAntialiasMode;
using Timer = System.Windows.Forms.Timer;

public class CalendarWidget : RenderForm, IDisposable
{
    private readonly Timer _timer;
    private readonly ID2D1SolidColorBrush brush;

    private readonly DateTime currentDate;
    private readonly ID2D1SolidColorBrush currentDayBrush;
    private readonly IDWriteTextFormat dayTextFormat;
    private readonly ID2D1SolidColorBrush highlightDayBrush;
    private readonly int LastNamedayIndex;

    private readonly Button nextButton;
    private readonly Button prevButton;
    private readonly ID2D1HwndRenderTarget renderTarget;
    private readonly ID2D1SolidColorBrush sundayBrush;
    private readonly IDWriteTextFormat textFormat;
    private readonly IDWriteFactory writeFactory;
    private DateTime displayDate;
    private Point dragOffset;
    private bool isDragging;
    private ID2D1SolidColorBrush textBrush;

    public CalendarWidget(int startX = 100, int startY = 100)
    {
        // Form settings
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        FormBorderStyle = FormBorderStyle.None;
        Width = 410;
        Height = 300;
        Opacity = 0.9f;
        AllowTransparency = true;
        BackColor = Color.FromArgb(0, 0, 0, 0);
        TransparencyKey = BackColor;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(startX, startY);

        currentDate = DateTime.Now;
        displayDate = currentDate;
        LastNamedayIndex = displayDate.Day;

        // Direct2D/DirectWrite initialization
        var factory = D2D1.D2D1CreateFactory<ID2D1Factory>();
        var renderProperties = new HwndRenderTargetProperties
        {
            Hwnd = Handle,
            PixelSize = new SizeI(Width, Height),
            PresentOptions = PresentOptions.None
        };
        renderTarget = factory.CreateHwndRenderTarget(new RenderTargetProperties(), renderProperties);

        brush = renderTarget.CreateSolidColorBrush(new Color4(1, 1, 1)); // White
        currentDayBrush = renderTarget.CreateSolidColorBrush(new Color4(1, 0.5f, 0)); // Orange for today
        sundayBrush = renderTarget.CreateSolidColorBrush(new Color4(1, 0, 0)); // Red for Sundays
        highlightDayBrush = renderTarget.CreateSolidColorBrush(new Color4(1, 0, 0)); // Red for highlighted days

        writeFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
        textFormat = writeFactory.CreateTextFormat("Arial", FontWeight.Bold, FontStyle.Normal, FontStretch.Normal,
            HeaderFontSize);
        dayTextFormat = writeFactory.CreateTextFormat("Arial", FontWeight.Bold, FontStyle.Normal, FontStretch.Normal,
            DayFontSize);

        // Navigation buttons
        nextButton = new Button { Text = ">", Width = 40, Height = 25, BackColor = BackColor };
        prevButton = new Button { Text = "<", Width = 40, Height = 25, BackColor = BackColor };
        Controls.Add(nextButton);
        Controls.Add(prevButton);

        nextButton.Click += (s, e) =>
        {
            displayDate = displayDate.AddMonths(1);
            Invalidate();
        };
        prevButton.Click += (s, e) =>
        {
            displayDate = displayDate.AddMonths(-1);
            Invalidate();
        };

        MouseDown += (s, e) => StartDrag(e);
        MouseMove += (s, e) => DragForm(e);
        MouseUp += (s, e) => StopDrag();
        Resize += (s, e) => UpdateButtonPositions();
        UpdateButtonPositions();
        var currentyear = DateTime.Now.Year;
        SetHighlightedDays(new List<DateTime>
        {
            new(currentyear, 1, 1),
            new(currentyear, 3, 15),
            EasterSunday(currentyear).AddDays(-2),
            EasterSunday(currentyear).AddDays(1),
            new(currentyear, 5, 1),
            EasterSunday(currentyear).AddDays(50),
            new(currentyear, 8, 20),
            new(currentyear, 10, 23),
            new(currentyear, 11, 1),
            new(currentyear, 12, 25),
            new(currentyear, 12, 26)
        });

        // Set up a timer for updating clock
        _timer = new Timer { Interval = 60000 };
        _timer.Tick += (s, e) =>
        {
            if (LastNamedayIndex != DateTime.Now.Day)
            {
                displayDate = DateTime.Now;
                Invalidate();
            }

            ;
        };
        _timer.Start();
        Show();
    }

    public HashSet<DateTime> HighlightedDays { get; private set; } = new();

    public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;
    public int CellSpacing { get; set; } = 50;
    public int CellHeight { get; set; } = 30;
    public int HeaderFontSize { get; set; } = 24;
    public int DayFontSize { get; set; } = 20;
    public int DayToNumberSpacing { get; set; } = -10;
    public int DayHeaderToTopSpacing { get; set; } = 50;

    public new void Dispose()
    {
        brush.Dispose();
        currentDayBrush.Dispose();
        sundayBrush.Dispose();
        highlightDayBrush.Dispose();
        renderTarget.Dispose();
        textFormat.Dispose();
        dayTextFormat.Dispose();
        writeFactory.Dispose();
        base.Dispose();
    }

    public void SetHighlightedDays(IEnumerable<DateTime> days)
    {
        HighlightedDays = new HashSet<DateTime>(days);
        Invalidate();
    }

    private void UpdateButtonPositions()
    {
        var buttonY = 20;
        var buttonMargin = 20;

        prevButton.Left = buttonMargin;
        prevButton.Top = buttonY;

        nextButton.Left = Width - nextButton.Width - buttonMargin;
        nextButton.Top = buttonY;
    }

    private void StartDrag(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
        {
            isDragging = true;
            dragOffset = new Point(e.X, e.Y);
        }
    }

    private void DragForm(MouseEventArgs e)
    {
        if (isDragging)
        {
            var newLocation = PointToScreen(new Point(e.X, e.Y));
            newLocation.Offset(-dragOffset.X, -dragOffset.Y);
            Location = newLocation;
        }
    }

    private void StopDrag()
    {
        isDragging = false;
        Beallitasok.WidgetSection["Naptár_X"].IntValue = Left;
        Beallitasok.WidgetSection["Naptár_Y"].IntValue = Top;
        Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.Path}\\{Beallitasok.SetttingsFileName}");
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Render();
    }

    private void Render()
    {
        renderTarget.TextAntialiasMode = TextAntialiasMode.Cleartype;
        renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

        renderTarget.BeginDraw();
        renderTarget.Clear(new Color4(0, 0, 0, 0));
        DrawCalendar();
        renderTarget.EndDraw();
    }

    private void DrawCalendar()
    {
        var header = displayDate.ToString("yyyy MMMM");
        using (var textLayout = writeFactory.CreateTextLayout(header, textFormat, float.MaxValue, float.MaxValue))
        {
            var textWidth = textLayout.Metrics.Width;
            var centerX = (Width - textWidth) / 2;
            renderTarget.DrawText(header, textFormat, new Rect(centerX, 20, centerX + textWidth, 60), brush);
        }

        var cultureInfo = CultureInfo.CurrentCulture;
        var dayNames = cultureInfo.DateTimeFormat.AbbreviatedDayNames;
        var startIndex = (int)FirstDayOfWeek;

        for (var i = 0; i < 7; i++)
        {
            var dayIndex = (startIndex + i) % 7;
            float x = i * CellSpacing + 50;
            var isLastDayOfWeek = i == 6;

            renderTarget.DrawText(
                dayNames[dayIndex],
                dayTextFormat,
                new Rect(x, DayHeaderToTopSpacing, x + CellSpacing, DayHeaderToTopSpacing + CellHeight),
                isLastDayOfWeek ? sundayBrush : brush
            );
        }

        var firstDay = new DateTime(displayDate.Year, displayDate.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(displayDate.Year, displayDate.Month);
        var startDay = ((int)firstDay.DayOfWeek - (int)FirstDayOfWeek + 7) % 7;

        for (var i = 0; i < daysInMonth; i++)
        {
            var row = (startDay + i) / 7;
            var col = (startDay + i) % 7;

            float x = col * CellSpacing + 50;
            float y = row * CellHeight + DayHeaderToTopSpacing + CellHeight + DayToNumberSpacing;

            var dateToDraw = new DateTime(displayDate.Year, displayDate.Month, i + 1);

            // Determine the correct brush to use

            if (dateToDraw.Date == currentDate.Date)
                // If it's the current day, use the currentDayBrush
                textBrush = currentDayBrush;
            else if (HighlightedDays.Contains(dateToDraw))
                // If the day is highlighted, use the highlightDayBrush
                textBrush = highlightDayBrush;
            else if (col == 6)
                // If it's a Sunday, use the sundayBrush
                textBrush = sundayBrush;
            else
                // Default to the regular brush
                textBrush = brush;

            // Draw the day number
            renderTarget.DrawText(
                (i + 1).ToString(),
                dayTextFormat,
                new Rect(x, y, x + CellSpacing, y + CellHeight),
                textBrush
            );
        }
    }
    /// <summary>
    /// Calculate the date of Easter Sunday for a given year.
    /// </summary>
    /// <param name="year">The year to calculate Easter Sunday for.</param>
    /// <returns></returns>
    public static DateTime EasterSunday(int year)
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
}