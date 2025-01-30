using System.Numerics;
using Speaking_Clock;
using Vanara.PInvoke;
using Vortice.Direct2D1;
using Vortice.Mathematics;
using Vortice.WinForms;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;
using Timer = System.Windows.Forms.Timer;

namespace Speaking_clock.Widgets;

public class DotMatrixClock : RenderForm
{
    private readonly ID2D1Factory1 _d2dFactory;
    private readonly float _digitSpacing = 80; // New digit spacing variable
    private readonly bool _showSeconds;
    private readonly Timer _timer;
    private ID2D1SolidColorBrush _dotBrush;
    private float _dotSize = 10;
    private float _dotSpacing = 15;
    private bool _isDragging;
    private Point _mouseDownLocation;
    private ID2D1HwndRenderTarget _renderTarget;
    private bool _showColon = true;
    /// <summary>
    /// Initializes a new instance of the <see cref="DotMatrixClock"/> class.
    /// </summary>
    /// <param name="showSeconds"></param>
    /// <param name="startX"></param>
    /// <param name="startY"></param>
    /// <param name="DotSize"></param>
    /// <param name="DotSpacing"></param>
    /// <param name="DigitSpacing"></param>
    public DotMatrixClock(bool showSeconds, int startX, int startY, int DotSize = 10, int DotSpacing = 15,
        float DigitSpacing = 80)
    {
        Opacity = 0.9f;
        // Initialize the form
        _dotSize = DotSize;
        _dotSpacing = DotSpacing;
        _digitSpacing = DigitSpacing;
        Text = "Dot Matrix Clock";
        FormBorderStyle = FormBorderStyle.None;
        //BackColor = Color.Black; // Transparent color
        StartPosition = FormStartPosition.CenterScreen;
        if (showSeconds)
            Size = new Size(720, 110);
        else
            Size = new Size(420, 110);

        ShowInTaskbar = false;

        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        AllowTransparency = true;
        BackColor = Color.FromArgb(0, 0, 0, 0);
        TransparencyKey = BackColor;

        // Set starting position
        StartPosition = FormStartPosition.Manual;
        Location = new Point(startX, startY);

        // Enable drag
        MouseDown += DotMatrixClock_MouseDown;
        MouseMove += DotMatrixClock_MouseMove;
        MouseUp += DotMatrixClock_MouseUp;
        Closed += DotMatrixClock_Closed;

        // Initialize Direct2D factory
        _d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();

        // Set up a timer for blinking
        _timer = new Timer { Interval = 1000 };
        _timer.Tick += (s, e) =>
        {
            _showColon = !_showColon;
            Invalidate();
        };
        _timer.Start();

        _showSeconds = showSeconds;

        DoubleBuffered = false;
        Show();

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }

    public float DotSize
    {
        get => _dotSize;
        set
        {
            if (value > 0)
            {
                _dotSize = value;
                Invalidate(); // Redraw the clock with the updated dot size
            }
        }
    }

