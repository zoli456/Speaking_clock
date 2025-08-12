using System.Numerics;
using Speaking_Clock;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.WinForms;
using static Vanara.PInvoke.User32;
using AlphaMode = Vortice.DCommon.AlphaMode;
using Color = System.Drawing.Color;
using FontStyle = Vortice.DirectWrite.FontStyle;
using Size = System.Drawing.Size;
using Timer = System.Windows.Forms.Timer;

namespace Speaking_clock.Widgets;

public class AnalogClock : RenderForm
{
    private readonly Timer _timer;
    private ID2D1SolidColorBrush _dotBrush;
    private ID2D1SolidColorBrush _handBrush;
    private bool _isDragging;
    private Point _mouseDownLocation;
    private ID2D1HwndRenderTarget _renderTarget;
    private float _scale = 1.0f; // Default scaling factor

    /// <summary>
    ///     Initializes a new instance of the <see cref="AnalogClock" /> class.
    /// </summary>
    /// <param name="startX"></param>
    /// <param name="startY"></param>
    /// <param name="scale"></param>
    public AnalogClock(int startX, int startY, float scale = 1f)
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        AllowTransparency = true;
        BackColor = Color.FromArgb(0, 0, 0, 0);
        TransparencyKey = BackColor;
        // Initialize the form
        _scale = scale;
        Text = "Analog Clock";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(420, 420);
        ShowInTaskbar = false;
        Opacity = 1f;

        // Set starting position
        StartPosition = FormStartPosition.Manual;
        Location = new Point(startX, startY);

        // Set up a timer for updating clock
        _timer = new Timer { Interval = 1000 };
        _timer.Tick += (s, e) => Invalidate();
        _timer.Start();

        DoubleBuffered = false;

        // Enable drag
        MouseDown += AnalogClock_MouseDown;
        MouseMove += AnalogClock_MouseMove;
        MouseUp += AnalogClock_MouseUp;
        Closed += AnalogClock_Closed;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        Show();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= (int)WindowStylesEx.WS_EX_LAYERED | (int)WindowStylesEx.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    public float GetScaleFactor()
    {
        return _scale;
    }

