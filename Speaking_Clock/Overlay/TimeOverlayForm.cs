using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using static Vanara.PInvoke.User32;

namespace Speaking_Clock;

public class TimeOverlayForm : NativeWindow
{
    private const int LWA_COLORKEY = 0x00000001;
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private ID2D1Factory _d2dFactory;
    private bool _isVisible = true;
    private ID2D1HwndRenderTarget _renderTarget;
    private Thread _renderThread;
    private bool _running;
    private ID2D1SolidColorBrush _textBrush;
    private IDWriteTextFormat _textFormat;
    private IDWriteFactory _writeFactory;

    public TimeOverlayForm()
    {
        CreateOverlayWindow();
        InitializeDirect2D();
        StartRenderLoop();
    }

    private void CreateOverlayWindow()
    {
        var safeHwnd = CreateWindowEx(
            WindowStylesEx.WS_EX_TOPMOST | WindowStylesEx.WS_EX_NOACTIVATE | WindowStylesEx.WS_EX_TRANSPARENT |
            WindowStylesEx.WS_EX_LAYERED | WindowStylesEx.WS_EX_TOOLWINDOW,
            "STATIC",
            null,
            WindowStyles.WS_POPUP,
            Screen.PrimaryScreen.WorkingArea.Width - 90,
            0,
            90,
            45,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        if (safeHwnd.IsInvalid)
            throw new InvalidOperationException($"Failed to create window. Error: {Marshal.GetLastWin32Error()}");

        var hwnd = safeHwnd.DangerousGetHandle();
        AssignHandle(hwnd);

        SetLayeredWindowAttributes(hwnd, 0, 255, (LayeredWindowAttributes)LWA_COLORKEY);
    }


    private void InitializeDirect2D()
    {
        _d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory>();
        _writeFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();

        // Create the render target and assign it to _renderTarget
        _renderTarget = _d2dFactory.CreateHwndRenderTarget(
            new RenderTargetProperties(),
            new HwndRenderTargetProperties
            {
                Hwnd = Handle,
                PixelSize = new SizeI(90, 45),
                PresentOptions = PresentOptions.None
            });
        // Create a solid color brush and text format using the render target and DirectWrite factory
        _textBrush = _renderTarget.CreateSolidColorBrush(new Color4(1, 1, 1)); // White color
        _textFormat = _writeFactory.CreateTextFormat("Segoe UI", 24.0f);
    }

    private void StartRenderLoop()
    {
        _running = true;
        _renderThread = new Thread(RenderLoop) { IsBackground = true };
        _renderThread.Start();
    }

    private void RenderLoop()
    {
        while (_running)
        {
            Thread.Sleep(1000);
            if (Beallitasok.FullScreenApplicationRunning && Beallitasok.GyorsmenüSection["Átfedés"].BoolValue)
            {
                ShowOverlay();
                SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
                    SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
                _renderTarget.BeginDraw();
                _renderTarget.Clear(new Color4(0, 0, 0, 0)); // Transparent background

                var timeText = DateTime.Now.ToString("HH:mm:ss");
                var layoutRect = new RectangleF(0, 0, 90, 45);

                _renderTarget.DrawText(timeText, _textFormat, (Rect)layoutRect, _textBrush);

                _renderTarget.EndDraw();
            }
            else
            {
                HideOverlay();
            }
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == (int)WindowMessage.WM_DESTROY)
        {
            _running = false;
            _renderThread?.Join();
            _textBrush.Dispose();
            _textFormat.Dispose();
            _renderTarget.Dispose();
            _writeFactory.Dispose();
            _d2dFactory.Dispose();
        }

        /*if (m.Msg == (int)WindowMessage.WM_WINDOWPOSCHANGED)
        {
            var pos = Marshal.PtrToStructure<WINDOWPOS>(m.LParam);
            if ((pos.flags & SetWindowPosFlags.SWP_NOZORDER) == 0)
                // Reapply topmost to maintain Z-order
                SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
                    SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE);
        }*/

        base.WndProc(ref m);
    }

    public void ShowOverlay()
    {
        if (!_isVisible)
        {
            ShowWindow(Handle, ShowWindowCommand.SW_SHOW);
            _isVisible = true;
        }
    }

    public void HideOverlay()
    {
        if (_isVisible)
        {
            ShowWindow(Handle, ShowWindowCommand.SW_HIDE);
            _isVisible = false;
        }
    }

    public void ToggleOverlay()
    {
        if (_isVisible)
            HideOverlay();
        else
            ShowOverlay();
    }
}