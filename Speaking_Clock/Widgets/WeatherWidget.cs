using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using SharpGen.Runtime;
using Speaking_Clock;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Vortice.WIC;
using Vortice.WinForms;
using static Vanara.PInvoke.User32;
using BitmapInterpolationMode = Vortice.Direct2D1.BitmapInterpolationMode;
using Color = System.Drawing.Color;
using FontStyle = Vortice.DirectWrite.FontStyle;
using Size = System.Drawing.Size;
using TextAntialiasMode = Vortice.Direct2D1.TextAntialiasMode;

namespace Speaking_clock.Widgets;

public class DisplayWeatherData
{
    public string DayName { get; set; } = "Friday";
    public string Date { get; set; } = "May 11";
    public string Location { get; set; } = "Budapest, Hungary";
    public string UpdateTime { get; set; } = $"Updated at {DateTime.Now:HH:mm} CEST";
    public string Temperature { get; set; } = "8°C";
    public string Condition { get; set; } = "Fair";
    public string WeatherIconName { get; set; } = "fair_day.png";
    public string FeelsLike { get; set; } = "7°";
    public string Humidity { get; set; } = "52%";
    public string Visibility { get; set; } = "10 km";
    public string Wind { get; set; } = "11 km/h E";
    public string Pressure { get; set; } = "1021 mb Rising";
    public string DewPoint { get; set; } = "1°";
    public string UvIndex { get; set; } = "0 (Low)";
    public string Sunrise { get; set; } = "5:08";
    public string Sunset { get; set; } = "20:05";
    public string MoonPhase { get; set; } = "Waxing Gibbous";
    public string MoonIllumination { get; set; } = "93.1% Megvilágítva";
    public string MoonIconName { get; set; } = "moon_waxing_gibbous.png"; // Placeholder name
    public string FullDateDisplay { get; set; } = DateTime.Now.ToString("dddd, MMMM dd");
}

public class ForecastDayData
{
    public string DayName { get; set; } = "Saturday";
    public string Date { get; set; } = "May 12";
    public string TemperatureHigh { get; set; } = "20°";
    public string TemperatureLow { get; set; } = "8°";
    public string Humidity { get; set; } = "N/A";
    public string Condition { get; set; } = "Mostly Sunny";
    public string WeatherIconName { get; set; } = "mostly_sunny.png";
    public string PrecipitationChance { get; set; } = "1%";
}

public class WeatherWidget : RenderForm
{
    private const string IconBasePath = "Assets/WeatherIcons/";
    private const string MoonBasePath = "Assets/MoonIcons/";
    private const string PlaceholderWeatherIconFilename = "placeholder_weather.png";
    private const string PlaceholderMoonIconFilename = "placeholder_moon.png";

    private const float GlobalPadding = 10f;
    private const float SeparatorThickness = 1f;

    private const float CornerRadius = 15.0f;
    private readonly SizeI _detailsMoonIconSize = new(80, 80);
    private readonly SizeI _forecastDayIconSize = new(125, 75);
    private readonly List<ForecastDayData> _forecastDays;
    private readonly Dictionary<string, ID2D1Bitmap?> _loadedForecastIcons = new();
    private readonly SizeI _mainWeatherIconSize = new(170, 120);

    private readonly int forecastdaystoDraw;
    private string _actualCenturyGothicNameInCollection = "Century Gothic";

    // Custom Font Collection for Century Gothic
    private IDWriteFontCollection1? _centuryGothicFontCollection;
    private IDWriteTextFormat? _conditionTextFormat;
    private ID2D1Bitmap? _currentMainWeatherIconBitmap;
    private ID2D1Bitmap? _currentMoonDetailsIconBitmap;

    private IDWriteTextFormat? _defaultTextFormat;

    private DisplayWeatherData _displayedWeather;
    private IDWriteTextFormat? _forecastDateTextFormat;
    private IDWriteTextFormat? _forecastDayNameTextFormat;
    private IDWriteTextFormat? _forecastTempTextFormat;

    private bool _isDragging;
    private IDWriteTextFormat? _largeTemperatureTextFormat;

    private List<string> _loadedCustomFontFamilyNames = new();

    //private IDWriteTextFormat? _locationTextFormat;
    private Point _mouseDownLocation;
    private ID2D1SolidColorBrush? _precipitationTextBrush;
    private ID2D1HwndRenderTarget? _renderTarget;
    private ID2D1SolidColorBrush? _separatorBrush;
    private IDWriteTextFormat? _smallTextFormat;

    private ID2D1SolidColorBrush? _textBrush;
    private PrivateFontCollection? customFonts;
    private IDWriteTextLayout precipTextLayout;

    private IDWriteTextLayout tempTextLayout;

