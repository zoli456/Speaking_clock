using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Text.Json;
using Speaking_Clock;
using Vortice;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using FontStyle = Vortice.DirectWrite.FontStyle;
using Timer = System.Windows.Forms.Timer;

namespace Speaking_clock.Widgets;

public class NamedayWidget : CompositionWidgetBase
{
    private readonly Timer _timer;
    private ID2D1SolidColorBrush _handBrush;

    public NamedayWidget(int startX, int startY, float scale = 1f)
        : base(startX, startY, 225, 100)
    {
        Text = "Nameday";
        Opacity = 0.9f;
        _scale = scale;

        // Shape the window
        Region = CreateRoundedRectangleRegion(Size.Width, Size.Height, 20);

        // Set up a timer for updating clock
        _timer = new Timer { Interval = 60000 };
        _timer.Tick += (s, e) =>
        {
            if (Beallitasok.LastNamedayIndex != DateTime.Now.Day) UpdateNamedays();
        };
        _timer.Start();

        if (string.IsNullOrEmpty(Beallitasok.NameDays) || Beallitasok.LastNamedayIndex != DateTime.Now.Day)
            UpdateNamedays();
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
                Invalidate();
            }
        }
    }

    protected override void DrawContent(ID2D1DeviceContext context)
    {
        // --- Create resources ---
        if (_handBrush == null || _handBrush.Factory.NativePointer != context.Factory.NativePointer)
        {
            _handBrush?.Dispose();
            _handBrush = context.CreateSolidColorBrush(new Color4(1f, 1f, 1f)); // white text
        }

        // Black transparent background brush
        using var bgBrush = context.CreateSolidColorBrush(new Color4(0f, 0f, 0f, 0.8f));

        // --- Draw rounded rectangle background ---
        var rect = new RoundedRectangle
        {
            Rect = new RawRectF(0, 0, ClientSize.Width, ClientSize.Height),
            RadiusX = 20,
            RadiusY = 20
        };

        context.FillRoundedRectangle(rect, bgBrush);

        // --- Draw text ---
        using var textFormat = _dwriteFactory.CreateTextFormat(
            "Arial", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal,
            20 * _scale);

        using var textLayout = _dwriteFactory.CreateTextLayout(
            $"Mai névnap:\n{Beallitasok.NameDays}",
            textFormat,
            ClientSize.Width,
            ClientSize.Height);

        textLayout.TextAlignment = TextAlignment.Center;
        textLayout.ParagraphAlignment = ParagraphAlignment.Center;

        context.DrawTextLayout(new Vector2(0, 0), textLayout, _handBrush);
    }


    protected override void SavePosition(int x, int y)
    {
        Beallitasok.WidgetSection["Névnap_X"].IntValue = x;
        Beallitasok.WidgetSection["Névnap_Y"].IntValue = y;
        Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
    }

    protected override bool CanDrag()
    {
        return Beallitasok.RSS_Reader_Section["Húzás"].BoolValue;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        // Keep the rounded region updated on resize
        Region = CreateRoundedRectangleRegion(ClientSize.Width, ClientSize.Height, 20);
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
            Debug.WriteLine(e.Message);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _handBrush?.Dispose();
            _timer?.Dispose();
        }

        base.Dispose(disposing);
    }
}