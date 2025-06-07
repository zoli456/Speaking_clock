using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Speaking_clock.Widgets;
using Vanara.PInvoke;
using Vortice;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Vortice.WIC;
using Vortice.WinForms;
using BitmapInterpolationMode = Vortice.Direct2D1.BitmapInterpolationMode;
using Color = System.Drawing.Color;
using FontStyle = Vortice.DirectWrite.FontStyle;
using Timer = System.Windows.Forms.Timer;

namespace Speaking_Clock.Widgets;

public class LogoData
{
    [JsonPropertyName("filename")] public string Filename { get; set; }

    [JsonPropertyName("full_filename")] public string FullFilename { get; set; }

    [JsonPropertyName("logo_name")] public string LogoName { get; set; }
}

public class LogoGuesser : RenderForm
{
    private const float MinimizeButtonSize = 30f;
    private const float MinimizeButtonPadding = 5f;
    private readonly Timer _nextRoundTimer;
    private readonly Random _random = new();

    private List<LogoData> _allLogos;
    private HashSet<char> _alreadyGuessedChars;
    private ID2D1SolidColorBrush _borderBrush;
    private ID2D1SolidColorBrush _buttonBrush;


    private int _correctlyGuessedLogosCount;

    private LogoData _currentLogo;
    private ID2D1Bitmap _currentLogoBitmap;
    private string _currentLogoNameNormalized = "";


    private IDWriteFactory _dwriteFactory;
    private GameState _gameState;
    private StringBuilder _guessedCharsDisplay;
    private ID2D1SolidColorBrush _inputBackgroundBrush;

    private RectangleF _inputDisplayRect;
    private bool _isDragging;
    private bool _isMinimized = true;
    private ID2D1SolidColorBrush _minimizeButtonBrush;
    private RectangleF _minimizeButtonRect;
    private Point _mouseDownLocation;
    private ID2D1HwndRenderTarget _renderTarget;
    private bool _showFullLogo;
    private ID2D1SolidColorBrush _skipButtonBrush;

    private RectangleF _skipButtonRect;
    private int _skippedLogosCount;

    private ID2D1SolidColorBrush _startButtonBrush;
    private RectangleF _startButtonRect;
    private ID2D1SolidColorBrush _textBrush;
    private IDWriteTextFormat _textFormatButton;
    private IDWriteTextFormat _textFormatInput;
    private IDWriteTextFormat _textFormatScore;
    private IWICImagingFactory _wicFactory;


    public LogoGuesser(int startX, int startY)
    {
        Opacity = 1;
        Text = "Logo Guesser Widget";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(startX, startY);
        ShowInTaskbar = false;
        Width = 160;
        Height = 40;
        KeyPreview = true; // Important for capturing key presses

        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        AllowTransparency = true;
        BackColor = Color.FromArgb(45, 45, 48);
        TransparencyKey = Color.Magenta;


        MouseDown += Widget_MouseDown;
        MouseMove += Widget_MouseMove;
        MouseUp += Widget_MouseUp;
        KeyPress += Widget_KeyPress;
        Closed += Widget_Closed;

        _gameState = GameState.Minimized;

        _nextRoundTimer = new Timer { Interval = 3000 };
        _nextRoundTimer.Tick += (s, e) =>
        {
            _nextRoundTimer.Stop();
            StartNewRound();
        };

        Show();
        LoadLogosDataAsync();
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
        _textFormatInput?.Dispose();
        _textFormatInput =
            _dwriteFactory.CreateTextFormat("Segoe UI", FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 24);
        _textFormatInput.TextAlignment = TextAlignment.Center;
        _textFormatInput.ParagraphAlignment = ParagraphAlignment.Center;
        _textFormatInput.WordWrapping = WordWrapping.NoWrap;


        _textFormatScore?.Dispose();
        _textFormatScore =
            _dwriteFactory.CreateTextFormat("Segoe UI", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 14);
        _textFormatScore.TextAlignment = TextAlignment.Leading;
        _textFormatScore.ParagraphAlignment = ParagraphAlignment.Center;

        _textFormatButton?.Dispose();
        _textFormatButton =
            _dwriteFactory.CreateTextFormat("Segoe UI", FontWeight.SemiBold, FontStyle.Normal, FontStretch.Normal, 16);
        _textFormatButton.TextAlignment = TextAlignment.Center;
        _textFormatButton.ParagraphAlignment = ParagraphAlignment.Center;
    }

