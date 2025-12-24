using System.Numerics;
using Speaking_Clock;
using Vortice.Direct2D1;
using Vortice.Mathematics;
using Timer = System.Windows.Forms.Timer;

namespace Speaking_clock.Widgets;

public class DotMatrixClock : CompositionWidgetBase
{
    private readonly float _digitSpacing = 80;
    private readonly bool _showSeconds;
    private readonly Timer _timer;
    private ID2D1SolidColorBrush _dotBrush;
    private float _dotSize = 10;
    private float _dotSpacing = 15;
    private bool _showColon = true;

    public DotMatrixClock(bool showSeconds, int startX, int startY, int DotSize = 10, int DotSpacing = 15,
        float DigitSpacing = 80)
        : base(startX, startY, showSeconds ? 720 : 420, 110)
    {
        Opacity = 0.9f;
        _dotSize = DotSize;
        _dotSpacing = DotSpacing;
        _digitSpacing = DigitSpacing;
        _showSeconds = showSeconds;

        Text = "Dot Matrix Clock";

        // Set up a timer for blinking
        _timer = new Timer { Interval = 1000 };
        _timer.Tick += (s, e) =>
        {
            _showColon = !_showColon;
            Invalidate();
        };
        _timer.Start();
    }

    public float GetDotSize()
    {
        return _dotSize;
    }

    public void SetDotSize(float value)
    {
        if (value > 0)
        {
            _dotSize = value;
            Invalidate();
        }
    }

    public float GetDotSpacing()
    {
        return _dotSpacing;
    }

    public void SetDotSpacing(float value)
    {
        if (value > 0)
        {
            _dotSpacing = value;
            Invalidate();
        }
    }

    protected override void DrawContent(ID2D1DeviceContext context)
    {
        if (_dotBrush == null || _dotBrush.Factory.NativePointer != context.Factory.NativePointer)
        {
            _dotBrush?.Dispose();
            _dotBrush = context.CreateSolidColorBrush(new Color4(1.0f, 1.0f, 1.0f));
        }

        var time = DateTime.Now;
        DrawDigit(context, time.Hour / 10, new Vector2(10, 10));
        DrawDigit(context, time.Hour % 10, new Vector2(10 + _digitSpacing, 10));

        if (_showColon) DrawColon(context, new Vector2(10 + _digitSpacing * 2, 10));

        DrawDigit(context, time.Minute / 10, new Vector2(10 + _digitSpacing * 2.5f, 10));
        DrawDigit(context, time.Minute % 10, new Vector2(10 + _digitSpacing * 3.5f, 10));

        if (_showSeconds)
        {
            if (_showColon) DrawColon(context, new Vector2(10 + _digitSpacing * 4.5f, 10));
            DrawDigit(context, time.Second / 10, new Vector2(10 + _digitSpacing * 5, 10));
            DrawDigit(context, time.Second % 10, new Vector2(10 + _digitSpacing * 6, 10));
        }
    }

    private void DrawDigit(ID2D1RenderTarget target, int digit, Vector2 position)
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
                target.FillEllipse(new Ellipse(dotPosition, _dotSize / 2, _dotSize / 2), _dotBrush);
            }
    }

    private void DrawColon(ID2D1RenderTarget target, Vector2 position)
    {
        var topDot = position + new Vector2(0, _dotSpacing);
        var bottomDot = position + new Vector2(0, _dotSpacing * 3);

        target.FillEllipse(new Ellipse(topDot, _dotSize / 2, _dotSize / 2), _dotBrush);
        target.FillEllipse(new Ellipse(bottomDot, _dotSize / 2, _dotSize / 2), _dotBrush);
    }

    protected override void SavePosition(int x, int y)
    {
        Beallitasok.WidgetSection["Pontozott_X"].IntValue = x;
        Beallitasok.WidgetSection["Pontozott_Y"].IntValue = y;
        Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
    }

    protected override bool CanDrag()
    {
        return Beallitasok.RSS_Reader_Section["Húzás"].BoolValue;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dotBrush?.Dispose();
            _timer?.Dispose();
        }

        base.Dispose(disposing);
    }
}