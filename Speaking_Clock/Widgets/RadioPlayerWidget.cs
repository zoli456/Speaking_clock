using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Numerics;
using Speaking_Clock;
using Vanara.PInvoke;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Vortice.WinForms;
using Color = System.Drawing.Color;
using FontStyle = Vortice.DirectWrite.FontStyle;
using Size = System.Drawing.Size;
using Timer = System.Windows.Forms.Timer;

namespace Speaking_clock.Widgets;

public class RadioPlayerWidget : RenderForm
{
    private readonly ContextMenuStrip _stationMenu;
    private readonly Timer _timer;
    private string _currentStation = "Nincs állomás kiválasztva";
    private Point _dragStartPoint;
    private bool _isDragging;
    internal bool _isPlaying;
    private ID2D1HwndRenderTarget _renderTarget;
    private float _scale = 1.0f;
    private ID2D1SolidColorBrush _textBrush;
    private float _volume = 0.05f; // Default volume: 5%
    private string currentRadioName, currentRadioURL;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RadioPlayerWidget" /> class.
    /// </summary>
    /// <param name="startX"></param>
    /// <param name="startY"></param>
    /// <param name="scale"></param>
    public RadioPlayerWidget(int startX, int startY, float scale = 1f)
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        AllowTransparency = true;
        BackColor = Color.Transparent;

        Text = "Radio Player";
        FormBorderStyle = FormBorderStyle.None;
        Size = new Size(300, 125);
        ShowInTaskbar = false;
        Opacity = 0.9f;
        Region = CreateRoundedRectangleRegion(Size.Width, Size.Height, 20);
        _scale = scale;

        StartPosition = FormStartPosition.Manual;
        Location = new Point(startX, startY);

        // Initialize station selection menu
        _stationMenu = new ContextMenuStrip();
        PopulateStationMenu();

        MouseDown += RadioPlayer_MouseDown;
        MouseMove += RadioPlayer_MouseMove;
        MouseUp += RadioPlayer_MouseUp;
        DoubleClick += RadioPlayer_DoubleClick;
        MouseWheel += RadioPlayer_MouseWheel;
        Closed += RadioPlayer_Closed;

        // Set up a timer for updating volume
        _timer = new Timer { Interval = 5000 };
        _timer.Tick += (s, e) =>
        {
            if (Beallitasok.RádióSection["Hangerő"].IntValue != (int)(_volume * 100))
            {
                Debug.WriteLine("Hangerő frissítés");
                Beallitasok.RádióSection["Hangerő"].IntValue = (int)(_volume * 100);
                Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
            }
        };
        _timer.Start();

