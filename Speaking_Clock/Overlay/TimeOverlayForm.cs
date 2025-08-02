using System.Runtime.InteropServices;
using Microsoft.Win32;
using Speaking_clock.Widgets;
using Vanara.PInvoke;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vanara.PInvoke.User32;
using AlphaMode = Vortice.DCommon.AlphaMode;

namespace Speaking_Clock;

public class TimeOverlayForm : NativeWindow, IDisposable
{
    private readonly int Height = 45, Width = 90;
    private bool _isVisible = true;
    private ID2D1HwndRenderTarget _renderTarget;
    private Thread _renderThread;
    private bool _running;
    private ID2D1SolidColorBrush _textBrush;
    private IDWriteTextFormat _textFormat;

    public TimeOverlayForm()
    {
        CreateOverlayWindow();
        InitializeDirect2D();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        StartRenderLoop();
    }

    public void Dispose()
    {
        _running = false;
        _renderThread?.Join();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _textBrush?.Dispose();
        _textFormat?.Dispose();
        _renderTarget?.Dispose();
        ReleaseHandle();
    }

    private void CreateOverlayWindow()
    {
        var safeHwnd = CreateWindowEx(
            WindowStylesEx.WS_EX_TOPMOST |
            WindowStylesEx.WS_EX_NOACTIVATE |
            WindowStylesEx.WS_EX_TRANSPARENT |
            WindowStylesEx.WS_EX_LAYERED |
            WindowStylesEx.WS_EX_TOOLWINDOW,
            "STATIC",
            null,
            WindowStyles.WS_POPUP,
            Screen.PrimaryScreen.WorkingArea.Width - Width,
            0,
            Width,
            Height,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        if (safeHwnd.IsInvalid)
            throw new InvalidOperationException($"Failed to create window. Error: {Marshal.GetLastWin32Error()}");

        var hwnd = safeHwnd.DangerousGetHandle();
        AssignHandle(hwnd);
        SetLayeredWindowAttributes(hwnd, 0, 255, LayeredWindowAttributes.LWA_COLORKEY);
    }

    private void InitializeDirect2D()
    {
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

        _textBrush = _renderTarget.CreateSolidColorBrush(new Color4(1, 1, 1));
        _textFormat = GraphicsFactories.DWriteFactory.CreateTextFormat("Segoe UI", 24.0f);
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
                RepositionOverlay();
                _renderTarget.BeginDraw();
                _renderTarget.Clear(new Color4(0, 0, 0, 0));

                var timeText = DateTime.Now.ToString("HH:mm:ss");
                var layoutRect = new RectangleF(0, 0, Width, Height);

                _renderTarget.DrawText(timeText, _textFormat, (Rect)layoutRect, _textBrush);
                _renderTarget.EndDraw();
            }
            else
            {
                HideOverlay();
            }
        }
    }

    private void OnDisplaySettingsChanged(object sender, EventArgs e)
    {
        RepositionOverlay();
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_DISPLAYCHANGE = 0x007E;
        const int WM_DESTROY = (int)WindowMessage.WM_DESTROY;

        if (m.Msg == WM_DISPLAYCHANGE)
            RepositionOverlay();
        else if (m.Msg == WM_DESTROY) Dispose();

        base.WndProc(ref m);
    }

    private void RepositionOverlay()
    {
        var wa = Screen.PrimaryScreen.WorkingArea;
        var x = wa.Right - Width;
        var y = wa.Top;
        SetWindowPos(Handle,
            HWND.HWND_TOPMOST,
            x, y,
            0, 0,
            SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE);
    }

    public void ShowOverlay()
    {
        if (!_isVisible)
        {
            RepositionOverlay();
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
        if (_isVisible) HideOverlay();
        else ShowOverlay();
    }
}