using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Xml;
using Speaking_Clock;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.WinForms;
using Timer = System.Timers.Timer;
using static Vanara.PInvoke.User32;
using AlphaMode = Vortice.DCommon.AlphaMode;
using Color = System.Drawing.Color;
using RectangleF = System.Drawing.RectangleF;
using Size = System.Drawing.Size;

namespace Speaking_clock.Widgets;

// --- Configuration Class ---
public class RssReaderSettings
{
    public int FontSize { get; set; } = 18;
    public int TitleFontSize { get; set; } = 20;
    public int ItemSpacing { get; set; } = 3;
    public int HeaderVerticalPadding { get; set; } = 5;
    public int MaxDisplayItems { get; set; } = 10;
    public int WindowWidth { get; set; } = 400;

    public double WindowOpacity { get; set; } = 0.9;

    //public bool AllowDragging { get; set; } = true;
    public string HeaderColor { get; set; } = "blue"; // Name or Hex
    public string TextColor { get; set; } = "white";
    public string ContentBackgroundColor { get; set; } = "#D0202020"; // Default: ~81% alpha, dark grey/black
    public string HoverItemBackgroundColor { get; set; } = "#404040";
    public string FontName { get; set; } = "Arial";
    public int UpdateIntervalMs { get; set; } = 600000;
    public int CornerRadius { get; set; } = 15;
    public int ScrollbarWidth { get; set; } = 8;
    public string ScrollbarColor { get; set; } = "gray";
    public int InitialX { get; set; } = 100;
    public int InitialY { get; set; } = 100;
}

// --- RSS Item Data Structure ---
public record RssFeedItem(string Title, string Link, string FullDescription);

internal class RssReader : RenderForm
{
    private const int ToolTipInitialDelay = 200;
    private const int ToolTipAutoPopDelay = 7000;
    private const int ToolTipReshowDelay = 200;
    private static readonly HttpClient httpClient = new();

    // --- Factories & Configuration ---
    private readonly string _feedUrl;
    private readonly int _formInstanceIndex;
    private readonly RssReaderSettings _settings;

    // --- Other ---
    private readonly ToolTip _toolTip = new();
    private readonly Timer _updateTimer;
    private readonly string _widgetTitle;
    private ID2D1SolidColorBrush _contentBackgroundBrush;
    private ID2D1SolidColorBrush _defaultTextBrush;
    private Point _dragStartPoint;
    private ID2D1Brush _headerBrush;
    private float _headerHeight;
    private ID2D1SolidColorBrush _hoverBackgroundBrush;
    private int _hoveredIndex = -1;
    private bool _isDragging;

    // --- Calculated Dimensions ---
    private float _itemHeight;

    // --- UI State ---
    private List<RssFeedItem> _items = new();
    private IDWriteTextFormat _listItemTextFormat;

    // --- Direct2D Resources ---
    private ID2D1HwndRenderTarget _renderTarget;
    private ID2D1SolidColorBrush _scrollbarBrush;
    private int _scrollOffset;
    private IDWriteTextFormat _titleTextFormat;
    private float _totalActualContentHeight; // Height required for all items
    private int _visibleWindowHeight;