        DoubleBuffered = false;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);


        Show();
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

    public float GetScaleFactor()
    {
        return _scale;
    }

    public void SetScaleFactor(float value)
    {
        if (value > 0 && value != _scale)
        {
            _scale = value;
            CreateRenderTarget();
            Invalidate();
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (Beallitasok.RádióSection["Hangerő"].IntValue == -1)
        {
            Beallitasok.RádióSection["Hangerő"].IntValue = Beallitasok.BeszédSection["Hangerő"].IntValue;
            Beallitasok.RadioVolume = (float)(double)Beallitasok.RádióSection["Hangerő"].IntValue / 100;
            Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
            _volume = (float)(double)Beallitasok.RádióSection["Hangerő"].IntValue / 100;
        }
        else
        {
            _volume = (float)(double)Beallitasok.RádióSection["Hangerő"].IntValue / 100;
        }

        CreateRenderTarget();
    }

    private void CreateRenderTarget()
    {
        _renderTarget?.Dispose();
        _renderTarget = GraphicsFactories.D2DFactory.CreateHwndRenderTarget(new RenderTargetProperties(),
            new HwndRenderTargetProperties
            {
                Hwnd = Handle,
                PixelSize = new SizeI(Width, Height),
                PresentOptions = PresentOptions.None
            });

        _textBrush?.Dispose();
        _textBrush = _renderTarget.CreateSolidColorBrush(new Color4(1.0f, 1.0f, 1.0f));
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

        using var textFormat = GraphicsFactories.DWriteFactory.CreateTextFormat(
            "Arial",
            FontWeight.Normal,
            FontStyle.Normal,
            FontStretch.Normal,
            18 * _scale
        );

        using var textLayout = GraphicsFactories.DWriteFactory.CreateTextLayout(
            $"Állomás: {_currentStation}\n" +
            $"Állapot: {(_isPlaying ? "Lejátszás" : "Szünet")}\n" +
            $"Hangerő: {Math.Round(_volume * 100)}%",
            textFormat,
            Size.Width,
            Size.Height
        );
        textLayout.TextAlignment = TextAlignment.Center;
        textLayout.ParagraphAlignment = ParagraphAlignment.Center;

        _renderTarget.DrawTextLayout(new Vector2(0, 0), textLayout, _textBrush);

        _renderTarget.EndDraw();
    }

    private void RadioPlayer_DoubleClick(object sender, EventArgs e)
    {
        if (_isPlaying)
            PauseRadio();
        else
            ResumeRadio();
    }

    private void RadioPlayer_MouseWheel(object sender, MouseEventArgs e)
    {
        AdjustVolume(e.Delta > 0 ? 0.05f : -0.05f);
    }

    private void AdjustVolume(float delta)
    {
        _volume = Math.Clamp(_volume + delta, 0.0f, 1.0f);
        Debug.WriteLine($"Volume adjusted to {_volume * 100}%");
        Invalidate();
        OnlineRadioPlayer.SetVolume(_volume);
    }

    private async Task PlayRadio(string stationUrl, string stationName)
    {
        while (Beallitasok.Lejátszás) await Task.Delay(100);
        try
        {
            _currentStation = stationName;
            currentRadioName = stationName;
            currentRadioURL = stationUrl;

            Beallitasok.PlayingRadio = true;
            Beallitasok.SafeInvoke(Beallitasok.SayItNowbutton, () => { Beallitasok.SayItNowbutton.Enabled = false; });
            OnlineRadioPlayer.PlayStreamAsync(stationUrl, _volume);

            _isPlaying = true;
            Invalidate();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error playing station: {ex.Message}");
            _currentStation = "Error Playing Station";
            Invalidate();
        }
    }

    private void PauseRadio()
    {
        if (string.IsNullOrEmpty(currentRadioName) || string.IsNullOrEmpty(currentRadioURL)) return;

        if (_isPlaying)
        {
            _isPlaying = false;
            Invalidate();
            OnlineRadioPlayer.Stop();
            Beallitasok.SafeInvoke(Beallitasok.SayItNowbutton, () => { Beallitasok.SayItNowbutton.Enabled = true; });
            Beallitasok.PlayingRadio = false;
        }
    }

    private void ResumeRadio()
    {
        if (string.IsNullOrEmpty(currentRadioName) || string.IsNullOrEmpty(currentRadioURL)) return;
        if (Beallitasok.PlayingRadio) return;
        _isPlaying = true;
        Invalidate();
        OnlineRadioPlayer.PlayStreamAsync(currentRadioURL, _volume);
        Beallitasok.SafeInvoke(Beallitasok.SayItNowbutton, () => { Beallitasok.SayItNowbutton.Enabled = false; });
        Beallitasok.PlayingRadio = true;
    }

    private async Task PopulateStationMenu()
    {
        _stationMenu.Items.Clear();

        while (Beallitasok.RadioNames.Count == 0) await Task.Delay(10);

        if (Beallitasok.RadioNames.Count != Beallitasok.RandioUrLs.Count)
        {
            Debug.WriteLine("Error: Station names and URLs lists must have the same number of elements.");
            return;
        }

        for (var i = 0; i < Beallitasok.RadioNames.Count; i++)
        {
            var name = Beallitasok.RadioNames[i];
            var url = Beallitasok.RandioUrLs[i];

            var item = new ToolStripMenuItem(name)
            {
                Tag = url // Store the URL in the item's tag
            };

            item.Click += (s, e) => PlayRadio((string)((ToolStripMenuItem)s).Tag, name);
            _stationMenu.Items.Add(item);
        }
    }


    private void RadioPlayer_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
        {
            _isDragging = true;
            _dragStartPoint = e.Location;
        }
        else if (e.Button == MouseButtons.Right)
        {
            _stationMenu.Show(this, e.Location);
        }
    }

    private void RadioPlayer_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
            Location = new Point(
                Location.X + e.X - _dragStartPoint.X,
                Location.Y + e.Y - _dragStartPoint.Y
            );
    }

    private void RadioPlayer_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = false;
            Beallitasok.WidgetSection["Rádió_X"].IntValue = Left;
            Beallitasok.WidgetSection["Rádió_Y"].IntValue = Top;
            Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
        }
    }

    private void RadioPlayer_Closed(object? sender, EventArgs e)
    {
        _textBrush?.Dispose();
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _textBrush?.Dispose();
            _renderTarget?.Dispose();
            _timer?.Dispose();
        }

        base.Dispose(disposing);
    }
}