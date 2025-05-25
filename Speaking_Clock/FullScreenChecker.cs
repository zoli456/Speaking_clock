using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace Speaking_Clock;

internal static class FullScreenChecker
{
    /// <summary>
    ///     Checks if the foreground window covers the entire monitor (true fullscreen).
    /// </summary>
    public static bool IsForegroundWindowFullscreen()
    {
        var hWnd = GetForegroundWindow();
        return hWnd != HWND.NULL && IsWindowFullscreen(hWnd);
    }

    /// <summary>
    ///     Checks if the foreground window is maximized or fullscreen.
    /// </summary>
    public static bool IsForegroundWindowMaximizedOrFullscreen()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == HWND.NULL || !IsWindowVisible(hWnd) || IsIconic(hWnd))
            return false;

        if (IsZoomed(hWnd)) // Maximized
            return true;

        return IsWindowFullscreen(hWnd);
    }

    /// <summary>
    ///     Determines whether the specified window occupies the full monitor area.
    /// </summary>
    public static bool IsWindowFullscreen(HWND hWnd)
    {
        if (!IsWindowVisible(hWnd) || IsIconic(hWnd))
            return false;

        // Filter out desktop and shell
        if (hWnd == GetDesktopWindow() || hWnd == GetShellWindow())
            return false;

        // Filter out owned windows (tooltips, dropdowns, etc.)
        if (GetWindow(hWnd, GetWindowCmd.GW_OWNER) != HWND.NULL)
            return false;

        // Get monitor info
        var mon = MonitorFromWindow(hWnd, MonitorFlags.MONITOR_DEFAULTTONEAREST);
        if (mon == HMONITOR.NULL)
            return false;

        var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(mon, ref mi))
            return false;
        var monRect = mi.rcMonitor;

        // Get window bounds via DWM or fallback
        RECT windowRect;
        var hr = DwmApi.DwmGetWindowAttribute(
            hWnd,
            DwmApi.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
            out windowRect);
        if (hr != 0)
            GetWindowRect(hWnd, out windowRect);

        // Compare
        return windowRect.Left <= monRect.Left
               && windowRect.Top <= monRect.Top
               && windowRect.Right >= monRect.Right
               && windowRect.Bottom >= monRect.Bottom;
    }
}