    public RssReader(int formInstanceIndex, string feedUrl, string title, RssReaderSettings settings)
    {
        _formInstanceIndex = formInstanceIndex;
        _feedUrl = feedUrl;
        _widgetTitle = title;
        _settings = settings ?? new RssReaderSettings(); // Ensure settings is not null

        InitializeUiProperties();
        CalculateDimensions();
        InitializeToolTip();
        RegisterEventHandlers();

        // Initial RSS Load
        _ = LoadRssFeedAsync(); // Fire and forget with error handling within

        _updateTimer = new Timer(_settings.UpdateIntervalMs);
        _updateTimer.Elapsed += async (s, e) => await OnUpdateTimerElapsedAsync();
        _updateTimer.Start();

        Show();
    }


    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= (int)WindowStylesEx.WS_EX_TOOLWINDOW; // Hide from Alt+Tab
            //cp.ExStyle |= (int)WindowStylesEx.WS_EX_LAYERED;
            return cp;
        }
    }

    private void InitializeUiProperties()
    {
        Text = _widgetTitle;
        FormBorderStyle = FormBorderStyle.None;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        Opacity = _settings.WindowOpacity;
        ClientSize = new Size(_settings.WindowWidth, _visibleWindowHeight);
        ShowInTaskbar = false;
        TopMost = false;
        DoubleBuffered = false;

        StartPosition = FormStartPosition.Manual;
        Location = new Point(_settings.InitialX, _settings.InitialY);
        Region = CreateRoundedRectangleRegion(ClientSize.Width, ClientSize.Height, _settings.CornerRadius);
    }

    private void CalculateDimensions()
    {
        _itemHeight = _settings.FontSize + _settings.ItemSpacing * 2; // More vertical padding for items
        _headerHeight = _settings.TitleFontSize + _settings.HeaderVerticalPadding * 2;
        _visibleWindowHeight = (int)(_headerHeight + _settings.MaxDisplayItems * _itemHeight);
        if (ClientSize.Height != _visibleWindowHeight || ClientSize.Width != _settings.WindowWidth)
        {
            ClientSize = new Size(_settings.WindowWidth, _visibleWindowHeight);
            Region = CreateRoundedRectangleRegion(ClientSize.Width, ClientSize.Height, _settings.CornerRadius);
        }
    }

    private void InitializeToolTip()
    {
        _toolTip.AutoPopDelay = ToolTipAutoPopDelay;
        _toolTip.InitialDelay = ToolTipInitialDelay;
        _toolTip.ReshowDelay = ToolTipReshowDelay;
        _toolTip.ShowAlways = true;
        _toolTip.UseFading = true;
        _toolTip.UseAnimation = true;
    }

    private void RegisterEventHandlers()
    {
        MouseDown += OnWidgetMouseDown;
        MouseMove += OnWidgetMouseMove;
        MouseUp += OnWidgetMouseUp;
        MouseLeave += OnWidgetMouseLeave;
        MouseWheel += OnWidgetMouseWheel;
        MouseClick += OnWidgetMouseClick;
        SizeChanged += OnWidgetSizeChanged;
    }

    private void OnWidgetSizeChanged(object sender, EventArgs e)
    {
        // Re-apply rounded region if size changes
        if (ClientSize.Width > 0 && ClientSize.Height > 0)
        {
            Region = CreateRoundedRectangleRegion(ClientSize.Width, ClientSize.Height, _settings.CornerRadius);
            CreateDirect2DResources();
            Invalidate();
        }
    }

    private void OnWidgetMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && e.Y <= _headerHeight && Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
        {
            _isDragging = true;
            _dragStartPoint = e.Location;
        }
    }

    private void OnWidgetMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            Left += e.X - _dragStartPoint.X;
            Top += e.Y - _dragStartPoint.Y;
        }
        else
        {
            UpdateHoveredItem(e.Location);
        }
    }

    private void UpdateHoveredItem(Point mouseLocation)
    {
        var newHoveredIndex = -1; // Relative to visible items
        var tooltipText = string.Empty;

        if (mouseLocation.Y > _headerHeight && mouseLocation.Y < _visibleWindowHeight)
        {
            // Calculate index relative to the displayed item area
            var relativeY = mouseLocation.Y - _headerHeight;
            var potentialVisibleIndex = (int)(relativeY / _itemHeight);

            if (potentialVisibleIndex >= 0 && potentialVisibleIndex < _settings.MaxDisplayItems)
            {
                var actualItemIndex = _scrollOffset + potentialVisibleIndex;
                if (actualItemIndex < _items.Count)
                {
                    newHoveredIndex = potentialVisibleIndex;
                    tooltipText = _items[actualItemIndex].Title;
                }
            }
        }

        if (_hoveredIndex != newHoveredIndex)
        {
            _hoveredIndex = newHoveredIndex;
            // Update tooltip only if necessary
            if (_toolTip.GetToolTip(this) != tooltipText) _toolTip.SetToolTip(this, tooltipText);
            Invalidate(); // Redraw for hover effect
        }
    }


    private void OnWidgetMouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _isDragging)
        {
            _isDragging = false;
            SaveWidgetPosition(_formInstanceIndex, Left, Top);
        }
    }

    private void OnWidgetMouseLeave(object sender, EventArgs e)
    {
        if (_hoveredIndex != -1)
        {
            _hoveredIndex = -1;
            _toolTip.SetToolTip(this, string.Empty);
            Invalidate();
        }
    }

    private void OnWidgetMouseWheel(object sender, MouseEventArgs e)
    {
        if (!_items.Any() || _items.Count <= _settings.MaxDisplayItems) return;

        var scrollDirection = e.Delta > 0 ? -1 : 1; // One item at a time for smoother feel
        var newScrollOffset = _scrollOffset + scrollDirection;

        // Clamp scrollOffset
        _scrollOffset = Math.Max(0, Math.Min(newScrollOffset, _items.Count - _settings.MaxDisplayItems));

        UpdateHoveredItem(PointToClient(MousePosition)); // Re-evaluate hover after scroll
        Invalidate();
    }

    private void OnWidgetMouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && !_isDragging && _hoveredIndex != -1)
        {
            var actualItemIndex = _scrollOffset + _hoveredIndex;
            if (actualItemIndex >= 0 && actualItemIndex < _items.Count)
                try
                {
                    var link = _items[actualItemIndex].Link;
                    if (!string.IsNullOrWhiteSpace(link) && Uri.IsWellFormedUriString(link, UriKind.Absolute))
                        Process.Start(new ProcessStartInfo { FileName = link, UseShellExecute = true });
                    else
                        Debug.WriteLine($"Invalid or empty link: {link}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error opening link: {ex.Message}");
                }
        }
        else if (e.Button == MouseButtons.Right && e.Y <= _headerHeight)
        {
            // Show context menu 
        }
    }

    internal async Task OnUpdateTimerElapsedAsync()
    {
        Debug.WriteLine($"RSS Feed Update Triggered for {_widgetTitle}");
        await LoadRssFeedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            _toolTip?.Dispose();

            // Dispose Direct2D resources
            _defaultTextBrush?.Dispose();
            _headerBrush?.Dispose();
            _hoverBackgroundBrush?.Dispose();
            _contentBackgroundBrush?.Dispose();
            _scrollbarBrush?.Dispose();
            _renderTarget?.Dispose(); // Must be disposed before factory if it holds resources from it

            // Dispose DirectWrite resources
            _titleTextFormat?.Dispose();
            _listItemTextFormat?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (DesignMode) return;
        CreateDirect2DResources();
    }

    // Create Direct2D and DirectWrite resources
    private void CreateDirect2DResources()
    {
        if (GraphicsFactories.D2DFactory == null || Handle == IntPtr.Zero || ClientSize.Width == 0 ||
            ClientSize.Height == 0)
            return;

        // Dispose existing resources first
        _renderTarget?.Dispose();
        _defaultTextBrush?.Dispose();
        _headerBrush?.Dispose();
        _hoverBackgroundBrush?.Dispose();
        _scrollbarBrush?.Dispose();
        _titleTextFormat?.Dispose();
        _listItemTextFormat?.Dispose();


        var rtProps = new RenderTargetProperties
        {
            Type = RenderTargetType.Hardware,
            PixelFormat = new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
            DpiX = 96f,
            DpiY = 96f,
            Usage = RenderTargetUsage.GdiCompatible,
            MinLevel = FeatureLevel.Level_9
        };

        var hwndProps = new HwndRenderTargetProperties
        {
            Hwnd = Handle,
            PixelSize = new SizeI(Width, Height),
            PresentOptions = PresentOptions.None
        };

        _renderTarget = GraphicsFactories.D2DFactory
            .CreateHwndRenderTarget(rtProps, hwndProps);

        // --- Brushes ---
        _defaultTextBrush = _renderTarget.CreateSolidColorBrush(ParseColor(_settings.TextColor, Colors.White));
        _headerBrush = CreateHeaderBrush();
        _contentBackgroundBrush =
            _renderTarget.CreateSolidColorBrush(ParseColor(_settings.ContentBackgroundColor,
                new Color4(0.2f, 0.2f, 0.2f, 0.8f)));
        _hoverBackgroundBrush =
            _renderTarget.CreateSolidColorBrush(ParseColor(_settings.HoverItemBackgroundColor, Colors.DarkGray));
        _scrollbarBrush = _renderTarget.CreateSolidColorBrush(ParseColor(_settings.ScrollbarColor, Colors.DimGray));


        // --- Text Formats ---
        _titleTextFormat =
            GraphicsFactories.DWriteFactory.CreateTextFormat(_settings.FontName, _settings.TitleFontSize);
        _titleTextFormat.TextAlignment = TextAlignment.Center;
        _titleTextFormat.ParagraphAlignment = ParagraphAlignment.Center;

        _listItemTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(_settings.FontName, _settings.FontSize);
        _listItemTextFormat.TextAlignment = TextAlignment.Leading; // Left-aligned
        _listItemTextFormat.ParagraphAlignment = ParagraphAlignment.Center; // Vertically centered in its layout box
        _listItemTextFormat.WordWrapping = WordWrapping.NoWrap;
        // Setup trimming
        var trimmingSign = GraphicsFactories.DWriteFactory.CreateEllipsisTrimmingSign(_listItemTextFormat);
        _listItemTextFormat.SetTrimming(
            new Trimming { Granularity = TrimmingGranularity.Character, Delimiter = 0, DelimiterCount = 0 },
            trimmingSign);
    }

    private ID2D1Brush CreateHeaderBrush()
    {
        return _renderTarget.CreateSolidColorBrush(ParseColor(_settings.HeaderColor, Colors.CornflowerBlue));
    }


    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_renderTarget == null || _items == null || DesignMode)
            return;

        _renderTarget.BeginDraw();
        _renderTarget.Clear(Colors.Transparent);

        DrawWidgetHeader(); // Draws the header

        // --- Draw content area background ---
        var contentAreaRect = new Rect(
            0,
            _headerHeight,
            ClientSize.Width,
            ClientSize.Height - _headerHeight
        );
        if (_contentBackgroundBrush != null)
            _renderTarget.FillRectangle(contentAreaRect, _contentBackgroundBrush);
        // --- End Draw content area background ---

        DrawRssItems(); // Draws items on top of this new background
        DrawCustomScrollbar(); // Draws scrollbar, also on top

        var result = _renderTarget.EndDraw();
        if (result.Failure)
        {
            Debug.WriteLine($"Direct2D EndDraw failed: {result.Code}");
            CreateDirect2DResources();
        }
    }

    private void DrawWidgetHeader()
    {
        var headerRect = new Rect(0, 0, ClientSize.Width, _headerHeight);
        _renderTarget.FillRectangle(headerRect, _headerBrush);
        _renderTarget.DrawText(_widgetTitle, _titleTextFormat, headerRect, _defaultTextBrush);
    }

    private void DrawRssItems()
    {
        var currentY = _headerHeight;
        var scrollbarAreaWidth = _items.Count > _settings.MaxDisplayItems ? _settings.ScrollbarWidth + 5 : 0;


        for (var i = 0; i < _settings.MaxDisplayItems; i++)
        {
            var itemActualIndex = _scrollOffset + i;
            if (itemActualIndex >= _items.Count) break;

            var item = _items[itemActualIndex];
            var itemRect = new Rect(0, currentY, ClientSize.Width - scrollbarAreaWidth, _itemHeight);
            var textRect = new Rect(
                10,
                currentY,
                ClientSize.Width - scrollbarAreaWidth - 20,
                _itemHeight
            );


            if (i == _hoveredIndex)
                _renderTarget.FillRectangle(itemRect, _hoverBackgroundBrush);

            var displayText = $"• {item.Title}";

            _renderTarget.DrawText(
                displayText,
                _listItemTextFormat,
                textRect, // Use the padded rect for text
                _defaultTextBrush,
                DrawTextOptions.Clip
            );
            currentY += _itemHeight;
        }
    }

    private void DrawCustomScrollbar()
    {
        if (_items.Count <= _settings.MaxDisplayItems) return; // No scrollbar if not needed

        var trackHeight = ClientSize.Height - _headerHeight - 10;
        float trackX = ClientSize.Width - _settings.ScrollbarWidth - 5;
        var trackY = _headerHeight + 5;

        // Scrollbar Thumb
        var contentRatio = (float)_settings.MaxDisplayItems / _items.Count;
        var thumbHeight = Math.Max(20, trackHeight * contentRatio); // Min thumb height

        var scrollableRatio = _items.Count - _settings.MaxDisplayItems == 0
            ? 0
            : (float)_scrollOffset / (_items.Count - _settings.MaxDisplayItems);
        var thumbY = trackY + (trackHeight - thumbHeight) * scrollableRatio;

        var thumbRect = new RectangleF(trackX, thumbY, _settings.ScrollbarWidth, thumbHeight);
        _renderTarget.FillRoundedRectangle(
            new RoundedRectangle(thumbRect, _settings.ScrollbarWidth / 2f, _settings.ScrollbarWidth / 2f),
            _scrollbarBrush);
    }


    private Region CreateRoundedRectangleRegion(int width, int height, int cornerRadius)
    {
        if (width <= 0 || height <= 0) return new Region(new Rectangle(0, 0, 1, 1));

        var path = new GraphicsPath();
        if (cornerRadius <= 0)
        {
            path.AddRectangle(new Rectangle(0, 0, width, height));
        }
        else
        {
            var R = cornerRadius * 2;
            path.AddArc(0, 0, R, R, 180, 90); // Top-left
            path.AddArc(width - R, 0, R, R, 270, 90); // Top-right
            path.AddArc(width - R, height - R, R, R, 0, 90); // Bottom-right
            path.AddArc(0, height - R, R, R, 90, 90); // Bottom-left
        }

        path.CloseAllFigures();
        return new Region(path);
    }

    private async Task LoadRssFeedAsync()
    {
        if (string.IsNullOrEmpty(_feedUrl))
        {
            UpdateItemsWithError("RSS URL is not configured.");
            return;
        }

        try
        {
            var rssContent = await httpClient.GetStringAsync(_feedUrl);
            var doc = new XmlDocument();
            doc.LoadXml(rssContent);

            var newFeedItems = new List<RssFeedItem>();
            var itemNodes = doc.SelectNodes("//item");

            if (itemNodes != null)
                foreach (XmlNode node in itemNodes)
                {
                    var title = node["title"]?.InnerText ?? "No Title";
                    var link = node["link"]?.InnerText ?? "#";
                    // Try to get description, fallback to title if not present
                    var description = node["description"]?.InnerText ?? node["summary"]?.InnerText ?? title;
                    newFeedItems.Add(new RssFeedItem(title, link, description));
                }

            if (!newFeedItems.SequenceEqual(_items))
            {
                _items = newFeedItems;
                _scrollOffset = 0; // Reset scroll on new content
                _hoveredIndex = -1; // Reset hover
                _totalActualContentHeight = _headerHeight + _items.Count * _itemHeight;
                Debug.WriteLine($"Loaded {_items.Count} items from {_widgetTitle}.");
                if (IsHandleCreated) Invalidate();
            }
        }
        catch (HttpRequestException httpEx)
        {
            UpdateItemsWithError($"Network error: {httpEx.Message}");
        }
        catch (XmlException xmlEx)
        {
            UpdateItemsWithError($"Feed parsing error: {xmlEx.Message}");
        }
        catch (Exception ex)
        {
            UpdateItemsWithError($"An unexpected error occurred: {ex.Message}");
            Debug.WriteLine($"Generic RSS Load Error: {ex}");
        }
    }

    private void UpdateItemsWithError(string errorMessage)
    {
        _items.Clear();
        _items.Add(new RssFeedItem("Error", "#", errorMessage));
        _scrollOffset = 0;
        _hoveredIndex = -1;
        if (IsHandleCreated) Invalidate();
        Debug.WriteLine($"RSS Reader Error for {_widgetTitle}: {errorMessage}");
    }


    // Helper to parse color strings (names or hex)
    private Color4 ParseColor(string colorString, Color4 defaultColor)
    {
        if (string.IsNullOrWhiteSpace(colorString))
            return defaultColor;

        try
        {
            // Handle hex colors (like #RRGGBB or #AARRGGBB)
            if (colorString.StartsWith("#"))
            {
                // Remove the # prefix
                var hex = colorString.Substring(1);

                // Parse the hex string
                uint argb;
                if (hex.Length == 6)
                {
                    argb = uint.Parse(hex, NumberStyles.HexNumber) | 0xFF000000;
                }
                else if (hex.Length == 8)
                {
                    argb = uint.Parse(hex, NumberStyles.HexNumber);
                    // ARGB to ABGR conversion needed for Vortice
                    argb = (argb & 0xFF00FF00) | ((argb & 0x00FF0000) >> 16) | ((argb & 0x000000FF) << 16);
                }
                else
                {
                    return defaultColor;
                }

                return new Color4(argb);
            }

            // Handle named colors using System.Drawing.Color
            var sdColor = ColorTranslator.FromHtml(colorString);
            return new Color4(
                sdColor.R / 255f,
                sdColor.G / 255f,
                sdColor.B / 255f,
                sdColor.A / 255f);
        }
        catch
        {
            return defaultColor;
        }
    }

    private static void SaveWidgetPosition(int instanceIndex, int left, int top)
    {
        Debug.WriteLine($"Saving position for Reader {instanceIndex}: X={left}, Y={top}");
        Beallitasok.RSS_Reader_Section[$"Olvasó_{instanceIndex}_Pos_X"].IntValue = left;
        Beallitasok.RSS_Reader_Section[$"Olvasó_{instanceIndex}_Pos_Y"].IntValue = top;
        Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
    }
}