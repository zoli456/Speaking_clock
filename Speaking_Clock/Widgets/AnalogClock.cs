using System.Numerics;
using Speaking_Clock;
using Vanara.PInvoke;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using FontStyle = Vortice.DirectWrite.FontStyle;
using Timer = System.Windows.Forms.Timer;

namespace Speaking_clock.Widgets;

public class AnalogClock : CompositionWidgetBase
{
    private readonly Timer _timer;
    private ID2D1SolidColorBrush _dotBrush;
    private ID2D1SolidColorBrush _handBrush;

    public AnalogClock(int startX, int startY, float scale = 1f)
        : base(startX, startY, 420, 420)
    {
        //_scale = scale;
        Scale(new SizeF(scale, scale));
        Text = "Analog Clock";

        // Opacity is handled by DirectComposition, keeping this at 1 ensures no Form-level alpha blend issues
        Opacity = 1f;

        _timer = new Timer { Interval = 1000 };
        _timer.Tick += (s, e) => Invalidate();
        _timer.Start();
    }

    // Configuration for Dragging
    protected override bool CanDrag()
    {
        // Access specific config section
        return Beallitasok.RSS_Reader_Section["Húzás"].BoolValue;
    }

    // Save Logic
    protected override void SavePosition(int x, int y)
    {
        Beallitasok.WidgetSection["Analóg_X"].IntValue = x;
        Beallitasok.WidgetSection["Analóg_Y"].IntValue = y;
        Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
    }

    // The Render Loop
    protected override void DrawContent(ID2D1DeviceContext context)
    {
        // Ensure brushes exist (re-create if context changed or first run)
        if (_handBrush == null || _handBrush.NativePointer == IntPtr.Zero)
            CreateBrushes(context);

        var time = DateTime.Now;
        var centerX = Width / 2f;
        var centerY = Height / 2f;
        var radius = Math.Min(Width, Height) / 2f - 20;

        // Draw clock face
        context.DrawEllipse(new Ellipse(new Vector2(centerX, centerY), radius, radius), _handBrush, 6);

        // Add hour markers and numbers
        for (var i = 0; i < 12; i++)
        {
            var isMajorHour = i % 3 == 0;
            DrawHourMarker(context, centerX, centerY, radius, i * 30, isMajorHour);

            if (isMajorHour)
            {
                var text = i == 0 ? "12" : i.ToString();
                DrawHourText(context, centerX, centerY, radius, i * 30, text);
            }
        }

        // Draw clock hands
        DrawHand(context, centerX, centerY, radius * 0.6f, time.Hour % 12 * 30 + time.Minute / 2.0f, 8);
        DrawHand(context, centerX, centerY, radius * 0.8f, time.Minute * 6, 5);
        DrawHand(context, centerX, centerY, radius * 0.9f, time.Second * 6);

        // Draw center dot
        context.FillEllipse(new Ellipse(new Vector2(centerX, centerY), 8, 8), _dotBrush);
    }

    private void CreateBrushes(ID2D1RenderTarget rt)
    {
        _handBrush?.Dispose();
        _dotBrush?.Dispose();
        _handBrush = rt.CreateSolidColorBrush(new Color4(1.0f, 1.0f, 1.0f));
        _dotBrush = rt.CreateSolidColorBrush(new Color4(1.0f, 0.0f, 0.0f));
    }

    // Helper methods updated to accept ID2D1RenderTarget/Context
    private void DrawHourMarker(ID2D1RenderTarget rt, float centerX, float centerY, float radius, float angle,
        bool isMajor)
    {
        var radians = (float)(Math.PI / 180.0 * (angle - 90));
        var outerRadius = radius;
        var innerRadius = radius - (isMajor ? radius * 0.1f : radius * 0.05f);

        var outerX = centerX + outerRadius * (float)Math.Cos(radians);
        var outerY = centerY + outerRadius * (float)Math.Sin(radians);
        var innerX = centerX + innerRadius * (float)Math.Cos(radians);
        var innerY = centerY + innerRadius * (float)Math.Sin(radians);

        rt.DrawLine(new Vector2(innerX, innerY), new Vector2(outerX, outerY), _handBrush, isMajor ? 8 : 4);
    }

    private void DrawHand(ID2D1RenderTarget rt, float x, float y, float length, float angle, float thickness = 3)
    {
        var radians = (float)(Math.PI / 180.0 * (angle - 90));
        var endX = x + length * (float)Math.Cos(radians);
        var endY = y + length * (float)Math.Sin(radians);

        rt.DrawLine(new Vector2(x, y), new Vector2(endX, endY), _handBrush, thickness * 1.5f);
    }

    private void DrawHourText(ID2D1RenderTarget rt, float centerX, float centerY, float radius, float angle,
        string text)
    {
        var radians = (float)(Math.PI / 180.0 * (angle - 90));
        var textRadius = radius - radius * 0.15f;

        var textX = centerX + textRadius * (float)Math.Cos(radians);
        var textY = centerY + textRadius * (float)Math.Sin(radians);

        // Using Base _dwriteFactory
        using var textFormat = _dwriteFactory.CreateTextFormat(
            "Arial", FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 30);

        using var layout = _dwriteFactory.CreateTextLayout(text, textFormat, 100, 50);
        var textWidth = layout.Metrics.Width;
        var textHeight = layout.Metrics.Height;

        rt.DrawText(text, textFormat,
            new Rect(textX - textWidth / 2, textY - textHeight / 2, textX + textWidth / 2, textY + textHeight / 2),
            _handBrush);
    }

    public void SetScaleFactor(float value)
    {
        if (value > 0 && value != _scale)
        {
            _scale = value;
            Invalidate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _handBrush?.Dispose();
            _dotBrush?.Dispose();
            _timer?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == (int)User32.WindowMessage.WM_DISPLAYCHANGE)
        {
            Left = Beallitasok.WidgetSection["Analóg_X"].IntValue;
            Top = Beallitasok.WidgetSection["Analóg_Y"].IntValue;
        }

        base.WndProc(ref m);
    }
}