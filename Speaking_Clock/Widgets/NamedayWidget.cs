using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Text.Json;
using Speaking_Clock;
using Vanara.PInvoke;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.WinForms;
using AlphaMode = Vortice.DCommon.AlphaMode;
using Color = System.Drawing.Color;
using FontStyle = Vortice.DirectWrite.FontStyle;
using Size = System.Drawing.Size;
using Timer = System.Windows.Forms.Timer;

namespace Speaking_clock.Widgets;

public class NamedayWidget : RenderForm
{
    private readonly Timer _timer;
    private ID2D1SolidColorBrush _handBrush;
    private bool _isDragging;
    private Point _mouseDownLocation;
    private ID2D1HwndRenderTarget _renderTarget;
    private float _scale = 1.0f;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NamedayWidget" /> class.
    /// </summary>
    /// <param name="startX"></param>
    /// <param name="startY"></param>
    /// <param name="scale"></param>
    public NamedayWidget(int startX, int startY, float scale = 1f)
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        AllowTransparency = true;
        BackColor = Color.Transparent;
        //TransparencyKey = Color.Empty;
        // Initialize the form
        Text = "Nameday";
        FormBorderStyle = FormBorderStyle.None;
        Size = new Size(225, 100);
        ShowInTaskbar = false;
        Opacity = 0.8f;
        Region = CreateRoundedRectangleRegion(Size.Width, Size.Height, 20);
        _scale = scale;

        // Set starting position
        StartPosition = FormStartPosition.Manual;
        Location = new Point(startX, startY);

        // Set up a timer for updating clock
        _timer = new Timer { Interval = 60000 };
        _timer.Tick += (s, e) =>
        {
            if (Beallitasok.LastNamedayIndex != DateTime.Now.Day) UpdateNamedays();
        };
        _timer.Start();

        DoubleBuffered = false;

        // Enable drag
        MouseDown += Nameday_MouseDown;
        MouseMove += Nameday_MouseMove;
        MouseUp += Nameday_MouseUp;
        Closed += Nameday_Closed;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        if (string.IsNullOrEmpty(Beallitasok.NameDays) || Beallitasok.LastNamedayIndex != DateTime.Now.Day)
            UpdateNamedays();

        Show();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float ScaleFactor
    {
        get => _scale;
        set
        {
            if (value > 0 && value != _scale)
            {
                _scale = value;
                CreateRenderTarget(); // Recreate render target for new size
                Invalidate(); // Redraw with updated scale
            }
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= (int)User32.WindowStylesEx.WS_EX_TOOLWINDOW;
            return cp;
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

        // Draw text inside the rectangle
        using var textFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(
            "Arial",
            FontWeight.Normal,
            FontStyle.Normal,
            FontStretch.Normal,
            20 * _scale
        );

        using var textLayout =
            GraphicsFactories.DWriteFactory.CreateTextLayout($"Mai névnap:\n{Beallitasok.NameDays}", textFormat,
                Size.Width, Size.Height);
        textLayout.TextAlignment = TextAlignment.Center;
        textLayout.ParagraphAlignment = ParagraphAlignment.Center;


        _renderTarget.DrawTextLayout(new Vector2(0, 0), textLayout, _handBrush);

        _renderTarget.EndDraw();
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _handBrush?.Dispose();
            _renderTarget?.Dispose();
            _timer?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Nameday_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
        {
            _isDragging = true;
            _mouseDownLocation = e.Location;
        }
    }

    private void Nameday_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            Left += e.X - _mouseDownLocation.X;
            Top += e.Y - _mouseDownLocation.Y;
        }
    }

    private void Nameday_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = false;
            Beallitasok.WidgetSection["Névnap_X"].IntValue = Left;
            Beallitasok.WidgetSection["Névnap_Y"].IntValue = Top;
            Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
        }
    }

    private void Nameday_Closed(object? sender, EventArgs e)
    {
        _handBrush?.Dispose();
        _renderTarget?.Dispose();
        _timer?.Dispose();
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

    private async Task UpdateNamedays()
    {
        try
        {
            var root = JsonDocument.Parse(await DataServices.GetNamedaysAsync()).RootElement;

            if (!root.TryGetProperty("nev1", out var nev1))
            {
                Debug.WriteLine("Error: 'nev1' key is missing in the response.");
                return;
            }

            Beallitasok.NameDays = string.Join(", ", nev1.EnumerateArray().Select(n => n.GetString()));
            Beallitasok.LastNamedayIndex = DateTime.Now.Day;
            Invalidate();
            Debug.WriteLine("Névnapok widget frissítve!");
        }
        catch (Exception e)
        {
        }
    }
}