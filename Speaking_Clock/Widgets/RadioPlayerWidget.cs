using System.Diagnostics;
using System.Numerics;
using Speaking_Clock;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using FontStyle = Vortice.DirectWrite.FontStyle;
using Timer = System.Windows.Forms.Timer;

namespace Speaking_clock.Widgets;

public class RadioPlayerWidget : CompositionWidgetBase
{
    private readonly ContextMenuStrip _stationMenu;
    private readonly Timer _timer;
    private ID2D1SolidColorBrush _backgroundBrush;

    // State
    private string _currentStation = "Nincs állomás kiválasztva";
    internal bool _isPlaying;

    // Resources
    private ID2D1SolidColorBrush _textBrush;
    private float _volume = 0.05f; // Default volume: 5%
    internal string currentRadioName, currentRadioURL;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RadioPlayerWidget" /> class.
    /// </summary>
    /// <param name="startX">Initial X position</param>
    /// <param name="startY">Initial Y position</param>
    /// <param name="scale">Scaling factor</param>
    public RadioPlayerWidget(int startX, int startY, float scale = 1f)
        : base(startX, startY, 300, 100) // Pass dimensions to base
    {
        Text = "Radio Player";

        // Initialize station selection menu
        _stationMenu = new ContextMenuStrip();
        PopulateStationMenu();

        // Bind specific input events
        DoubleClick += RadioPlayer_DoubleClick;
        MouseWheel += RadioPlayer_MouseWheel;

        // Set up a timer for updating volume
        _timer = new Timer { Interval = 5000 };
        _timer.Tick += (s, e) =>
        {
            if (Beallitasok.RádióSection["Hangerő"].IntValue != (int)(_volume * 100))
            {
                Debug.WriteLine($"Hangerő frissítés({(int)(_volume * 100)})");
                Beallitasok.RádióSection["Hangerő"].IntValue = (int)(_volume * 100);
                Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
            }
        };
        _timer.Start();
    }

    // --- Base Class Overrides ---

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // Initialize volume from settings
        if (Beallitasok.RádióSection["Hangerő"].IntValue == -1)
        {
            Beallitasok.RádióSection["Hangerő"].IntValue = Beallitasok.BeszédSection["Hangerő"].IntValue;
            Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
            _volume = (float)(double)Beallitasok.RádióSection["Hangerő"].IntValue / 100;
        }
        else
        {
            _volume = (float)(double)Beallitasok.RádióSection["Hangerő"].IntValue / 100;
        }
    }

    protected override bool CanDrag()
    {
        // Logic from original MouseDown check
        return Beallitasok.RSS_Reader_Section["Húzás"].BoolValue;
    }

    protected override void SavePosition(int x, int y)
    {
        Beallitasok.WidgetSection["Rádió_X"].IntValue = x;
        Beallitasok.WidgetSection["Rádió_Y"].IntValue = y;
        Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
    }

    protected override void OnChildMouseDown(MouseEventArgs e)
    {
        // Base handles Left click dragging. We handle Right click menu.
        if (e.Button == MouseButtons.Right) _stationMenu.Show(this, e.Location);
    }

    protected override void DrawContent(ID2D1DeviceContext context)
    {
        // Create Resources if needed
        if (_textBrush == null || _textBrush.NativePointer == IntPtr.Zero)
            _textBrush = context.CreateSolidColorBrush(new Color4(1.0f, 1.0f, 1.0f));

        if (_backgroundBrush == null || _backgroundBrush.NativePointer == IntPtr.Zero)
            _backgroundBrush = context.CreateSolidColorBrush(new Color4(0f, 0f, 0f, 0.8f));

        // Draw Background (
        var rect = new RectangleF(0, 0, ClientSize.Width, ClientSize.Height);
        var roundedRect = new RoundedRectangle(rect, 20, 20);

        context.FillRoundedRectangle(roundedRect, _backgroundBrush);

        // Create Text Layout
        using var textFormat = _dwriteFactory.CreateTextFormat(
            "Arial",
            FontWeight.Normal,
            FontStyle.Normal,
            FontStretch.Normal,
            18
        );

        using var textLayout = _dwriteFactory.CreateTextLayout(
            $"Állomás: {_currentStation}\n" +
            $"Állapot: {(_isPlaying ? "Lejátszás" : "Szünet")}\n" +
            $"Hangerő: {Math.Round(_volume * 100)}%",
            textFormat,
            ClientSize.Width,
            ClientSize.Height
        );

        textLayout.TextAlignment = TextAlignment.Center;
        textLayout.ParagraphAlignment = ParagraphAlignment.Center;

        // Draw Text
        context.DrawTextLayout(new Vector2(0, 0), textLayout, _textBrush);
    }

    // --- Radio Logic ---

    public void SetScaleFactor(float value)
    {
        if (value > 0 && value != _scale)
        {
            _scale = value;
            Invalidate();
        }
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

    internal void ChangeVolume(float new_int)
    {
        _volume = Math.Clamp(new_int, 0.0f, 1.0f);
        Debug.WriteLine($"Volume adjusted to {_volume * 100}%");
        Invalidate();
        OnlineRadioPlayer.SetVolume(_volume);
    }

    internal async Task PlayRadio(string stationUrl, string stationName)
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

    internal void PauseRadio()
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _textBrush?.Dispose();
            _backgroundBrush?.Dispose();
            _stationMenu?.Dispose();
            _timer?.Dispose();
        }

        base.Dispose(disposing);
    }
}