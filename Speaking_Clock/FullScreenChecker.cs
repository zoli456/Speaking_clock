using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace Speaking_Clock;

internal static class FullScreenChecker
{
    internal static bool IsForegroundWindowFullScreenOrMaximized()
    {
        var hWnd = User32.GetForegroundWindow();
        if (hWnd == HWND.NULL) return false;

        var windowInfo = new User32.WINDOWINFO();
        windowInfo.cbSize = (uint)Marshal.SizeOf(windowInfo);
        if (!User32.GetWindowInfo(hWnd, ref windowInfo)) return false;

        // Basic visibility checks
        if (!windowInfo.dwStyle.HasFlag(User32.WindowStyles.WS_VISIBLE) ||
            windowInfo.dwStyle.HasFlag(User32.WindowStyles.WS_MINIMIZE))
            return false;

        // Check for maximized state first
        if (windowInfo.dwStyle.HasFlag(User32.WindowStyles.WS_MAXIMIZE)) // Or use User32.IsZoomed(hWnd)
            return true;

        // If not maximized, check for fullscreen state
        var hMonitor = User32.MonitorFromWindow(hWnd, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
        if (hMonitor == HMONITOR.NULL) return false;

        var monitorInfo = new User32.MONITORINFO();
        monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);
        if (!User32.GetMonitorInfo(hMonitor, ref monitorInfo)) return false;

        // Compare window bounds with monitor bounds for fullscreen
        return windowInfo.rcWindow == monitorInfo.rcMonitor;
    }

    internal static bool IsForegroundWindowFullScreen()
    {
        var hWnd = User32.GetForegroundWindow();
        if (hWnd == HWND.NULL) return false;

        var windowInfo = new User32.WINDOWINFO();
        windowInfo.cbSize = (uint)Marshal.SizeOf(windowInfo);
        if (!User32.GetWindowInfo(hWnd, ref windowInfo)) return false;

        if (!windowInfo.dwStyle.HasFlag(User32.WindowStyles.WS_VISIBLE) ||
            windowInfo.dwStyle.HasFlag(User32.WindowStyles.WS_MINIMIZE)) return false;

        var hMonitor = User32.MonitorFromWindow(hWnd, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
        if (hMonitor == HMONITOR.NULL) return false;

        var monitorInfo = new User32.MONITORINFO();
        monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);
        if (!User32.GetMonitorInfo(hMonitor, ref monitorInfo)) return false;

        return windowInfo.rcWindow == monitorInfo.rcMonitor;
    }

    /// <summary>
    ///     Checks if the foreground window is maximized (but not necessarily fullscreen).
    /// </summary>
    /// <returns>True if the foreground window is maximized, false otherwise.</returns>
    internal static bool IsForegroundWindowMaximized()
    {
        var hWnd = User32.GetForegroundWindow();
        if (hWnd == HWND.NULL) return false;
        return User32.IsZoomed(hWnd);
    }
}