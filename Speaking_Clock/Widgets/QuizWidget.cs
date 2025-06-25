using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Speaking_clock.Widgets;
using Vanara.PInvoke;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Vortice.WinForms;
using Color = System.Drawing.Color;
using FontStyle = Vortice.DirectWrite.FontStyle;
using Timer = System.Windows.Forms.Timer;


namespace Speaking_Clock.Widgets;

public class QuizItem
{
    //[JsonPropertyName("Sorszam")] public string Sorszam { get; set; }
    [JsonPropertyName("Kerdes")] public string Kerdes { get; set; }
    [JsonPropertyName("A")] public string A { get; set; }
    [JsonPropertyName("B")] public string B { get; set; }
    [JsonPropertyName("C")] public string C { get; set; }
    [JsonPropertyName("D")] public string D { get; set; }

    [JsonPropertyName("Valasz")] public string Valasz { get; set; } // Should be "A", "B", "C", or "D"
    //[JsonPropertyName("Kategoria")] public string Kategoria { get; set; }
}

public class QuizWidget : RenderForm
{
    private const int NumOptions = 4;
    private const float MinimizeButtonSize = 30f;
    private const float MinimizeButtonPadding = 5f;
    private readonly List<string> _currentOptionTexts = new();
    private readonly Timer _nextRoundTimer;