    public WeatherWidget(int startX, int startY, int days)
    {
        forecastdaystoDraw = days;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        //SetStyle(ControlStyles.Opaque, false);
        Text = "Weather Widget";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(startX, startY);
        ClientSize = new Size(270, 500);
        ShowInTaskbar = false;
        DoubleBuffered = false;
        Opacity = 0.9f;
        Region = CreateRoundedRectangleRegion(Size.Width, Size.Height, 20);

        _displayedWeather = new DisplayWeatherData();
        _forecastDays = new List<ForecastDayData>();

        MouseDown += WeatherWidget_MouseDown;
        MouseMove += WeatherWidget_MouseMove;
        MouseUp += WeatherWidget_MouseUp;

        Show();
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= (int)WindowStylesEx.WS_EX_TOOLWINDOW;
            ;
            return cp;
        }
    }

    private Region CreateRoundedRectangleRegion(int width, int height, int cornerRadius)
    {
        var path = new GraphicsPath();
        path.AddArc(0, 0, cornerRadius * 2, cornerRadius * 2, 180, 90);
        path.AddArc(width - cornerRadius * 2, 0, cornerRadius * 2, cornerRadius * 2, 270, 90);
        path.AddArc(width - cornerRadius * 2, height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
        path.AddArc(0, height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
        path.CloseAllFigures();
        return new Region(path);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        LoadCustomFonts();
        CreateRenderTarget();
        CreateDeviceDependentResources();
        LoadAssets();
        FetchWeatherDataAndForecast();
    }

    private void LoadCustomFonts()
    {
        if (GraphicsFactories.DWriteFactory == null)
        {
            Debug.WriteLine("DWriteFactory is not initialized. Cannot load custom fonts.");
            return;
        }

        string[] centuryGothicFiles = new[]
        {
            $"{Beallitasok.BasePath}\\Assets\\Fonts\\gothic.ttf",
            $"{Beallitasok.BasePath}\\Assets\\Fonts\\gothicb.ttf",
            $"{Beallitasok.BasePath}\\Assets\\Fonts\\gothici.ttf",
            $"{Beallitasok.BasePath}\\Assets\\Fonts\\gothicbi.ttf"
        };

        // Filter out non-existent files
        var existingFontFiles = centuryGothicFiles.Where(File.Exists).ToArray();
        if (!existingFontFiles.Any())
        {
            Debug.WriteLine(
                "No Century Gothic font files found at the specified paths. Custom font loading skipped. System font will be attempted.");
            return;
        }

        if (existingFontFiles.Length < centuryGothicFiles.Length)
            Debug.WriteLine(
                "Warning: Some specified Century Gothic font files were not found. Only found files will be loaded.");

        _centuryGothicFontCollection = DirectWriteFontLoader.LoadFontCollection(
            GraphicsFactories.DWriteFactory,
            existingFontFiles,
            out _loadedCustomFontFamilyNames
        );

        if (_centuryGothicFontCollection != null)
        {
            Debug.WriteLine("Custom Century Gothic font collection loaded successfully.");
            var targetName = "Century Gothic";
            _actualCenturyGothicNameInCollection =
                _loadedCustomFontFamilyNames.FirstOrDefault(name =>
                    name.Equals(targetName, StringComparison.OrdinalIgnoreCase)) ??
                _loadedCustomFontFamilyNames.FirstOrDefault() ??
                targetName;

            Debug.WriteLine(
                $"Will attempt to use font family name: '{_actualCenturyGothicNameInCollection}' from custom collection.");
        }
        else
        {
            Debug.WriteLine(
                "Failed to load custom Century Gothic font collection. Text formats will attempt to use system version of 'Century Gothic'.");
            _actualCenturyGothicNameInCollection = "Century Gothic";
        }
    }

    private void CreateRenderTarget()
    {
        _renderTarget?.Dispose();
        _renderTarget = GraphicsFactories.D2DFactory.CreateHwndRenderTarget(
            new RenderTargetProperties(),
            new HwndRenderTargetProperties
            {
                Hwnd = Handle,
                PixelSize = new SizeI(ClientSize.Width, ClientSize.Height),
                PresentOptions = PresentOptions.None
            });
        if (_renderTarget != null)
        {
            _renderTarget.SetDpi(96.0f, 96.0f);
            _renderTarget.TextAntialiasMode = TextAntialiasMode.Cleartype;
        }
    }

    private void CreateDeviceDependentResources()
    {
        if (_renderTarget == null) return;

        // Dispose existing resources first
        _textBrush?.Dispose();
        _separatorBrush?.Dispose();
        _precipitationTextBrush?.Dispose();
        //_locationTextFormat?.Dispose();
        _largeTemperatureTextFormat?.Dispose();
        _conditionTextFormat?.Dispose();
        _defaultTextFormat?.Dispose();
        _smallTextFormat?.Dispose();
        _forecastDayNameTextFormat?.Dispose();
        _forecastDateTextFormat?.Dispose();
        _forecastTempTextFormat?.Dispose();

        _textBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.White.ToArgb()));
        _separatorBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.LightGray.R / 255f,
            Color.LightGray.G / 255f, Color.LightGray.B / 255f, 0.4f));
        _precipitationTextBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.SkyBlue.R / 255f,
            Color.SkyBlue.G / 255f, Color.SkyBlue.B / 255f));

        // Use the custom font collection for Century Gothic if loaded, otherwise DWrite uses system fonts.
        IDWriteFontCollection? fontCollectionForCenturyGothic = _centuryGothicFontCollection;
        var centuryGothicNameToUse = _actualCenturyGothicNameInCollection;


        // Location Text Format 
        /* _locationTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(
             "Arial",
             null, // Use system font collection
             FontWeight.SemiBold,
             FontStyle.Normal,
             FontStretch.Normal,
             18f,
             CultureInfo.CurrentCulture.Name);*/

        // Century Gothic Formats
        _largeTemperatureTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(centuryGothicNameToUse,
            fontCollectionForCenturyGothic, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 60f,
            CultureInfo.CurrentCulture.Name);
        _conditionTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(centuryGothicNameToUse,
            fontCollectionForCenturyGothic, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 15f,
            CultureInfo.CurrentCulture.Name);
        _defaultTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(centuryGothicNameToUse,
            fontCollectionForCenturyGothic, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 15f,
            CultureInfo.CurrentCulture.Name);
        _smallTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(centuryGothicNameToUse,
            fontCollectionForCenturyGothic, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 15f,
            CultureInfo.CurrentCulture.Name);
        _forecastDayNameTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(centuryGothicNameToUse,
            fontCollectionForCenturyGothic, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 15f,
            CultureInfo.CurrentCulture.Name);
        _forecastDateTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(centuryGothicNameToUse,
            fontCollectionForCenturyGothic, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 13f,
            CultureInfo.CurrentCulture.Name);
        _forecastTempTextFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(centuryGothicNameToUse,
            fontCollectionForCenturyGothic, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 13f,
            CultureInfo.CurrentCulture.Name);

        // Set text alignment for all formats
        Action<IDWriteTextFormat?> setAlignment = tf =>
        {
            if (tf != null) tf.TextAlignment = TextAlignment.Leading;
        };
        //setAlignment(_locationTextFormat);
        setAlignment(_largeTemperatureTextFormat);
        setAlignment(_conditionTextFormat);
        setAlignment(_defaultTextFormat);
        setAlignment(_smallTextFormat);
        setAlignment(_forecastDayNameTextFormat);
        setAlignment(_forecastDateTextFormat);
        setAlignment(_forecastTempTextFormat);
    }

    private void LoadAssets()
    {
        if (_renderTarget == null) return;

        _currentMainWeatherIconBitmap?.Dispose();
        _currentMainWeatherIconBitmap =
            LoadBitmapFromPath(Path.Combine(IconBasePath, _displayedWeather.WeatherIconName),
                PlaceholderWeatherIconFilename);

        _currentMoonDetailsIconBitmap?.Dispose();
        _currentMoonDetailsIconBitmap = LoadBitmapFromPath(Path.Combine(MoonBasePath, _displayedWeather.MoonIconName),
            PlaceholderMoonIconFilename);

        foreach (var day in _forecastDays)
            if (!_loadedForecastIcons.ContainsKey(day.WeatherIconName) ||
                _loadedForecastIcons[day.WeatherIconName] == null)
                _loadedForecastIcons[day.WeatherIconName] =
                    LoadBitmapFromPath(Path.Combine(IconBasePath, day.WeatherIconName), PlaceholderWeatherIconFilename);
    }

    private ID2D1Bitmap? LoadBitmapFromPath(string primaryIconFilenameWithSubpath, string fallbackIconFilename)
    {
        if (_renderTarget == null) return null;
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var primaryFullPath = Path.Combine(baseDir, primaryIconFilenameWithSubpath);
        Debug.WriteLine(
            $"Attempting to load primary icon from: '{primaryFullPath}' (Derived from '{primaryIconFilenameWithSubpath}')");
        string? pathToLoad = null;

        if (File.Exists(primaryFullPath))
        {
            pathToLoad = primaryFullPath;
            Debug.WriteLine($"Primary icon found: '{pathToLoad}'");
        }
        else
        {
            Debug.WriteLine(
                $"Primary icon NOT FOUND at '{primaryFullPath}'. Trying fallback file: '{fallbackIconFilename}'");
            string[] fallbackSearchPaths =
            {
                Path.Combine(baseDir, IconBasePath, fallbackIconFilename),
                Path.Combine(baseDir, "Assets", fallbackIconFilename),
                Path.Combine(baseDir, fallbackIconFilename)
            };

            foreach (var fallbackPath in fallbackSearchPaths)
            {
                Debug.WriteLine($"Checking fallback at: '{fallbackPath}'");
                if (File.Exists(fallbackPath))
                {
                    pathToLoad = fallbackPath;
                    Debug.WriteLine($"Fallback icon found: '{pathToLoad}'");
                    break;
                }
            }

            if (string.IsNullOrEmpty(pathToLoad))
            {
                Debug.WriteLine(
                    $"All attempts failed for primary '{primaryIconFilenameWithSubpath}' and fallback '{fallbackIconFilename}'. No icon will be loaded.");
                return null;
            }
        }

        try
        {
            Debug.WriteLine($"Final path to load bitmap from: '{pathToLoad}'");
            using var decoder = GraphicsFactories.WicFactory.CreateDecoderFromFileName(pathToLoad);
            using var frame = decoder.GetFrame(0);
            using var converter = GraphicsFactories.WicFactory.CreateFormatConverter();
            converter.Initialize(frame, PixelFormat.Format32bppPBGRA, BitmapDitherType.None, null, 0,
                BitmapPaletteType.MedianCut);
            var bitmap = _renderTarget.CreateBitmapFromWicBitmap(converter);
            Debug.WriteLine($"Successfully loaded bitmap from '{pathToLoad}'");
            return bitmap;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading bitmap from '{pathToLoad}': {ex.Message} (HResult: {ex.HResult:X})");
            if ((uint)ex.HResult == 0x88982F03)
                Debug.WriteLine(
                    "WIC Error: Component not found. Ensure image format is supported and OS codecs are intact.");
            else if ((uint)ex.HResult == 0x80070002)
                Debug.WriteLine(
                    "Error: File not found by decoder, though File.Exists passed. Check permissions or path issues.");
            return null;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_renderTarget != null && ClientSize.Width > 0 && ClientSize.Height > 0)
        {
            try
            {
                _renderTarget.Resize(new SizeI(ClientSize.Width, ClientSize.Height));
            }
            catch (SharpGenException ex) when ((uint)ex.ResultCode.Code == 0x8899000C)
            {
                Debug.WriteLine("RenderTarget resize failed (0x8899000C), recreating.");
                CreateRenderTarget();
                CreateDeviceDependentResources();
                LoadAssets();
            }

            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_renderTarget == null || _textBrush == null || _separatorBrush == null ||
            _precipitationTextBrush == null) return;

        _renderTarget.BeginDraw();
        _renderTarget.Clear(new Color4(0.1f, 0.1f, 0.1f, 0.85f));
        // DrawBackground();
        float currentY = 0;
        DrawMainWeatherArea(new Rect(0, currentY, ClientSize.Width, ClientSize.Height), ref currentY);
        DrawForecastArea(new Rect(0, currentY, ClientSize.Width, ClientSize.Height - currentY), ref currentY);
        var result = _renderTarget.EndDraw();
        if (result.Failure) HandleRenderTargetFailure(result);
    }

    private void DrawBackground()
    {
        if (_renderTarget == null) return;
        using var backgroundBrush = _renderTarget.CreateSolidColorBrush(new Color4(0.1f, 0.1f, 0.1f));
        var backgroundRect = new Rect(0, 0, ClientSize.Width, ClientSize.Height);
        var roundedRect = new RoundedRectangle((RectangleF)backgroundRect, CornerRadius, CornerRadius);
        _renderTarget.FillRoundedRectangle(roundedRect, backgroundBrush);
    }

    private void DrawHorizontalSeparator(float yPosition, float startX, float width)
    {
        if (_renderTarget == null || _separatorBrush == null) return;
        _renderTarget.DrawLine(new Vector2(startX, yPosition), new Vector2(startX + width, yPosition), _separatorBrush,
            SeparatorThickness);
    }

    private void DrawMainWeatherArea(Rect areaRect, ref float currentGlobalY)
    {
        if (_renderTarget == null || _displayedWeather == null || _textBrush == null) return;

        var localCurrentY = areaRect.Top + 5;
        var contentWidth = areaRect.Width - 2 * GlobalPadding;

        /* if (_locationTextFormat != null && _smallTextFormat != null)
         {
             DrawTextBlock(ref localCurrentY, GlobalPadding, contentWidth, _displayedWeather.Location,
                 _locationTextFormat, _textBrush, 2);
             DrawTextBlock(ref localCurrentY, GlobalPadding, contentWidth, _displayedWeather.UpdateTime,
                 _smallTextFormat, _textBrush, 0);
         }*/

        var textBlockForTempCondWidth = contentWidth;
        if (_largeTemperatureTextFormat != null)
            DrawTextBlock(ref localCurrentY, GlobalPadding, textBlockForTempCondWidth, _displayedWeather.Temperature,
                _largeTemperatureTextFormat, _textBrush, 0);
        if (_conditionTextFormat != null)
            DrawTextBlock(ref localCurrentY, GlobalPadding, textBlockForTempCondWidth, _displayedWeather.Condition,
                _conditionTextFormat, _textBrush);

        var iconMainWeatherX = areaRect.Right - GlobalPadding - _mainWeatherIconSize.Width;
        var iconMainWeatherY = localCurrentY - _mainWeatherIconSize.Height + 20;

        if (_currentMainWeatherIconBitmap != null)
        {
            var iconRect = new Rect(iconMainWeatherX, iconMainWeatherY, _mainWeatherIconSize.Width,
                _mainWeatherIconSize.Height);
            var sourceRect = new Rect(0, 0, _currentMainWeatherIconBitmap.PixelSize.Width,
                _currentMainWeatherIconBitmap.PixelSize.Height);
            _renderTarget.DrawBitmap(_currentMainWeatherIconBitmap, iconRect, 1.0f, BitmapInterpolationMode.Linear,
                sourceRect);
        }
        else
        {
            DrawIconPlaceholder(
                new Rect(iconMainWeatherX, iconMainWeatherY, _mainWeatherIconSize.Width, _mainWeatherIconSize.Height),
                "W", Color.DarkGray);
        }

        localCurrentY += _mainWeatherIconSize.Height - 120;

        if (_defaultTextFormat != null)
        {
            DrawTextBlock(ref localCurrentY, GlobalPadding, contentWidth, $"Érződik: {_displayedWeather.FeelsLike}",
                _defaultTextFormat, _textBrush);
            DrawTextBlock(ref localCurrentY, GlobalPadding, contentWidth, $"Páratartalom: {_displayedWeather.Humidity}",
                _defaultTextFormat, _textBrush);
            DrawTextBlock(ref localCurrentY, GlobalPadding, contentWidth,
                $"Látótávolság: {_displayedWeather.Visibility}", _defaultTextFormat, _textBrush);
            DrawTextBlock(ref localCurrentY, GlobalPadding, contentWidth, $"Szél: {_displayedWeather.Wind}",
                _defaultTextFormat, _textBrush);
            /*/DrawTextBlock(ref localCurrentY, GlobalPadding, contentWidth, $"Nyomás: {_displayedWeather.Pressure}",
                _defaultTextFormat, _textBrush);*/
            /*/DrawTextBlock(ref localCurrentY, GlobalPadding, contentWidth, $"Harmatpont: {_displayedWeather.DewPoint}",
                _defaultTextFormat, _textBrush);*/
            DrawTextBlock(ref localCurrentY, GlobalPadding, contentWidth, $"UV Index: {_displayedWeather.UvIndex}",
                _defaultTextFormat, _textBrush);

            // Moon Details
            var actualMoonTextStartY = localCurrentY;
            float moonPhaseTextHeight = 0, moonIllumTextHeight = 0;
            var spacingAfterMoonPhase = 2f;

            if (_defaultTextFormat != null && !string.IsNullOrEmpty(_displayedWeather.MoonPhase))
            {
                using var layout1 = GraphicsFactories.DWriteFactory.CreateTextLayout(
                    $"Hold: {_displayedWeather.MoonPhase}", _defaultTextFormat, contentWidth, float.MaxValue);
                moonPhaseTextHeight = layout1.Metrics.Height;
            }

            if (_smallTextFormat != null && !string.IsNullOrEmpty(_displayedWeather.MoonIllumination))
            {
                using var layout2 = GraphicsFactories.DWriteFactory.CreateTextLayout(_displayedWeather.MoonIllumination,
                    _smallTextFormat, contentWidth, float.MaxValue);
                moonIllumTextHeight = layout2.Metrics.Height;
            }

            var totalTextBlockHeight = moonPhaseTextHeight + spacingAfterMoonPhase + moonIllumTextHeight;

            var moonIconDetailsX = areaRect.Right - GlobalPadding - _detailsMoonIconSize.Width;
            var iconDrawY = actualMoonTextStartY + (totalTextBlockHeight - _detailsMoonIconSize.Height) / 2;
            if (iconDrawY < actualMoonTextStartY) iconDrawY = actualMoonTextStartY;

            var iconVisualX = moonIconDetailsX;
            var iconVisualY = iconDrawY;

            if (_currentMoonDetailsIconBitmap != null)
            {
                var iconRect = new Rect(iconVisualX, iconVisualY, _detailsMoonIconSize.Width,
                    _detailsMoonIconSize.Height);
                var sourceRect = new Rect(0, 0, _currentMoonDetailsIconBitmap.PixelSize.Width,
                    _currentMoonDetailsIconBitmap.PixelSize.Height);
                _renderTarget.DrawBitmap(_currentMoonDetailsIconBitmap, iconRect, 1.0f, BitmapInterpolationMode.Linear,
                    sourceRect);
            }
            else
            {
                DrawIconPlaceholder(
                    new Rect(iconVisualX, iconVisualY, _detailsMoonIconSize.Width, _detailsMoonIconSize.Height), "M",
                    Color.LightSlateGray);
            }

            var tempTextY = actualMoonTextStartY; // Text starts at the same Y as the icon block
            DrawTextBlock(ref tempTextY, GlobalPadding, contentWidth, $"Hold: {_displayedWeather.MoonPhase}",
                _defaultTextFormat, _textBrush, spacingAfterMoonPhase);
            DrawTextBlock(ref tempTextY, GlobalPadding, contentWidth, _displayedWeather.MoonIllumination,
                _smallTextFormat, _textBrush);

            localCurrentY = Math.Max(tempTextY, iconVisualY + _detailsMoonIconSize.Height);
            localCurrentY -= 40; // Spacing after moon section
        }

        if (_defaultTextFormat != null)
        {
            DrawTextBlock(ref localCurrentY, GlobalPadding, contentWidth, $"Napkelte: {_displayedWeather.Sunrise}",
                _defaultTextFormat, _textBrush);
            DrawTextBlock(ref localCurrentY, GlobalPadding, contentWidth, $"Napnyugta: {_displayedWeather.Sunset}",
                _defaultTextFormat, _textBrush);
            localCurrentY += 5;
        }

        currentGlobalY = localCurrentY;
    }

    private void DrawForecastArea(Rect areaRect, ref float currentGlobalY)
    {
        if (_renderTarget == null || _forecastDays == null || _textBrush == null ||
            _precipitationTextBrush == null) return;

        var localCurrentY = currentGlobalY;

        // Draw separator if needed
        if (localCurrentY > GlobalPadding + SeparatorThickness)
        {
            DrawHorizontalSeparator(localCurrentY, GlobalPadding, areaRect.Width - 2 * GlobalPadding);
            localCurrentY += SeparatorThickness + 5;
        }
        else if (localCurrentY == 0)
        {
            localCurrentY += GlobalPadding;
        }

        var contentWidth = areaRect.Width - 2 * GlobalPadding;

        for (var i = 0; i < _forecastDays.Count; i++)
        {
            var day = _forecastDays[i];
            if (i > forecastdaystoDraw) break;

            var itemStartY = localCurrentY;
            var textStartX = GlobalPadding;
            var availableTextWidth = contentWidth; // Initially use full width for text

            // --- Draw Text ---
            if (_forecastDayNameTextFormat != null)
                DrawTextBlock(ref localCurrentY, textStartX, availableTextWidth, day.DayName,
                    _forecastDayNameTextFormat, _textBrush, 1);
            if (_forecastDateTextFormat != null)
                DrawTextBlock(ref localCurrentY, textStartX, availableTextWidth, day.Date, _forecastDateTextFormat,
                    _textBrush, 1);

            if (_forecastTempTextFormat != null && _textBrush != null && _precipitationTextBrush != null &&
                _renderTarget != null)
            {
                var temperaturesText =
                    i != 0 ? $"{day.TemperatureHigh} / {day.TemperatureLow}" : $"{day.TemperatureHigh}";
                var precipitationText = $"💧{day.PrecipitationChance}";
                var horizontalPaddingBetweenTexts = 15f;
                var spacingAfterCombinedLine = 1f;
                var lineStartY = localCurrentY;

                var maxLayoutHeight = Math.Max(_forecastTempTextFormat.FontSize, ClientSize.Height - lineStartY);
                if (maxLayoutHeight <= 0) maxLayoutHeight = _forecastTempTextFormat.FontSize;

                using var tempTextLayoutLocal = GraphicsFactories.DWriteFactory.CreateTextLayout(
                    temperaturesText, _forecastTempTextFormat, availableTextWidth, maxLayoutHeight);
                _renderTarget.DrawTextLayout(new Vector2(textStartX, lineStartY), tempTextLayoutLocal, _textBrush,
                    DrawTextOptions.None);

                var precipitationX = textStartX + tempTextLayoutLocal.Metrics.WidthIncludingTrailingWhitespace +
                                     horizontalPaddingBetweenTexts;
                var remainingWidthForPrecipitation = areaRect.Width - GlobalPadding - precipitationX;
                if (remainingWidthForPrecipitation < _forecastTempTextFormat.FontSize)
                    remainingWidthForPrecipitation = _forecastTempTextFormat.FontSize;

                using var precipTextLayoutLocal = GraphicsFactories.DWriteFactory.CreateTextLayout(
                    precipitationText, _forecastTempTextFormat, remainingWidthForPrecipitation, maxLayoutHeight);
                _renderTarget.DrawTextLayout(new Vector2(precipitationX, lineStartY), precipTextLayoutLocal,
                    _precipitationTextBrush, DrawTextOptions.None);

                var tempHeight = tempTextLayoutLocal.Metrics.Height;
                var precipHeight = precipTextLayoutLocal.Metrics.Height;
                localCurrentY = lineStartY + Math.Max(tempHeight, precipHeight) + spacingAfterCombinedLine;
            }
            // --- Prepare for Icon and Condition ---

            // Calculate the height of the condition text without drawing it yet
            float conditionTextHeight = 0;
            var conditionSpacingAfter = 3f;
            IDWriteTextLayout? conditionLayout = null;
            if (_smallTextFormat != null && !string.IsNullOrEmpty(day.Condition))
            {
                // Calculate available width considering icon placement
                var widthForCondition = contentWidth /*- _forecastDayIconSize.Width*/ - GlobalPadding;
                if (widthForCondition <= 0) widthForCondition = availableTextWidth;

                conditionLayout = GraphicsFactories.DWriteFactory.CreateTextLayout(
                    day.Condition, _smallTextFormat, widthForCondition, float.MaxValue);
                conditionTextHeight = conditionLayout.Metrics.Height;
            }

            // Calculate the total height the text
            var potentialYAfterCondition = localCurrentY + conditionTextHeight + conditionSpacingAfter;
            var textBlockActualHeight =
                potentialYAfterCondition - itemStartY -
                conditionSpacingAfter; // Height of text block area up to where condition would end

            _loadedForecastIcons.TryGetValue(day.WeatherIconName, out var dayIcon);
            var iconX = areaRect.Right - GlobalPadding - _forecastDayIconSize.Width;

            // Calculate icon Y based on the full potential text height to vertically center it
            var iconY = itemStartY + (textBlockActualHeight - _forecastDayIconSize.Height) / 2;
            if (iconY < itemStartY) iconY = itemStartY; // Ensure it doesn't go above the item start

            var iconRect = new Rect(iconX, iconY, _forecastDayIconSize.Width, _forecastDayIconSize.Height);
            if (dayIcon != null)
            {
                var sourceRect = new Rect(0, 0, dayIcon.PixelSize.Width, dayIcon.PixelSize.Height);
                _renderTarget.DrawBitmap(dayIcon, iconRect, 1.0f, BitmapInterpolationMode.Linear, sourceRect);
            }
            else
            {
                DrawIconPlaceholder(iconRect, "i", Color.DimGray);
            }

            if (conditionLayout != null && _renderTarget != null && _textBrush != null)
            {
                // Draw the pre-calculated layout
                _renderTarget.DrawTextLayout(new Vector2(textStartX, localCurrentY), conditionLayout, _textBrush,
                    DrawTextOptions.None);
                localCurrentY += conditionLayout.Metrics.Height + conditionSpacingAfter; // Advance Y position
                conditionLayout.Dispose(); // Dispose the layout object now that it's drawn
            }
            else if (!string.IsNullOrEmpty(day.Condition))
            {
                // Use DrawTextBlock if layout wasn't pre-calculated (less ideal but safe)
                var widthForCondition = contentWidth - _forecastDayIconSize.Width - GlobalPadding;
                if (widthForCondition <= 0) widthForCondition = availableTextWidth;
                DrawTextBlock(ref localCurrentY, textStartX, widthForCondition, day.Condition, _smallTextFormat!,
                    _textBrush, conditionSpacingAfter);
            }
            else
            {
                localCurrentY +=
                    conditionSpacingAfter;
            }

            var bottomOfIcon = iconY + _forecastDayIconSize.Height;
            localCurrentY = Math.Max(localCurrentY, bottomOfIcon) + 5; // Add final small padding

            // Draw separator for next item
            if (i < forecastdaystoDraw) // Draw separator if NOT the last item to be drawn
            {
                DrawHorizontalSeparator(localCurrentY, GlobalPadding, contentWidth);
                localCurrentY += SeparatorThickness + 5; // Spacing before next item
            }
            else
            {
                // Add padding after the last item
                localCurrentY += 5;
            }
        }

        // Update the global Y position based on where drawing finished in this area
        currentGlobalY = localCurrentY;
    }

    private void DrawTextBlock(ref float currentY, float x, float maxWidth, string text, IDWriteTextFormat format,
        ID2D1Brush brush, float spacingAfter = 5f)
    {
        if (string.IsNullOrEmpty(text) || _renderTarget == null || brush == null || format == null) return;
        var availableHeight = ClientSize.Height - currentY;
        if (availableHeight <= format.FontSize) return;

        using var textLayout =
            GraphicsFactories.DWriteFactory.CreateTextLayout(text, format, maxWidth,
                Math.Max(format.FontSize, availableHeight));
        _renderTarget.DrawTextLayout(new Vector2(x, currentY), textLayout, brush, DrawTextOptions.None);
        currentY += textLayout.Metrics.Height + spacingAfter;
    }

    private void DrawIconPlaceholder(Rect rect, string text, Color color)
    {
        if (_renderTarget == null || _defaultTextFormat == null || _textBrush == null) return;
        if (rect.Width <= 0 || rect.Height <= 0) return;

        using var phBrush =
            _renderTarget.CreateSolidColorBrush(new Color4(color.R / 255f, color.G / 255f, color.B / 255f, 0.7f));
        var roundedRect = new RoundedRectangle((RectangleF)rect, CornerRadius / 3, CornerRadius / 3);
        _renderTarget.FillRoundedRectangle(roundedRect, phBrush);

        if (_defaultTextFormat == null || _textBrush == null) return;
        using var layout =
            GraphicsFactories.DWriteFactory.CreateTextLayout(text, _defaultTextFormat, rect.Width, rect.Height);
        layout.TextAlignment = TextAlignment.Center;
        layout.ParagraphAlignment = ParagraphAlignment.Center;
        _renderTarget.DrawTextLayout(new Vector2(rect.Left, rect.Top), layout, _textBrush);
    }

    private void HandleRenderTargetFailure(Result result)
    {
        if (!result.Failure) return;
        Debug.WriteLine($"EndDraw failed (0x{result.Code:X}). Recreating target.");
        _renderTarget?.Dispose();
        _renderTarget = null;
        _currentMainWeatherIconBitmap?.Dispose();
        _currentMainWeatherIconBitmap = null;
        _currentMoonDetailsIconBitmap?.Dispose();
        _currentMoonDetailsIconBitmap = null;
        foreach (var iconEntry in _loadedForecastIcons) iconEntry.Value?.Dispose();
        _loadedForecastIcons.Clear();

        _textBrush?.Dispose();
        _textBrush = null;
        _separatorBrush?.Dispose();
        _separatorBrush = null;
        _precipitationTextBrush?.Dispose();
        _precipitationTextBrush = null;

        /*_locationTextFormat?.Dispose();
        _locationTextFormat = null;*/
        _largeTemperatureTextFormat?.Dispose();
        _largeTemperatureTextFormat = null;
        _conditionTextFormat?.Dispose();
        _conditionTextFormat = null;
        _defaultTextFormat?.Dispose();
        _defaultTextFormat = null;
        _smallTextFormat?.Dispose();
        _smallTextFormat = null;
        _forecastDayNameTextFormat?.Dispose();
        _forecastDayNameTextFormat = null;
        _forecastDateTextFormat?.Dispose();
        _forecastDateTextFormat = null;
        _forecastTempTextFormat?.Dispose();
        _forecastTempTextFormat = null;

        CreateRenderTarget();
        if (_renderTarget != null)
        {
            CreateDeviceDependentResources();
            LoadAssets();
            Invalidate();
        }
    }

    internal async Task FetchWeatherDataAndForecast()
    {
        Debug.WriteLine("Fetching weather and forecast data from JsonDocument...");

        if (Beallitasok.weatherData == null)
        {
            Debug.WriteLine("Input JsonDocument is null. Using default/empty data.");
            _displayedWeather = new DisplayWeatherData();
            _forecastDays.Clear();
            LoadAssets();
            if (IsHandleCreated && !IsDisposed) Invalidate();
            return;
        }

        var root = Beallitasok.weatherData.RootElement;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        V3WxObservationsCurrent? currentObs = null;
        V3WxForecastDaily7Day? forecastData = null;

        try
        {
            if (root.TryGetProperty("v3-wx-observations-current", out var currentObsElement))
                currentObs = currentObsElement.Deserialize<V3WxObservationsCurrent>(options);
            else
                Debug.WriteLine("Property 'v3-wx-observations-current' not found in JsonDocument.");

            if (root.TryGetProperty("v3-wx-forecast-daily-7day", out var forecastDataElement))
                forecastData = forecastDataElement.Deserialize<V3WxForecastDaily7Day>(options);
            else
                Debug.WriteLine("Property 'v3-wx-forecast-daily-7day' not found in JsonDocument.");


            if (currentObs == null || forecastData == null)
            {
                Debug.WriteLine("Failed to deserialize essential parts from JsonDocument. Using default/empty data.");
                _displayedWeather = new DisplayWeatherData();
                _forecastDays.Clear();
                LoadAssets();
                if (IsHandleCreated && !IsDisposed) Invalidate();
                return;
            }
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"Error deserializing from JsonElement: {ex.Message}. Using default/empty data.");
            _displayedWeather = new DisplayWeatherData();
            _forecastDays.Clear();
            LoadAssets();
            if (IsHandleCreated && !IsDisposed) Invalidate();
            return;
        }

        var moonData = await MoonService.LoadMoonDataAsync();

        var currentValidTime = DateTime.TryParse(currentObs.ValidTimeLocal, out var parsedValidTime)
            ? parsedValidTime
            : DateTime.Now;
        var sunriseTime = DateTime.TryParse(currentObs.SunriseTimeLocal, out var parsedSunrise)
            ? parsedSunrise
            : currentValidTime.Date.AddHours(6);
        var sunsetTime = DateTime.TryParse(currentObs.SunsetTimeLocal, out var parsedSunset)
            ? parsedSunset
            : currentValidTime.Date.AddHours(20);

        _displayedWeather = new DisplayWeatherData
        {
            Location = Beallitasok.Location,
            UpdateTime = $"Frissítve {currentValidTime:HH:mm} CEST",
            Temperature = $"{currentObs.Temperature}°C",
            Condition = currentObs.WxPhraseLong ?? currentObs.CloudCoverPhrase ?? "N/A",
            WeatherIconName = currentObs.IconCode.HasValue ? $"{currentObs.IconCode}.png" : "unknown.png",
            FeelsLike = $"{currentObs.TemperatureFeelsLike}°",
            Humidity = $"{currentObs.RelativeHumidity}%",
            Visibility = $"{currentObs.Visibility?.ToString("F2", CultureInfo.InvariantCulture) ?? "N/A"} km",
            Wind = $"{currentObs.WindSpeed} km/h {currentObs.WindDirectionCardinal}",
            Pressure = $"{currentObs.PressureAltimeter} mb {currentObs.PressureTendencyTrend ?? ""}",
            DewPoint = $"{currentObs.TemperatureDewPoint}°",
            UvIndex = $"{currentObs.UvIndex} ({currentObs.UvDescription})",
            Sunrise = sunriseTime.ToString("H:mm"),
            Sunset = sunsetTime.ToString("H:mm"),
            MoonPhase = MoonString(moonData.Phase.Replace("_", " ")),
            MoonIllumination = $"{moonData.PercentIlluminated}% megvilágítva",
            MoonIconName = $"{moonData.Phase}.png",
            Date = currentValidTime.ToString("MMMM dd", CultureInfo.CreateSpecificCulture("hu-HU")),
            FullDateDisplay = currentValidTime.ToString("dddd, MMMM dd", CultureInfo.CreateSpecificCulture("hu-HU"))
        };

        _forecastDays.Clear();
        var dayParts = forecastData.Daypart?.FirstOrDefault();
        var todayforecastIndex = 0;

        if (!dayParts.IconCode[0].HasValue) todayforecastIndex = 1;
        _forecastDays.Add(new ForecastDayData
        {
            DayName = dayParts.DaypartName[todayforecastIndex] ?? "Tonight",
            Date = currentValidTime.ToString("MMM dd",
                CultureInfo.CreateSpecificCulture("hu-HU")), // "Tonight" or "Today" is part of current day
            TemperatureLow = currentObs.DayOrNight == "N" ? "" :
                dayParts.Temperature[0].HasValue ? $"{dayParts.Temperature[0]}°" : "N/A",
            TemperatureHigh = dayParts.Temperature[todayforecastIndex].HasValue
                ? $"{dayParts.Temperature[todayforecastIndex]}°"
                : "N/A",
            PrecipitationChance = dayParts.PrecipChance[todayforecastIndex].HasValue
                ? $"{dayParts.PrecipChance[1]}%"
                : "N/A",
            Condition = (dayParts.WxPhraseLong?.Count > 1 ? dayParts.WxPhraseLong[todayforecastIndex] : null) ?? "N/A",
            WeatherIconName = dayParts.IconCode[todayforecastIndex].HasValue
                ? $"{dayParts.IconCode[todayforecastIndex]}.png"
                : "unknown.png"
        });

        var numberOfForecastDays = forecastData.DayOfWeek?.Count ?? 0;
        if (forecastData.DayOfWeek != null && dayParts != null)
            for (var i = 1;
                 i < numberOfForecastDays && _forecastDays.Count < 8;
                 i++) // Start from 1 for subsequent days
            {
                var dayPartIndexForDay = i * 2; // Index for the daytime part of the forecast day i
                if (dayPartIndexForDay >= (dayParts.DaypartName?.Count ?? 0) ||
                    dayPartIndexForDay >= (dayParts.IconCode?.Count ?? 0) ||
                    dayPartIndexForDay >= (dayParts.PrecipChance?.Count ?? 0)) break;

                var forecastDate = i < (forecastData.ValidTimeLocal?.Count ?? 0) &&
                                   DateTime.TryParse(forecastData.ValidTimeLocal?[i], out var parsedForecastDate)
                    ? parsedForecastDate
                    : currentValidTime.Date.AddDays(i);

                var dayCondition = (dayParts.WxPhraseLong?.Count > dayPartIndexForDay
                    ? dayParts.WxPhraseLong[dayPartIndexForDay]
                    : null) ?? "N/A";
                var precipChance = dayParts.PrecipChance?.Count > dayPartIndexForDay &&
                                   dayParts.PrecipChance[dayPartIndexForDay].HasValue
                    ? $"{dayParts.PrecipChance[dayPartIndexForDay]}%"
                    : "N/A";

                _forecastDays.Add(new ForecastDayData
                {
                    DayName = forecastData.DayOfWeek[i] ?? "N/A",
                    Date = forecastDate.ToString("MMM dd", CultureInfo.CreateSpecificCulture("hu-HU")),
                    TemperatureHigh = forecastData.CalendarDayTemperatureMax?.Count > i &&
                                      forecastData.CalendarDayTemperatureMax[i].HasValue
                        ? $"{forecastData.CalendarDayTemperatureMax[i]}°"
                        : "N/A",
                    TemperatureLow = forecastData.CalendarDayTemperatureMin?.Count > i &&
                                     forecastData.CalendarDayTemperatureMin[i].HasValue
                        ? $"{forecastData.CalendarDayTemperatureMin[i]}°"
                        : "N/A",
                    PrecipitationChance = precipChance,
                    Condition = dayCondition,
                    WeatherIconName = dayParts.IconCode?.Count > dayPartIndexForDay &&
                                      dayParts.IconCode[dayPartIndexForDay].HasValue
                        ? $"{dayParts.IconCode[dayPartIndexForDay]}.png"
                        : "unknown.png"
                });
            }

        LoadAssets();
        RecalculateHeight();
        if (IsHandleCreated && !IsDisposed) Invalidate();
    }


    private void WeatherWidget_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
        {
            _isDragging = true;
            _mouseDownLocation = e.Location;
        }
    }

    private void WeatherWidget_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            Left += e.X - _mouseDownLocation.X;
            Top += e.Y - _mouseDownLocation.Y;
        }
    }

    private void WeatherWidget_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = false;
            Beallitasok.WidgetSection["Időjárás_X"].IntValue = Left;
            Beallitasok.WidgetSection["Időjárás_Y"].IntValue = Top;
            Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderTarget?.Dispose();
            _currentMainWeatherIconBitmap?.Dispose();
            _currentMoonDetailsIconBitmap?.Dispose();
            foreach (var iconEntry in _loadedForecastIcons) iconEntry.Value?.Dispose();

            _textBrush?.Dispose();
            _separatorBrush?.Dispose();
            _precipitationTextBrush?.Dispose();

            // _locationTextFormat?.Dispose();
            _largeTemperatureTextFormat?.Dispose();
            _conditionTextFormat?.Dispose();
            _defaultTextFormat?.Dispose();
            _smallTextFormat?.Dispose();
            _forecastDayNameTextFormat?.Dispose();
            _forecastDateTextFormat?.Dispose();
            _forecastTempTextFormat?.Dispose();
            tempTextLayout?.Dispose();
            precipTextLayout?.Dispose();
        }

        base.Dispose(disposing);
    }

    private string MoonString(string name)
    {
        return name switch
        {
            "Full Moon" => "Telihold",
            "Waning Gibbous" => "Fogyó hold",
            "Last Quarter" => "Utolsó negyed",
            "Waning Crescent" => "Fogyó félhold",
            "New Moon" => "Újhold",
            "Waxing Crescent" => "Növő félhold",
            "First Quarter" => "Első negyed",
            "Waxing Gibbous" => "Növő félhold",
            _ => "Hold"
        };
    }

    /// <summary>
    ///     Measures how tall the main + forecast areas will be
    ///     and then resizes the window to exactly fit.
    /// </summary>
    private void RecalculateHeight()
    {
        //“Replay” the OnPaint’s layout logic, but only in-memory to get the total Y
        float y = 0;
        DrawMainWeatherArea(new Rect(0, 0, ClientSize.Width, 10000), ref y);
        DrawForecastArea(new Rect(0, y, ClientSize.Width, 10000), ref y);
        var needed = (int)(y + GlobalPadding);

        // Apply it on the UI thread
        if (InvokeRequired)
            Invoke((Action)(() => ApplyNewHeight(needed)));
        else
            ApplyNewHeight(needed);
    }

    private void ApplyNewHeight(int newHeight)
    {
        // resize the form
        ClientSize = new Size(ClientSize.Width, newHeight);
        // update the rounded region so corners stay round
        Region = CreateRoundedRectangleRegion(ClientSize.Width, newHeight, 20);

        // resize D2D render target if it already exists
        if (_renderTarget != null)
            _renderTarget.Resize(new SizeI(ClientSize.Width, ClientSize.Height));
    }
}