    public float DotSpacing
    {
        get => _dotSpacing;
        set
        {
            if (value > 0)
            {
                _dotSpacing = value;
                Invalidate(); // Redraw the clock with the updated dot spacing
            }
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // Add layered style but avoid WS_EX_TRANSPARENT
            cp.ExStyle |= (int)User32.WindowStylesEx.WS_EX_LAYERED | (int)User32.WindowStylesEx.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    private void DotMatrixClock_Closed(object? sender, EventArgs e)
    {
        _dotBrush?.Dispose();
        _renderTarget?.Dispose();
        _d2dFactory?.Dispose();
        _timer?.Dispose();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // Set window to fully transparent
        //User32.SetLayeredWindowAttributes(Handle, 0, 0, (User32.LayeredWindowAttributes)LWA_COLORKEY);

        CreateRenderTarget();
    }

    private void CreateRenderTarget()
    {
        _renderTarget?.Dispose();
        _renderTarget = _d2dFactory.CreateHwndRenderTarget(new RenderTargetProperties(), new HwndRenderTargetProperties
        {
            Hwnd = Handle,
            PixelSize = new SizeI(Width, Height),
            PresentOptions = PresentOptions.None
        });

        _dotBrush?.Dispose();
        _dotBrush = _renderTarget.CreateSolidColorBrush(new Color4(1.0f, 1.0f, 1.0f));
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_renderTarget != null)
        {
            CreateRenderTarget();
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_renderTarget == null)
            return;

        // Begin drawing
        _renderTarget.BeginDraw();
        _renderTarget.Clear(new Color4(0, 0, 0, 0));

        // Get the current time
        var time = DateTime.Now;
        DrawDigit(time.Hour / 10, new Vector2(10, 10));
        DrawDigit(time.Hour % 10, new Vector2(10 + _digitSpacing, 10));

        if (_showColon) DrawColon(new Vector2(10 + _digitSpacing * 2, 10));

        DrawDigit(time.Minute / 10, new Vector2(10 + _digitSpacing * 2.5f, 10));
        DrawDigit(time.Minute % 10, new Vector2(10 + _digitSpacing * 3.5f, 10));

        if (_showSeconds)
        {
            if (_showColon) DrawColon(new Vector2(10 + _digitSpacing * 4.5f, 10));
            DrawDigit(time.Second / 10, new Vector2(10 + _digitSpacing * 5, 10));
            DrawDigit(time.Second % 10, new Vector2(10 + _digitSpacing * 6, 10));
        }

        // End drawing
        _renderTarget.EndDraw();
    }

    private void DrawDigit(int digit, Vector2 position)
    {
        int[,] patterns =
        {
            {
                1, 1, 1, 1, 1, // 0
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 1
            },

            {
                0, 0, 1, 0, 0, // 1
                0, 1, 1, 0, 0,
                1, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                1, 1, 1, 1, 1
            },

            {
                1, 1, 1, 1, 1, // 2
                0, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                0, 1, 1, 1, 1,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 1, 1, 1, 1
            },

            {
                1, 1, 1, 1, 1, // 3
                0, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                0, 1, 1, 1, 1,
                0, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                1, 1, 1, 1, 1
            },

            {
                1, 0, 0, 1, 0, // 4
                1, 0, 0, 1, 0,
                1, 0, 0, 1, 0,
                1, 1, 1, 1, 1,
                0, 0, 0, 1, 0,
                0, 0, 0, 1, 0,
                0, 0, 0, 1, 0
            },

            {
                1, 1, 1, 1, 1, // 5
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 1, 1, 1, 1,
                0, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                1, 1, 1, 1, 1
            },

            {
                1, 1, 1, 1, 1, // 6
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 1, 1, 1, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 1
            },

            {
                1, 1, 1, 1, 1, // 7
                0, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                0, 0, 0, 1, 0,
                0, 0, 1, 0, 0,
                0, 1, 0, 0, 0,
                1, 0, 0, 0, 0
            },

            {
                1, 1, 1, 1, 1, // 8
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 1
            },

            {
                1, 1, 1, 1, 1, // 9
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 1,
                0, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                1, 1, 1, 1, 1
            }
        };

        for (var row = 0; row < 7; row++)
        for (var col = 0; col < 5; col++)
            if (patterns[digit, row * 5 + col] == 1)
            {
                var dotPosition = position + new Vector2(col * _dotSpacing, row * _dotSpacing);
                _renderTarget.FillEllipse(new Ellipse(dotPosition, _dotSize / 2, _dotSize / 2), _dotBrush);
            }
    }

    private void DrawColon(Vector2 position)
    {
        var topDot = position + new Vector2(0, _dotSpacing);
        var bottomDot = position + new Vector2(0, _dotSpacing * 3);

        _renderTarget.FillEllipse(new Ellipse(topDot, _dotSize / 2, _dotSize / 2), _dotBrush);
        _renderTarget.FillEllipse(new Ellipse(bottomDot, _dotSize / 2, _dotSize / 2), _dotBrush);
    }

    private void DotMatrixClock_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
        {
            _isDragging = true;
            _mouseDownLocation = e.Location;
        }
    }

    private void DotMatrixClock_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            Left += e.X - _mouseDownLocation.X;
            Top += e.Y - _mouseDownLocation.Y;
        }
    }

    private void DotMatrixClock_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = false;
            Beallitasok.WidgetSection["Pontozott_X"].IntValue = Left;
            Beallitasok.WidgetSection["Pontozott_Y"].IntValue = Top;
            Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.Path}\\{Beallitasok.SetttingsFileName}");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dotBrush?.Dispose();
            _renderTarget?.Dispose();
            _d2dFactory?.Dispose();
            _timer?.Dispose();
        }

        base.Dispose(disposing);
    }
}