    private void CreateRenderTarget()
    {
        _renderTarget?.Dispose();
        if (Width == 0 || Height == 0) return;

        _renderTarget = GraphicsFactories.D2DFactory.CreateHwndRenderTarget(
            new RenderTargetProperties(),
            new HwndRenderTargetProperties
            {
                Hwnd = Handle,
                PixelSize = new SizeI(Width, Height),
                PresentOptions =
                    PresentOptions
                        .None
            });
        CreateDeviceDependentResources();
    }

    private void CreateDeviceDependentResources()
    {
        _textBrush?.Dispose();
        _buttonBrush?.Dispose();
        _borderBrush?.Dispose();
        _startButtonBrush?.Dispose();
        _minimizeButtonBrush?.Dispose();
        _skipButtonBrush?.Dispose();
        _inputBackgroundBrush?.Dispose();

        if (_renderTarget == null) return;

        _textBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.White.ToArgb()));
        _buttonBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 70, 70, 75).ToArgb()));
        _inputBackgroundBrush =
            _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 50, 50, 55).ToArgb()));
        _borderBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 90, 90, 95).ToArgb()));
        _startButtonBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 0, 122, 204).ToArgb()));
        _minimizeButtonBrush =
            _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 80, 80, 85).ToArgb()));
        _skipButtonBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 200, 80, 80).ToArgb()));
    }

    private async void LoadLogosDataAsync()
    {
        try
        {
            var resourceName = "Speaking_clock.logos_final.json";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var filePath = Path.Combine(baseDirectory, "logos_corrected.json");
                if (File.Exists(filePath))
                {
                    using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    _allLogos = await JsonSerializer.DeserializeAsync<List<LogoData>>(fileStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    throw new FileNotFoundException(
                        $"Resource '{resourceName}' or file '{filePath}' not found. Ensure 'logos_corrected.json' is an embedded resource or in the output directory.");
                }
            }
            else
            {
                _allLogos = await JsonSerializer.DeserializeAsync<List<LogoData>>(stream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }


            _allLogos = _allLogos?
                .Where(logo => !string.IsNullOrWhiteSpace(logo.Filename) &&
                               !string.IsNullOrWhiteSpace(logo.FullFilename) &&
                               !string.IsNullOrWhiteSpace(logo.LogoName))
                .ToList();

            if (_allLogos == null || _allLogos.Count == 0)
            {
                MessageBox.Show(
                    "Not enough valid logo data (missing filenames or logo names) to start the game.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _gameState = GameState.Error;
                Invalidate();
                return;
            }

            _gameState = GameState.Minimized;
            Invalidate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading logo data: {ex.Message}\nStackTrace: {ex.StackTrace}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _gameState = GameState.Error;
            Invalidate();
        }
    }

    private void StartNewRound()
    {
        if (_allLogos == null || _allLogos.Count == 0)
        {
            _gameState = GameState.Error;
            Invalidate();
            return;
        }

        _showFullLogo = false;
        _currentLogoBitmap?.Dispose();
        _currentLogoBitmap = null;

        _currentLogo = _allLogos[_random.Next(_allLogos.Count)];
        _currentLogoNameNormalized = NormalizeString(_currentLogo.LogoName);

        _guessedCharsDisplay = new StringBuilder();
        for (var i = 0; i < _currentLogoNameNormalized.Length; i++)
            if (_currentLogoNameNormalized[i] == ' ') // Preserve spaces from normalized name
                _guessedCharsDisplay.Append(' ');
            else
                _guessedCharsDisplay.Append('_');
        _alreadyGuessedChars = new HashSet<char>();

        LoadLogoImage(_currentLogo.Filename);

        _gameState = GameState.ShowingQuestion;
        Invalidate();
    }

    private string NormalizeString(string input)
    {
        //ToUpper and replace multiple spaces with one, trim.
        if (string.IsNullOrWhiteSpace(input)) return "";
        var upper = input.ToUpperInvariant();
        return Regex.Replace(upper, @"\s+", " ").Trim();
    }

    private void LoadLogoImage(string fileName, bool isFullImage = false)
    {
        _currentLogoBitmap?.Dispose();
        _currentLogoBitmap = null;

        if (string.IsNullOrWhiteSpace(fileName)) return;

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var logoPath = Path.Combine(baseDirectory, "Assets", "logos", fileName);

        if (File.Exists(logoPath))
            try
            {
                using (var stream = new FileStream(logoPath, FileMode.Open, FileAccess.Read))
                {
                    if (_renderTarget != null)
                        _currentLogoBitmap = LoadBitmapFromStream(_renderTarget, stream, _wicFactory);
                }

                if (isFullImage) _showFullLogo = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading logo file {logoPath}: {ex.Message}");
            }
        else
            Console.WriteLine($"Logo file not found: {logoPath}");

        Invalidate();
    }


    public static ID2D1Bitmap LoadBitmapFromStream(ID2D1RenderTarget renderTarget, Stream stream,
        IWICImagingFactory wicFactory)
    {
        using var wicBitmapDecoder = wicFactory.CreateDecoderFromStream(stream);
        using var wicFrameDecode = wicBitmapDecoder.GetFrame(0);
        using var wicConverter = wicFactory.CreateFormatConverter();
        wicConverter.Initialize(wicFrameDecode, PixelFormat.Format32bppPBGRA, BitmapDitherType.None, null, 0.0,
            BitmapPaletteType.MedianCut);
        return renderTarget.CreateBitmapFromWicBitmap(wicConverter);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_renderTarget == null)
        {
            CreateRenderTarget();
            if (_renderTarget == null) return;
        }

        _renderTarget.BeginDraw();
        _renderTarget.Clear(ToColor4(Color.Gray));


        if (_isMinimized)
            DrawMinimizedScreen(_renderTarget);
        else
            switch (_gameState)
            {
                case GameState.Initializing:
                    DrawCenteredText(_renderTarget, "Initializing Logos...", ClientRectangle, _textFormatButton);
                    break;
                case GameState.Error:
                    DrawCenteredText(_renderTarget, "An error occurred. Please restart.", ClientRectangle,
                        _textFormatButton);
                    break;
                case GameState.ShowingQuestion:
                case GameState.ShowingResult:
                    DrawGameScreen(_renderTarget);
                    break;
            }

        _renderTarget.EndDraw();
    }

    private Color4 ToColor4(Color color)
    {
        return new Color4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
    }

    private void DrawCenteredText(ID2D1RenderTarget rt, string text, RectangleF bounds, IDWriteTextFormat format,
        ID2D1Brush brush = null)
    {
        if (format != null && (brush ?? _textBrush) != null)
            rt.DrawText(text, format, (Rect)bounds, brush ?? _textBrush);
    }

    private void DrawGameScreen(ID2D1RenderTarget rt)
    {
        _minimizeButtonRect = new RectangleF(
            Width - MinimizeButtonSize - MinimizeButtonPadding,
            MinimizeButtonPadding,
            MinimizeButtonSize,
            MinimizeButtonSize);
        rt.FillRectangle(_minimizeButtonRect, _minimizeButtonBrush);
        rt.DrawRectangle(_minimizeButtonRect, _borderBrush, 1.0f);
        var minusLine = new RectangleF(_minimizeButtonRect.Left + 7,
            _minimizeButtonRect.Top + _minimizeButtonRect.Height / 2 - 1, _minimizeButtonRect.Width - 14, 2);
        rt.FillRectangle(minusLine, _textBrush);


        var topMargin = MinimizeButtonSize + MinimizeButtonPadding * 2;
        var contentPadding = 15f;
        var scoreHeight = 25f;
        var logoAreaHeight = Height * 0.40f;
        var inputAreaHeight = 60f;
        var buttonHeight = 40f;
        var spacing = 10f;

        var scoreText = $"Kitalálva: {_correctlyGuessedLogosCount}  Kihagyva: {_skippedLogosCount}";
        var scoreRect = new RectangleF(contentPadding, topMargin, Width - contentPadding * 2, scoreHeight);
        DrawCenteredText(rt, scoreText, scoreRect, _textFormatScore);

        var logoTopY = scoreRect.Bottom + spacing;
        RectangleF logoRect;
        if (_currentLogoBitmap != null)
        {
            var aspectRatio = (float)_currentLogoBitmap.PixelSize.Width / _currentLogoBitmap.PixelSize.Height;
            var displayWidth = Width - contentPadding * 2;
            var displayHeight = displayWidth / aspectRatio;

            if (displayHeight > logoAreaHeight)
            {
                displayHeight = logoAreaHeight;
                displayWidth = displayHeight * aspectRatio;
            }

            if (displayWidth > Width - contentPadding * 2)
            {
                displayWidth = Width - contentPadding * 2;
                displayHeight = displayWidth / aspectRatio;
            }

            logoRect = new RectangleF((Width - displayWidth) / 2, logoTopY, displayWidth, displayHeight);
            rt.DrawBitmap(_currentLogoBitmap, logoRect, 1.0f, BitmapInterpolationMode.Linear,
                new RawRectF(0, 0, _currentLogoBitmap.Size.Width, _currentLogoBitmap.Size.Height));
        }
        else
        {
            logoRect = new RectangleF(contentPadding, logoTopY, Width - contentPadding * 2, logoAreaHeight);
            DrawCenteredText(rt, _currentLogo != null ? "Logo missing..." : "Loading...", logoRect, _textFormatButton);
        }

        var inputDisplayTopY = logoRect.Bottom + spacing;
        _inputDisplayRect =
            new RectangleF(contentPadding, inputDisplayTopY, Width - contentPadding * 2, inputAreaHeight);
        rt.FillRectangle(_inputDisplayRect, _inputBackgroundBrush);
        rt.DrawRectangle(_inputDisplayRect, _borderBrush, 1.0f);

        var currentGuessedString = _guessedCharsDisplay?.ToString() ?? "";

        // Font size adjustment logic
        if (!string.IsNullOrEmpty(currentGuessedString) && _inputDisplayRect.Width > 0)
            using (var textLayout = _dwriteFactory.CreateTextLayout(currentGuessedString, _textFormatInput,
                       _inputDisplayRect.Width, _inputDisplayRect.Height))
            {
                if (textLayout.Metrics.LayoutWidth > _inputDisplayRect.Width)
                {
                    var newFontSize = _textFormatInput.FontSize *
                                      (_inputDisplayRect.Width / textLayout.Metrics.LayoutWidth) * 0.9f;
                    if (newFontSize < 1) newFontSize = 1;

                    var oldFormat = _textFormatInput;
                    _textFormatInput = _dwriteFactory.CreateTextFormat(oldFormat.FontFamilyName, oldFormat.FontWeight,
                        oldFormat.FontStyle, oldFormat.FontStretch, Math.Max(10, newFontSize));
                    _textFormatInput.TextAlignment = TextAlignment.Center;
                    _textFormatInput.ParagraphAlignment = ParagraphAlignment.Center;
                    _textFormatInput.WordWrapping = WordWrapping.NoWrap;
                    oldFormat.Dispose();
                }
            }

        var stringToDrawOnScreen = " "; // Default to a single space
        if (!string.IsNullOrEmpty(currentGuessedString))
            stringToDrawOnScreen = string.Join(" ", currentGuessedString.ToCharArray());
        DrawCenteredText(rt, stringToDrawOnScreen, _inputDisplayRect, _textFormatInput);


        var skipButtonTopY = _inputDisplayRect.Bottom + spacing * 2;
        _skipButtonRect = new RectangleF(Width / 2f - 60f, skipButtonTopY, 120f, buttonHeight);
        rt.FillRectangle(_skipButtonRect, _skipButtonBrush);
        rt.DrawRectangle(_skipButtonRect, _borderBrush, 1.0f);
        DrawCenteredText(rt, "KIHAGY", _skipButtonRect, _textFormatButton);
    }


    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        CreateRenderTarget();
        Invalidate();
    }

    private void Widget_MouseDown(object sender, MouseEventArgs e)
    {
        if (Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
        {
            if (e.Button == MouseButtons.Right && _isMinimized)
            {
                var dragRect = new RectangleF(0, 0, Width, MinimizeButtonSize);
                if (dragRect.Contains(e.Location) )
                {
                    _isDragging = true;
                    _mouseDownLocation = e.Location;
                }
                return;
            }

            if (!_isMinimized && e.Button == MouseButtons.Left && !_minimizeButtonRect.Contains(e.Location) && !_skipButtonRect.Contains(e.Location))
            {
                _isDragging = true;
                _mouseDownLocation = e.Location;
                return;
            }
        }
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
        if (e.Button == MouseButtons.Left && _isMinimized)
            if (_startButtonRect.Contains(e.Location))
            {
                ExpandToFullView();
                return;
            }

        if (!_isMinimized && _minimizeButtonRect.Contains(e.Location))
        {
            MinimizeToButton();
            return;
        }

        if (_gameState == GameState.ShowingQuestion && _skipButtonRect.Contains(e.Location))
        {
            HandleSkip();
            return;
        }

        if (_isDragging)
        {
            _isDragging = false;
            Beallitasok.WidgetSection["Logo_X"].IntValue = Left;
            Beallitasok.WidgetSection["Logo_Y"].IntValue = Top;
            Beallitasok.ConfigParser.SaveToFile(Path.Combine(Beallitasok.BasePath, Beallitasok.SetttingsFileName));
            return;
        }
    }

    private void Widget_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (_isMinimized || _gameState != GameState.ShowingQuestion || _showFullLogo || _currentLogo == null)
            return;

        var keyPressed = char.ToUpperInvariant(e.KeyChar);
        // Allow letters, digits, and spaces
        if (!char.IsLetterOrDigit(keyPressed) && !(keyPressed == ' ' && _currentLogoNameNormalized.Contains(' ')))
            return;

        if (_alreadyGuessedChars.Contains(keyPressed))
            return;

        _alreadyGuessedChars.Add(keyPressed);
        var found = false;
        for (var i = 0; i < _currentLogoNameNormalized.Length; i++)
            if (_currentLogoNameNormalized[i] == keyPressed)
            {
                // Use original casing for display from the original LogoName, not normalized one
                _guessedCharsDisplay[i] = _currentLogo.LogoName[i];
                found = true;
            }

        if (found)
        {
            // Check if all non-space characters are revealed
            var allRevealed = true;
            var currentDisplay = _guessedCharsDisplay.ToString();
            for (var i = 0; i < currentDisplay.Length; i++)
                if (_currentLogoNameNormalized[i] != ' ' && currentDisplay[i] == '_')
                {
                    allRevealed = false;
                    break;
                }

            if (allRevealed)
            {
                _correctlyGuessedLogosCount++;
                _gameState = GameState.ShowingResult;
                LoadLogoImage(_currentLogo.FullFilename, true);
                _nextRoundTimer.Start();
            }
        }

        Invalidate();
    }


    private void HandleSkip()
    {
        if (_currentLogo == null) return; // If there's no current logo, do nothing

        _skippedLogosCount++;
        _gameState = GameState.ShowingResult;

        if (_guessedCharsDisplay != null && _currentLogo.LogoName != null && _currentLogoNameNormalized != null)
            for (var i = 0; i < _currentLogoNameNormalized.Length; i++)
                if (_currentLogoNameNormalized[i] != ' ')
                {
                    if (i < _currentLogo.LogoName.Length)
                        _guessedCharsDisplay[i] = _currentLogo.LogoName[i];
                    else
                        _guessedCharsDisplay[i] = _currentLogoNameNormalized[i];
                }

        // Load the full version of the logo image to show the visual answer
        LoadLogoImage(_currentLogo.FullFilename, true);

        // Start the timer for the next round
        _nextRoundTimer.Start();

        // Request a repaint to show the changes
        Invalidate();
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
            _currentLogoBitmap?.Dispose();
            _textBrush?.Dispose();
            _buttonBrush?.Dispose();
            _inputBackgroundBrush?.Dispose();
            _borderBrush?.Dispose();
            _startButtonBrush?.Dispose();
            _minimizeButtonBrush?.Dispose();
            _skipButtonBrush?.Dispose();
            _textFormatInput?.Dispose();
            _textFormatScore?.Dispose();
            _textFormatButton?.Dispose();
            _renderTarget?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void DrawMinimizedScreen(ID2D1RenderTarget rt)
    {
        BackColor = Color.FromArgb(45, 45, 48);
        TransparencyKey = Color.Magenta;
        rt.Clear(ToColor4(BackColor));

        _startButtonRect = new RectangleF(0, 0, Width, Height);
        rt.FillRectangle(_startButtonRect, _startButtonBrush);
        rt.DrawRectangle(_startButtonRect, _borderBrush, 1.5f);
        DrawCenteredText(rt, "Logók", _startButtonRect, _textFormatButton);
    }

    private void ExpandToFullView()
    {
        _isMinimized = false;
        Width = 400;
        Height = 550;
        BackColor = Color.FromArgb(45, 45, 48);
        TransparencyKey = Color.Magenta;
        _gameState = GameState.Initializing;
        Invalidate();
        Application.DoEvents();
        StartNewRound();
    }

    private void MinimizeToButton()
    {
        _isMinimized = true;
        _gameState = GameState.Minimized;
        Width = 160;
        Height = 40;
        BackColor = Color.FromArgb(45, 45, 48);
        TransparencyKey = Color.Magenta;
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