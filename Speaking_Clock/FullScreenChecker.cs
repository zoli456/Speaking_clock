using System.Diagnostics;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace Speaking_Clock;

internal static class FullScreenChecker
{
    private static readonly HashSet<string> IgnoredProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer",
        "searchui",
        "runtimebroker",
        "applicationframehost",
        "shellexperiencehost",
        "dwm",
        "idlewatch",
        "steamwebhelper",
        "league of legends",
        "valorant",
        "zoom",
        "ms-teams",
        "teams",
        "discord",
        "spotify",
        "devenv",
        "lockapp"
    };


    /// <summary>
    ///     Returns the foreground fullscreen window's process, or null.
    /// </summary>
    internal static Process? GetForegroundFullscreenProcess()
    {
        var hWnd = GetForegroundFullscreenWindow();
        if (hWnd == HWND.NULL)
            return null;

        return GetProcessFromHwnd(hWnd);
    }

    internal static HWND GetForegroundFullscreenWindow()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == HWND.NULL)
            return HWND.NULL;

        return IsWindowFullscreen(hWnd) ? hWnd : HWND.NULL;
    }

    internal static bool IsWindowFullscreen(HWND hWnd)
    {
        if (!IsWindowVisible(hWnd) || IsIconic(hWnd))
            return false;

        if (hWnd == GetDesktopWindow() || hWnd == GetShellWindow())
            return false;

        if (IsSystemWindow(hWnd))
            return false;

        var mon = MonitorFromWindow(hWnd, MonitorFlags.MONITOR_DEFAULTTONEAREST);
        if (mon == HMONITOR.NULL)
            return false;

        var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(mon, ref mi))
            return false;

        RECT windowRect;
        var hr = DwmApi.DwmGetWindowAttribute(
            hWnd,
            DwmApi.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
            out windowRect);

        if (hr != 0)
            GetWindowRect(hWnd, out windowRect);

        var monRect = mi.rcMonitor;

        return windowRect.Left <= monRect.Left &&
               windowRect.Top <= monRect.Top &&
               windowRect.Right >= monRect.Right &&
               windowRect.Bottom >= monRect.Bottom;
    }

    private static bool IsSystemWindow(HWND hWnd)
    {
        GetWindowThreadProcessId(hWnd, out var pid);
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            foreach (var ignored in IgnoredProcessNames)
                if (proc.ProcessName.StartsWith(ignored, StringComparison.OrdinalIgnoreCase))
                    return true;
        }
        catch
        {
        }

        return false;
    }

    private static Process? GetProcessFromHwnd(HWND hWnd)
    {
        GetWindowThreadProcessId(hWnd, out var pid);
        if (pid == 0)
            return null;

        try
        {
            return Process.GetProcessById((int)pid);
        }
        catch
        {
            return null;
        }
    }
}