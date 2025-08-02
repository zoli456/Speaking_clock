using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Speaking_Clock;
using Vanara.PInvoke;
using Vortice;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.WIC;
using Vortice.WinForms;
using AlphaMode = Vortice.DCommon.AlphaMode;
using BitmapInterpolationMode = Vortice.Direct2D1.BitmapInterpolationMode;
using Color = System.Drawing.Color;
using FontStyle = Vortice.DirectWrite.FontStyle;
using PixelFormat = Vortice.WIC.PixelFormat;
using Timer = System.Windows.Forms.Timer;

namespace Speaking_clock.Widgets;

public class CountryName
{
    [JsonPropertyName("common")] public string Common { get; set; }

    [JsonPropertyName("official")] public string Official { get; set; }
}

public class TranslationDetail
{
    [JsonPropertyName("official")] public string Official { get; set; }

    [JsonPropertyName("common")] public string Common { get; set; }
}

public class Translations
{
    [JsonPropertyName("hun")] public TranslationDetail Hun { get; set; }
}

public class Flags
{
    [JsonPropertyName("filename")] public string Filename { get; set; }
}

public class CountryData
{
    [JsonPropertyName("name")] public CountryName Name { get; set; }

    [JsonPropertyName("translations")] public Translations Translations { get; set; }

    [JsonPropertyName("flags")] public Flags Flags { get; set; }
}

public class Flagguesser : RenderForm
{
    private const int NumOptions = 4;
    private const float MinimizeButtonSize = 30f;
    private const float MinimizeButtonPadding = 5f;
    private readonly List<CountryData> _currentOptions = new();
    private readonly Timer _nextRoundTimer;