    private readonly RectangleF[] _optionRects;
    private readonly Random _random = new((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    private List<QuizItem> _allQuestions;
    private ID2D1SolidColorBrush _borderBrush;
    private ID2D1SolidColorBrush _buttonBrush;

    private int _correctAnswersCount;
    private ID2D1SolidColorBrush _correctBrush;
    private QuizItem _currentQuestion;

    private IDWriteFactory _dwriteFactory;
    private GameState _gameState;
    private int _incorrectAnswersCount;
    private ID2D1SolidColorBrush _incorrectBrush;
    private bool _isDragging;
    private bool _isMinimized = true;
    private ID2D1SolidColorBrush _minimizeButtonBrush;
    private RectangleF _minimizeButtonRect;
    private Point _mouseDownLocation;
    private ID2D1SolidColorBrush _questionBackgroundBrush;
    private ID2D1HwndRenderTarget _renderTarget;
    private int _selectedIndex = -1;

    private bool _showResultFeedback;

    private ID2D1SolidColorBrush _startButtonBrush;
    private RectangleF _startButtonRect;
    private ID2D1SolidColorBrush _textBrush;
    private IDWriteTextFormat _textFormatOptions;
    private IDWriteTextFormat _textFormatQuestion;
    private IDWriteTextFormat _textFormatScore;

    public QuizWidget(int startX, int startY)
    {
        Opacity = 1;
        Text = "Quiz Widget";
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

        _optionRects = new RectangleF[NumOptions];
        _gameState = GameState.Minimized;

        _nextRoundTimer = new Timer { Interval = 4000 };
        _nextRoundTimer.Tick += (s, e) =>
        {
            _nextRoundTimer.Stop();
            StartNewRound();
        };

        Show();
        LoadQuizDataAsync();
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
        CreateDeviceIndependentResources();
        CreateRenderTarget();
    }

    private void CreateDeviceIndependentResources()
    {
        _textFormatQuestion?.Dispose();
        _textFormatQuestion =
            _dwriteFactory.CreateTextFormat("Segoe UI", FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 18);
        _textFormatQuestion.TextAlignment = TextAlignment.Leading;
        _textFormatQuestion.ParagraphAlignment = ParagraphAlignment.Near;
        _textFormatQuestion.WordWrapping = WordWrapping.Wrap;

        _textFormatOptions?.Dispose();
        _textFormatOptions =
            _dwriteFactory.CreateTextFormat("Segoe UI", FontWeight.SemiBold, FontStyle.Normal, FontStretch.Normal,
                16);
        _textFormatOptions.TextAlignment = TextAlignment.Center;
        _textFormatOptions.ParagraphAlignment = ParagraphAlignment.Center;
        _textFormatOptions.WordWrapping = WordWrapping.Wrap;

        _textFormatScore?.Dispose();
        _textFormatScore =
            _dwriteFactory.CreateTextFormat("Segoe UI", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal,
                14);
        _textFormatScore.TextAlignment = TextAlignment.Center;
        _textFormatScore.ParagraphAlignment = ParagraphAlignment.Center;
    }

    private void CreateRenderTarget()
    {
        _renderTarget?.Dispose();

        _renderTarget = GraphicsFactories.D2DFactory.CreateHwndRenderTarget(
            new RenderTargetProperties(),
            new HwndRenderTargetProperties
            {
                Hwnd = Handle,
                PixelSize = new SizeI(Width, Height),
                PresentOptions = PresentOptions.None
            });
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
        _minimizeButtonBrush?.Dispose();
        _questionBackgroundBrush?.Dispose();

        if (_renderTarget == null) return;

        _textBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.White.ToArgb()));
        _buttonBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 60, 60, 65).ToArgb()));
        _correctBrush =
            _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 34, 139, 34)
                .ToArgb())); // ForestGreen
        _incorrectBrush =
            _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 178, 34, 34).ToArgb())); // Firebrick
        _borderBrush = _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 90, 90, 95).ToArgb()));
        _startButtonBrush =
            _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 70, 70, 75).ToArgb()));
        _minimizeButtonBrush =
            _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 80, 80, 85).ToArgb()));
        _questionBackgroundBrush =
            _renderTarget.CreateSolidColorBrush(new Color4(Color.FromArgb(255, 45, 45, 50)
                .ToArgb()));
    }

    private async void LoadQuizDataAsync()
    {
        try
        {
            var resourceName = "Speaking_clock.QuestionData.json";

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                               ?? throw new FileNotFoundException($"Could not load embedded resource: {resourceName}");

            string jsonText;
            using (var rdr = new StreamReader(stream))
            {
                jsonText = await rdr.ReadToEndAsync();
            }

            _allQuestions = JsonSerializer.Deserialize<List<QuizItem>>(
                jsonText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (_allQuestions == null || _allQuestions.Count == 0)
            {
                MessageBox.Show(
                    "No quiz questions found or error in loading data. Check QuestionData.json.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _gameState = GameState.Error;
                Invalidate();
                return;
            }

            _allQuestions = _allQuestions.Where(q =>
                !string.IsNullOrWhiteSpace(q.Kerdes) &&
                !string.IsNullOrWhiteSpace(q.A) &&
                !string.IsNullOrWhiteSpace(q.B) &&
                !string.IsNullOrWhiteSpace(q.C) &&
                !string.IsNullOrWhiteSpace(q.D) &&
                !string.IsNullOrWhiteSpace(q.Valasz) &&
                new[] { "A", "B", "C", "D" }.Contains(q.Valasz.ToUpper())
            ).ToList();

            if (_allQuestions.Count == 0)
            {
                MessageBox.Show(
                    "Not enough valid question data (missing question, options, or valid answer) to start the game.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _gameState = GameState.Error;
                Invalidate();
                return;
            }

            if (!_isMinimized)
            {
                StartNewRound();
            }
            else
            {
                _gameState = GameState.Minimized; // Or Initializing if you want to show loading briefly
                Invalidate();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading quiz data: {ex.Message}\n{ex.StackTrace}", "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _gameState = GameState.Error;
            Invalidate();
        }
    }

    private void StartNewRound()
    {
        if (_allQuestions == null || _allQuestions.Count == 0)
        {
            // Attempt to reload data if it's missing and we are trying to start a round
            if (_gameState != GameState.Error && _gameState != GameState.Initializing)
                LoadQuizDataAsync(); // This will set to Error if it fails again
            else if
                (_gameState != GameState.Error) _gameState = GameState.Error; // If already tried loading and failed
            Invalidate();
            return;
        }

        _showResultFeedback = false;
        _selectedIndex = -1;

        _currentOptionTexts.Clear();

        _currentQuestion = _allQuestions[_random.Next(_allQuestions.Count)];

        _currentOptionTexts.Add(_currentQuestion.A);
        _currentOptionTexts.Add(_currentQuestion.B);
        _currentOptionTexts.Add(_currentQuestion.C);
        _currentOptionTexts.Add(_currentQuestion.D);

        _gameState = GameState.ShowingQuestion;
        Invalidate();
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
                    DrawCenteredText(_renderTarget, "Initializing Quiz...", ClientRectangle, _textFormatOptions);
                    break;
                case GameState.Error:
                    DrawCenteredText(_renderTarget, "An error occurred. Please check data.", ClientRectangle,
                        _textFormatOptions);
                    break;
                case GameState.ShowingQuestion:
                case GameState.ShowingResult:
                    DrawGameScreen(_renderTarget);
                    break;
            }

        _renderTarget.EndDraw();
    }

    private void DrawTextInRect(ID2D1RenderTarget rt, string text, RectangleF layoutRect, IDWriteTextFormat format,
        ID2D1Brush brush)
    {
        if (format != null && brush != null && !string.IsNullOrEmpty(text))
            rt.DrawText(text, format, (Rect)layoutRect, brush);
    }

    private void DrawCenteredText(ID2D1RenderTarget rt, string text, RectangleF bounds, IDWriteTextFormat format)
    {
        if (format != null && _textBrush != null && !string.IsNullOrEmpty(text))
            rt.DrawText(text, format, (Rect)bounds, _textBrush);
    }


    private void DrawGameScreen(ID2D1RenderTarget rt)
    {
        _minimizeButtonRect = new RectangleF(
            Width - MinimizeButtonSize - MinimizeButtonPadding,
            MinimizeButtonPadding,
            MinimizeButtonSize,
            MinimizeButtonSize);

        rt.FillRectangle(_minimizeButtonRect, _minimizeButtonBrush);
        rt.DrawRectangle(_minimizeButtonRect, _borderBrush, 1.5f);
        var minusLine = new RectangleF(
            _minimizeButtonRect.Left + 8,
            _minimizeButtonRect.Top + _minimizeButtonRect.Height / 2 - 1,
            _minimizeButtonRect.Width - 16,
            3);
        rt.FillRectangle(minusLine, _textBrush);

        var scoreDisplayHeight = 30f;
        var topMargin = MinimizeButtonSize + MinimizeButtonPadding * 2;
        var spacing = 10f;
        var questionAreaPadding = Width * 0.05f;

        var scoreText = $"Helyes: {_correctAnswersCount}    Hibás: {_incorrectAnswersCount}";
        var scoreRect = new RectangleF(questionAreaPadding, topMargin, Width - questionAreaPadding * 2,
            scoreDisplayHeight);
        DrawTextInRect(rt, scoreText, scoreRect, _textFormatScore, _textBrush);

        // Draw Question Text
        var questionTopY = topMargin + scoreDisplayHeight + spacing;
        var questionHeight = Height * 0.25f;
        var questionBackgroundRect = new RectangleF(questionAreaPadding, questionTopY,
            Width - questionAreaPadding * 2, questionHeight);

        // Fill background for the question area
        if (_questionBackgroundBrush != null) rt.FillRectangle(questionBackgroundRect, _questionBackgroundBrush);
        // Draw border for the question background
        if (_borderBrush != null) rt.DrawRectangle(questionBackgroundRect, _borderBrush, 1.5f);

        // Create a slightly smaller rect for the text to give padding inside the background
        var textPadding = 5f; // Internal padding of 5 pixels
        var questionTextLayoutRect = new RectangleF(
            questionBackgroundRect.Left + textPadding,
            questionBackgroundRect.Top + textPadding,
            questionBackgroundRect.Width - textPadding * 2,
            questionBackgroundRect.Height - textPadding * 2
        );

        if (_currentQuestion != null)
            DrawTextInRect(rt, _currentQuestion.Kerdes, questionTextLayoutRect, _textFormatQuestion, _textBrush);
        else
            DrawTextInRect(rt, "Loading question...", questionTextLayoutRect, _textFormatQuestion, _textBrush);


        // Draw Options
        var buttonsTopY =
            questionBackgroundRect.Bottom + spacing * 1.5f;
        var availableHeightForButtons = Height - buttonsTopY - spacing - MinimizeButtonPadding;
        var buttonHeight = (availableHeightForButtons - (NumOptions - 1) * spacing) / NumOptions;
        buttonHeight = Math.Max(35f, Math.Min(buttonHeight, 60f));


        var currentY = buttonsTopY;
        for (var i = 0; i < NumOptions; i++)
        {
            _optionRects[i] =
                new RectangleF(questionAreaPadding, currentY, Width - questionAreaPadding * 2, buttonHeight);
            var currentOptionBrush = _buttonBrush;

            if (_showResultFeedback && _currentQuestion != null)
            {
                var correctVal = _currentQuestion.Valasz.ToUpper();
                var optionLetter = ((char)('A' + i)).ToString();

                if (optionLetter == correctVal)
                    currentOptionBrush = _correctBrush;
                else if (i == _selectedIndex)
                    currentOptionBrush = _incorrectBrush;
            }

            if (currentOptionBrush != null)
                rt.FillRectangle(_optionRects[i], currentOptionBrush);
            if (_borderBrush != null)
                rt.DrawRectangle(_optionRects[i], _borderBrush, 1.5f);

            var optionText = _currentOptionTexts.Count > i ? _currentOptionTexts[i] : "N/A";
            optionText = $"{(char)('A' + i)}) {optionText}";
            DrawTextInRect(rt, optionText, _optionRects[i], _textFormatOptions, _textBrush);
            currentY += buttonHeight + spacing;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (IsHandleCreated && ClientRectangle.Width > 0 && ClientRectangle.Height > 0) CreateRenderTarget();
        Invalidate();
    }

    private void Widget_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && !_isMinimized && _minimizeButtonRect.Contains(e.Location))
            // Click on minimize button, don't start drag.
            return;

        var canDrag = Beallitasok.RSS_Reader_Section?["Húzás"]?.BoolValue ?? true;
        if (!canDrag) return;

        if (_isMinimized && e.Button == MouseButtons.Right) // Drag minimized with Right Mouse
        {
            _isDragging = true;
            _mouseDownLocation = e.Location;
        }
        else if (!_isMinimized && e.Button == MouseButtons.Left) // Drag expanded with Left Mouse on specific areas
        {
            // Allow dragging by clicking the question area or score area
            var draggableAreaTop = MinimizeButtonSize + MinimizeButtonPadding * 2;
            var draggableHeight = Height * 0.25f + 30f + 10f; // Approx height of score + question + spacing
            var questionAreaRect =
                GetQuestionAreaRect();

            if (questionAreaRect.Contains(e.Location) ||
                new RectangleF(0, 0, Width, draggableAreaTop + draggableHeight)
                    .Contains(e.Location)) // Broader drag area
            {
                _isDragging = true;
                _mouseDownLocation = e.Location;
            }
        }
    }

    private RectangleF GetQuestionAreaRect()
    {
        var topMargin = MinimizeButtonSize + MinimizeButtonPadding * 2;
        var scoreDisplayHeight = 30f;
        var spacing = 10f;
        var questionAreaPadding = Width * 0.05f;
        var questionTopY = topMargin + scoreDisplayHeight + spacing;
        var questionHeight = Height * 0.25f; 
        return new RectangleF(questionAreaPadding, questionTopY, Width - questionAreaPadding * 2, questionHeight); // The background rectangle
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
            if (Beallitasok.WidgetSection != null)
            {
                Beallitasok.WidgetSection["Quiz_X"].IntValue = Left;
                Beallitasok.WidgetSection["Quiz_Y"].IntValue = Top;
                Beallitasok.ConfigParser?.SaveToFile(Path.Combine(Beallitasok.BasePath, Beallitasok.SetttingsFileName));
            }

            return;
        }

        if (_isMinimized)
        {
            if (e.Button == MouseButtons.Left && _startButtonRect.Contains(e.X, e.Y)) ExpandToFullView();
        }
        else // Not minimized
        {
            if (e.Button == MouseButtons.Left && _minimizeButtonRect.Contains(e.X, e.Y))
            {
                MinimizeToButton();
                return;
            }

            if (_gameState == GameState.ShowingQuestion && e.Button == MouseButtons.Left)
                for (var i = 0; i < NumOptions; i++)
                    if (_optionRects[i].Contains(e.X, e.Y))
                    {
                        _selectedIndex = i;
                        _showResultFeedback = true;
                        _gameState = GameState.ShowingResult;

                        var selectedAnswerLetter = ((char)('A' + i)).ToString();
                        if (_currentQuestion != null && selectedAnswerLetter == _currentQuestion.Valasz.ToUpper())
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
            _textBrush?.Dispose();
            _buttonBrush?.Dispose();
            _correctBrush?.Dispose();
            _incorrectBrush?.Dispose();
            _borderBrush?.Dispose();
            _minimizeButtonBrush?.Dispose();
            _startButtonBrush?.Dispose();
            _questionBackgroundBrush?.Dispose();

            _textFormatQuestion?.Dispose();
            _textFormatOptions?.Dispose();
            _textFormatScore?.Dispose();

            _dwriteFactory?.Dispose();
            _renderTarget?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void DrawMinimizedScreen(ID2D1RenderTarget rt)
    {
        _startButtonRect = new RectangleF(10, 10, Width - 20, Height - 20);

        if (_startButtonBrush != null && _borderBrush != null)
        {
            rt.FillRectangle(_startButtonRect, _startButtonBrush);
            rt.DrawRectangle(_startButtonRect, _borderBrush, 1.5f);
        }


        DrawCenteredText(rt, "Quiz", _startButtonRect, _textFormatOptions);
    }

    private void ExpandToFullView()
    {
        _isMinimized = false;
        Width = 420;
        Height = 550;
        if (_allQuestions == null || (_allQuestions.Count == 0 && _gameState != GameState.Error))
        {
            _gameState = GameState.Initializing; // Show initializing while loading data
            LoadQuizDataAsync();
        }
        else if (_gameState != GameState.Error)
        {
            StartNewRound();
        }
        else
        {
            Invalidate(); // If error, just redraw in expanded error state.
        }
    }

    private void MinimizeToButton()
    {
        _isMinimized = true;
        _gameState = GameState.Minimized;
        Width = 200;
        Height = 60;
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