    public void SetScaleFactor(float value)
    {
        if (value > 0 && value != _scale)
        {
            _scale = value;
            CreateRenderTarget(); // Recreate render target for new size
            Invalidate(); // Redraw with updated scale
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        //User32.SetLayeredWindowAttributes(Handle, 0, 0, (User32.LayeredWindowAttributes)LWA_COLORKEY);
        CreateRenderTarget();
    }

    private void CreateRenderTarget()
    {
        _renderTarget?.Dispose();
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

        _handBrush?.Dispose();
        _handBrush = _renderTarget.CreateSolidColorBrush(new Color4(1.0f, 1.0f, 1.0f));

        _dotBrush?.Dispose();
        _dotBrush = _renderTarget.CreateSolidColorBrush(new Color4(1.0f, 0.0f, 0.0f));
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

        _renderTarget.BeginDraw();
        _renderTarget.Clear(new Color4(0, 0, 0, 0));

        // Get the current time
        var time = DateTime.Now;
        var centerX = Width / 2f;
        var centerY = Height / 2f;
        var radius = (Math.Min(Width, Height) / 2f - 20) * _scale;

        // Draw clock face
        _renderTarget.DrawEllipse(new Ellipse(new Vector2(centerX, centerY), radius, radius), _handBrush, 6 * _scale);


        // Add hour markers and numbers
        for (var i = 0; i < 12; i++)
        {
            var isMajorHour = i % 3 == 0; // Major hour for 12, 3, 6, 9
            DrawHourMarker(centerX, centerY, radius, i * 30, isMajorHour);

            if (isMajorHour)
            {
                var text = i == 0 ? "12" : i.ToString();
                DrawHourText(centerX, centerY, radius, i * 30, text);
            }
        }

        // Draw clock hands
        DrawHand(centerX, centerY, radius * 0.6f, time.Hour % 12 * 30 + time.Minute / 2.0f, 8); // Hour hand
        DrawHand(centerX, centerY, radius * 0.8f, time.Minute * 6, 5); // Minute hand
        DrawHand(centerX, centerY, radius * 0.9f, time.Second * 6); // Second hand

        // Draw center dot
        _renderTarget.FillEllipse(new Ellipse(new Vector2(centerX, centerY), 8 * _scale, 8 * _scale), _dotBrush);

        _renderTarget.EndDraw();
    }

    private void DrawHourMarker(float centerX, float centerY, float radius, float angle, bool isMajor)
    {
        var radians = (float)(Math.PI / 180.0 * (angle - 90));
        var outerRadius = radius;
        var innerRadius = radius - (isMajor ? radius * 0.1f : radius * 0.05f);

        var outerX = centerX + outerRadius * (float)Math.Cos(radians);
        var outerY = centerY + outerRadius * (float)Math.Sin(radians);
        var innerX = centerX + innerRadius * (float)Math.Cos(radians);
        var innerY = centerY + innerRadius * (float)Math.Sin(radians);

        _renderTarget.DrawLine(new Vector2(innerX, innerY), new Vector2(outerX, outerY), _handBrush,
            (isMajor ? 8 : 4) * _scale);
    }


    private void DrawHand(float x, float y, float length, float angle, float thickness = 3)
    {
        var radians = (float)(Math.PI / 180.0 * (angle - 90));
        var endX = x + length * (float)Math.Cos(radians);
        var endY = y + length * (float)Math.Sin(radians);

        _renderTarget.DrawLine(new Vector2(x, y), new Vector2(endX, endY), _handBrush, thickness * _scale * 1.5f);
    }

    private void DrawHourText(float centerX, float centerY, float radius, float angle, string text)
    {
        var radians = (float)(Math.PI / 180.0 * (angle - 90));
        var textRadius = radius - radius * 0.15f; // Use percentage of radius for positioning

        var textX = centerX + textRadius * (float)Math.Cos(radians);
        var textY = centerY + textRadius * (float)Math.Sin(radians);

        var textFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(
            "Arial",
            FontWeight.Bold,
            FontStyle.Normal,
            FontStretch.Normal,
            30 * _scale // Adjust font size based on scale
        );
        // Draw text using Direct2D's text rendering

        var layout = GraphicsFactories.DWriteFactory.CreateTextLayout(text, textFormat, 100, 50);
        var textWidth = layout.Metrics.Width;
        var textHeight = layout.Metrics.Height;
        _renderTarget.DrawText(text, textFormat,
            new Rect(textX - textWidth / 2, textY - textHeight / 2, textX + textWidth / 2, textY + textHeight / 2),
            _handBrush);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _handBrush?.Dispose();
            _dotBrush?.Dispose();
            _renderTarget?.Dispose();
            _timer?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void AnalogClock_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
        {
            _isDragging = true;
            _mouseDownLocation = e.Location;
        }
    }

    private void AnalogClock_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            Left += e.X - _mouseDownLocation.X;
            Top += e.Y - _mouseDownLocation.Y;
        }
    }

    private void AnalogClock_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = false;
            Beallitasok.WidgetSection["Analóg_X"].IntValue = Left;
            Beallitasok.WidgetSection["Analóg_Y"].IntValue = Top;
            Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
        }
    }

    private void AnalogClock_Closed(object? sender, EventArgs e)
    {
        _handBrush?.Dispose();
        _dotBrush?.Dispose();
        _renderTarget?.Dispose();
        _timer?.Dispose();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == (int)WindowMessage.WM_DISPLAYCHANGE)
            RepositionOverlay();

        base.WndProc(ref m);
    }

    private void RepositionOverlay()
    {
        Left = Beallitasok.WidgetSection["Analóg_X"].IntValue;
        Top = Beallitasok.WidgetSection["Analóg_Y"].IntValue;
    }
}