    private readonly RectangleF[] _optionRects;
    private readonly Random _random = new((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    private List<CountryData> _allCountries;
    private ID2D1SolidColorBrush _borderBrush;
    private ID2D1SolidColorBrush _buttonBrush;

    // Counters
    private int _correctAnswersCount;
    private ID2D1SolidColorBrush _correctBrush;
    private CountryData _correctCountry;
    private ID2D1Bitmap _currentFlagBitmap;

    private IDWriteFactory _dwriteFactory;
    private GameState _gameState;
    private int _incorrectAnswersCount;
    private ID2D1SolidColorBrush _incorrectBrush;
    private bool _isDragging;
    private bool _isMinimized = true;
    private ID2D1SolidColorBrush _minimizeButtonBrush;
    private RectangleF _minimizeButtonRect;
    private Point _mouseDownLocation;
    private ID2D1HwndRenderTarget _renderTarget;
    private int _selectedIndex = -1;
    private bool _showResultFeedback;

    private ID2D1SolidColorBrush _startButtonBrush;
    private RectangleF _startButtonRect;
    private ID2D1SolidColorBrush _textBrush;
    private IDWriteTextFormat _textFormatOptions;
    private IDWriteTextFormat _textFormatScore;
    private IWICImagingFactory _wicFactory;

    public Flagguesser(int startX, int startY)
    {
        Opacity = 1;
        Text = "Flag Guesser Widget";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(startX, startY);
        ShowInTaskbar = false;
        Width = 200;
        Height = 60;

        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        AllowTransparency = true;
        BackColor = Color.Gray;
        TransparencyKey = BackColor;

        MouseDown += Widget_MouseDown;
        MouseMove += Widget_MouseMove;
        MouseUp += Widget_MouseUp;
        Closed += Widget_Closed;

        _currentOptions = new List<CountryData>();
        _optionRects = new RectangleF[NumOptions];
        _gameState = GameState.Minimized;

        _nextRoundTimer = new Timer { Interval = 5000 };
        _nextRoundTimer.Tick += (s, e) =>
        {
            _nextRoundTimer.Stop();
            StartNewRound();
        };

        Show();
        LoadCountriesDataAsync();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= (int)User32.WindowStylesEx.WS_EX_LAYERED | (int)User32.WindowStylesEx.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        InitializeDirectXComponents();
    }

    private void InitializeDirectXComponents()
    {
        _dwriteFactory = GraphicsFactories.DWriteFactory;
        _wicFactory = GraphicsFactories.WicFactory;

        CreateDeviceIndependentResources();
        CreateRenderTarget();
    }

    private void CreateDeviceIndependentResources()
    {
        _textFormatOptions?.Dispose();
        _textFormatOptions =
            _dwriteFactory.CreateTextFormat("Segoe UI", FontWeight.SemiBold, FontStyle.Normal, FontStretch.Normal, 16);
        _textFormatOptions.TextAlignment = TextAlignment.Center;
        _textFormatOptions.ParagraphAlignment = ParagraphAlignment.Center;
        _textFormatOptions.WordWrapping = WordWrapping.Wrap;

        _textFormatScore?.Dispose();
        _textFormatScore =
            _dwriteFactory.CreateTextFormat("Segoe UI", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 14);
        _textFormatScore.TextAlignment = TextAlignment.Center;
        _textFormatScore.ParagraphAlignment = ParagraphAlignment.Center;
    }

    private void CreateRenderTarget()
    {
        _renderTarget?.Dispose();

        var rtProps = new RenderTargetProperties
        {
            Type = RenderTargetType.Hardware,
            PixelFormat = new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
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

        CreateDeviceDependentResources();
    }

    private void CreateDeviceDependentResources()
    {
        _textBrush?.Dispose();
        _buttonBrush?.Dispose();
        _correctBrush?.Dispose();
        _incorrectBrush?.Dispose();
        _borderBrush?.Dispose();
        _startButtonBrush?.Dispose();

        if (_renderTarget == null) return;

        _textBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.White.ToArgb()));
        _buttonBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 60, 60, 65).ToArgb()));
        _correctBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 34, 139, 34).ToArgb()));
        _incorrectBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 178, 34, 34).ToArgb()));
        _borderBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 90, 90, 95).ToArgb()));
        _startButtonBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 70, 70, 75).ToArgb()));
        _minimizeButtonBrush =
            _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 80, 80, 85).ToArgb()));
    }

    private async void LoadCountriesDataAsync()
    {
        try
        {
            using var rs = Assembly.GetExecutingAssembly()
                               .GetManifestResourceStream("Speaking_clock.countries.json")
                           ?? throw new FileNotFoundException("Resource not found!");

            string jsonText;
            using (var rdr = new StreamReader(rs))
            {
                jsonText = await rdr.ReadToEndAsync();
            }

            var allLoadedCountries = JsonSerializer.Deserialize<List<CountryData>>(
                jsonText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );


            _allCountries = allLoadedCountries?
                .Where(c => c.Flags?.Filename != null &&
                            !string.IsNullOrWhiteSpace(c.Flags.Filename) &&
                            c.Translations?.Hun?.Common != null &&
                            !string.IsNullOrWhiteSpace(c.Translations.Hun.Common) &&
                            c.Name?.Common != null)
                .ToList();

            if (_allCountries == null || _allCountries.Count < NumOptions)
            {
                MessageBox.Show(
                    "Not enough valid country data (missing flag filenames, names, or common names) to start the game.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _gameState = GameState.Error;
                Invalidate();
                return;
            }

            StartNewRound();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading country data: {ex.Message}", "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _gameState = GameState.Error;
            Invalidate();
        }
    }

    private void StartNewRound()
    {
        if (_allCountries == null || _allCountries.Count < NumOptions)
        {
            _gameState = GameState.Error;
            Invalidate();
            return;
        }

        _showResultFeedback = false;
        _selectedIndex = -1;
        _currentFlagBitmap?.Dispose();
        _currentFlagBitmap = null;

        _currentOptions.Clear();

        _correctCountry = _allCountries[_random.Next(_allCountries.Count)];
        _currentOptions.Add(_correctCountry);

        while (_currentOptions.Count < NumOptions)
        {
            var randomCountry = _allCountries[_random.Next(_allCountries.Count)];
            if (!_currentOptions.Any(c => c.Name.Common == randomCountry.Name.Common))
                _currentOptions.Add(randomCountry);
        }

        for (var i = _currentOptions.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (_currentOptions[i], _currentOptions[j]) = (_currentOptions[j], _currentOptions[i]);
        }

        // Load flag from disk
        if (_correctCountry.Flags != null && !string.IsNullOrWhiteSpace(_correctCountry.Flags.Filename))
        {
            var flagFileName = _correctCountry.Flags.Filename;
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var flagPath = Path.Combine(baseDirectory, "Assets", "flags", flagFileName);

            if (File.Exists(flagPath))
            {
                try
                {
                    using (var stream = new FileStream(flagPath, FileMode.Open, FileAccess.Read))
                    {
                        if (_renderTarget != null)
                            _currentFlagBitmap = LoadBitmapFromStream(_renderTarget, stream, _wicFactory);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading flag file {flagPath}: {ex.Message}");
                    _currentFlagBitmap = null; // Ensure it's null if loading failed
                }
            }
            else
            {
                Console.WriteLine($"Flag file not found: {flagPath}");
                _currentFlagBitmap = null;
            }
        }
        else
        {
            _currentFlagBitmap = null; // No filename provided
            Console.WriteLine($"No flag filename for {_correctCountry.Name.Common}");
        }

        _gameState = GameState.ShowingQuestion;
        Invalidate(); // Redraw with new flag and options
    }

    public static ID2D1Bitmap LoadBitmapFromStream(ID2D1RenderTarget renderTarget, Stream stream,
        IWICImagingFactory wicFactory)
    {
        using (var wicBitmapDecoder = wicFactory.CreateDecoderFromStream(stream))
        {
            using (var wicFrameDecode = wicBitmapDecoder.GetFrame(0))
            {
                using (var wicConverter = wicFactory.CreateFormatConverter())
                {
                    wicConverter.Initialize(wicFrameDecode, PixelFormat.Format32bppPBGRA, BitmapDitherType.None, null,
                        0.0, BitmapPaletteType.MedianCut);
                    return renderTarget.CreateBitmapFromWicBitmap(wicConverter);
                }
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_renderTarget == null)
            return;

        _renderTarget.BeginDraw();
        _renderTarget.Clear(new Color4(Color.Gray.ToArgb()));

        if (_isMinimized)
            DrawMinimizedScreen(_renderTarget);
        else
            switch (_gameState)
            {
                case GameState.Initializing:
                    DrawCenteredText(_renderTarget, "Initializing...", ClientRectangle, _textFormatOptions);
                    break;
                case GameState.Error:
                    DrawCenteredText(_renderTarget, "An error occurred. Please restart.", ClientRectangle,
                        _textFormatOptions);
                    break;
                case GameState.ShowingQuestion:
                case GameState.ShowingResult:
                    DrawGameScreen(_renderTarget);
                    break;
            }

        _renderTarget.EndDraw();
    }

    private void DrawCenteredText(ID2D1RenderTarget rt, string text, RectangleF bounds, IDWriteTextFormat format)
    {
        if (format != null &&
            _textBrush != null)
            rt.DrawText(text, format, (Rect)bounds, _textBrush);
    }

    private void DrawGameScreen(ID2D1RenderTarget rt)
    {
        // Draw minimize button in top-right corner
        _minimizeButtonRect = new RectangleF(
            Width - MinimizeButtonSize - MinimizeButtonPadding,
            MinimizeButtonPadding,
            MinimizeButtonSize,
            MinimizeButtonSize);

        rt.FillRectangle(_minimizeButtonRect, _minimizeButtonBrush);
        rt.DrawRectangle(_minimizeButtonRect, _borderBrush, 1.5f);

        // Draw minus sign for minimize
        var minusLine = new RectangleF(
            _minimizeButtonRect.Left + 8,
            _minimizeButtonRect.Top + _minimizeButtonRect.Height / 2 - 1,
            _minimizeButtonRect.Width - 16,
            3);
        rt.FillRectangle(minusLine, _textBrush);

        // Adjust other elements to account for minimize button
        var scoreDisplayHeight = 30f;
        var topMargin = MinimizeButtonSize + MinimizeButtonPadding * 2; // Increased top margin
        var spacing = 10f;
        var flagMaxHeight = Height * 0.35f;
        var flagMaxWidth = Width * 0.9f;
        var buttonAreaPadding = Width * 0.05f;

        // Draw Score first at the top
        var scoreText = $"Helyes: {_correctAnswersCount}   Hibás: {_incorrectAnswersCount}";
        var scoreRect = new RectangleF(buttonAreaPadding, topMargin, Width - buttonAreaPadding * 2, scoreDisplayHeight);
        DrawCenteredText(rt, scoreText, scoreRect, _textFormatScore);


        // Draw flag
        var flagTopY = topMargin + scoreDisplayHeight + spacing;
        var flagRect = new RectangleF();
        if (_currentFlagBitmap != null)
        {
            var aspectRatio = (float)_currentFlagBitmap.PixelSize.Width / _currentFlagBitmap.PixelSize.Height;
            var displayWidth = flagMaxWidth;
            var displayHeight = displayWidth / aspectRatio;

            if (displayHeight > flagMaxHeight)
            {
                displayHeight = flagMaxHeight;
                displayWidth = displayHeight * aspectRatio;
            }

            flagRect = new RectangleF((Width - displayWidth) / 2, flagTopY, displayWidth, displayHeight);
            rt.DrawBitmap(_currentFlagBitmap, flagRect, 1.0f, BitmapInterpolationMode.Linear,
                new RawRectF(0, 0, _currentFlagBitmap.PixelSize.Width, _currentFlagBitmap.PixelSize.Height));
        }
        else if (_correctCountry != null)
        {
            flagRect = new RectangleF((Width - flagMaxWidth) / 2, flagTopY, flagMaxWidth, flagMaxHeight);
            DrawCenteredText(rt, $"Flag for\n{_correctCountry.Translations.Hun.Common}\nunavailable", flagRect,
                _textFormatOptions);
        }
        else
        {
            flagRect = new RectangleF((Width - flagMaxWidth) / 2, flagTopY, flagMaxWidth, flagMaxHeight);
            DrawCenteredText(rt, "Loading flag...", flagRect, _textFormatOptions);
        }

        // Draw options
        var buttonsTopY = flagRect.Bottom + spacing * 1.5f;
        var availableHeightForButtons =
            Height - buttonsTopY - spacing;
        var buttonHeight = (availableHeightForButtons - (NumOptions - 1) * spacing) / NumOptions;
        if (buttonHeight < 35) buttonHeight = 35;
        if (buttonHeight > 60) buttonHeight = 60;


        var currentY = buttonsTopY;

        for (var i = 0; i < NumOptions; i++)
        {
            _optionRects[i] = new RectangleF(buttonAreaPadding, currentY, Width - buttonAreaPadding * 2, buttonHeight);
            var currentOptionBrush = _buttonBrush;

            if (_showResultFeedback)
            {
                if (_currentOptions[i].Name.Common == _correctCountry.Name.Common)
                    currentOptionBrush = _correctBrush;
                else if (i == _selectedIndex) currentOptionBrush = _incorrectBrush;
            }

            if (currentOptionBrush != null)
                rt.FillRectangle(_optionRects[i], currentOptionBrush);
            if (_borderBrush != null)
                rt.DrawRectangle(_optionRects[i], _borderBrush, 1.5f);

            var optionText = _currentOptions.Count > i && _currentOptions[i]?.Translations?.Hun?.Common != null
                ? _currentOptions[i].Translations.Hun.Common
                : "N/A";
            DrawCenteredText(rt, optionText, _optionRects[i], _textFormatOptions);
            currentY += buttonHeight + spacing;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        CreateRenderTarget();
        Invalidate();
    }

    private void Widget_MouseDown(object sender, MouseEventArgs e)
    {
        if (_isMinimized)
        {
            // When minimized, only allow dragging with right mouse button
            if (e.Button == MouseButtons.Right && Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
            {
                _isDragging = true;
                _mouseDownLocation = e.Location;
            }
        }
        else
        {
            // When not minimized, check if clicking on minimize button
            if (_minimizeButtonRect.Contains(e.Location)) return;

            // When not minimized, only allow dragging if clicking on flag area
            if (e.Button == MouseButtons.Left && Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
            {
                // Calculate flag area (you'll need to define this based on your drawing logic)
                var flagRect = GetCurrentFlagRect();
                if (flagRect.Contains(e.Location))
                {
                    _isDragging = true;
                    _mouseDownLocation = e.Location;
                }
            }
        }
    }

    private RectangleF GetCurrentFlagRect()
    {
        // Same flag drawing logic in DrawGameScreen()
        var topMargin = MinimizeButtonSize + MinimizeButtonPadding * 2;
        var scoreDisplayHeight = 30f;
        var spacing = 10f;
        var flagTopY = topMargin + scoreDisplayHeight + spacing;
        var flagMaxWidth = Width * 0.9f;
        var flagMaxHeight = Height * 0.35f;

        if (_currentFlagBitmap != null)
        {
            var aspectRatio = (float)_currentFlagBitmap.PixelSize.Width / _currentFlagBitmap.PixelSize.Height;
            var displayWidth = flagMaxWidth;
            var displayHeight = displayWidth / aspectRatio;

            if (displayHeight > flagMaxHeight)
            {
                displayHeight = flagMaxHeight;
                displayWidth = displayHeight * aspectRatio;
            }

            return new RectangleF((Width - displayWidth) / 2, flagTopY, displayWidth, displayHeight);
        }

        return new RectangleF((Width - flagMaxWidth) / 2, flagTopY, flagMaxWidth, flagMaxHeight);
    }

    private void Widget_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            Left += e.X - _mouseDownLocation.X;
            Top += e.Y - _mouseDownLocation.Y;
        }
    }

    private void Widget_MouseUp(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            Beallitasok.WidgetSection["Zászló_X"].IntValue = Left;
            Beallitasok.WidgetSection["Zászló_Y"].IntValue = Top;
            Beallitasok.ConfigParser.SaveToFile(Path.Combine(Beallitasok.BasePath, Beallitasok.SetttingsFileName));
            return; // Skip other checks if we were dragging
        }

        if (_isMinimized)
        {
            if (_startButtonRect.Contains(e.X, e.Y)) ExpandToFullView();
        }
        else
        {
            // Check if clicked on minimize button
            if (_minimizeButtonRect.Contains(e.X, e.Y))
            {
                MinimizeToButton();
                return;
            }

            if (_gameState == GameState.ShowingQuestion)
                for (var i = 0; i < NumOptions; i++)
                    if (_optionRects[i].Contains(e.X, e.Y))
                    {
                        _selectedIndex = i;
                        _showResultFeedback = true;
                        _gameState = GameState.ShowingResult;

                        if (_currentOptions[i].Name.Common == _correctCountry.Name.Common)
                            _correctAnswersCount++;
                        else
                            _incorrectAnswersCount++;

                        _nextRoundTimer.Start();
                        Invalidate();
                        break;
                    }
        }
    }

    private void Widget_Closed(object? sender, EventArgs e)
    {
        Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _nextRoundTimer?.Dispose();
            _currentFlagBitmap?.Dispose();
            _textBrush?.Dispose();
            _buttonBrush?.Dispose();
            _correctBrush?.Dispose();
            _incorrectBrush?.Dispose();
            _borderBrush?.Dispose();
            _textFormatOptions?.Dispose();
            _textFormatScore?.Dispose();
            _dwriteFactory?.Dispose();
            (_wicFactory as IDisposable)?.Dispose();
            _renderTarget?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void DrawMinimizedScreen(ID2D1RenderTarget rt)
    {
        // Draw single start button
        _startButtonRect = new RectangleF(20, 10, Width - 40, Height - 20);

        rt.FillRectangle(_startButtonRect, _startButtonBrush);
        rt.DrawRectangle(_startButtonRect, _borderBrush, 1.5f);

        DrawCenteredText(rt, "Zászlók", _startButtonRect, _textFormatOptions);
    }

    private void ExpandToFullView()
    {
        _isMinimized = false;
        Width = 380;
        Height = 600;
        StartNewRound();
    }

    private void MinimizeToButton()
    {
        _isMinimized = true;
        Width = 200;
        Height = 60;
        Invalidate();
    }

    private enum GameState
    {
        Minimized,
        Initializing,
        ShowingQuestion,
        ShowingResult,
        Error
    }
}