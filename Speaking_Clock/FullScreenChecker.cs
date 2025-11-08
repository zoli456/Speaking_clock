using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace Speaking_Clock;

internal static class FullScreenChecker
{
    // Common system UI window classes
    private static readonly HashSet<string> IgnoredWindowClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman", // Desktop background
        "WorkerW", // Desktop icons
        "Shell_TrayWnd", // Taskbar
        "Shell_SecondaryTrayWnd", // Secondary taskbar
        "Button", // Generic buttons
        "Windows.UI.Core.CoreWindow" // UWP overlays
    };

    // Common system processes
    private static readonly HashSet<string> IgnoredProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer.exe", // Windows shell
        "SearchUI.exe", // Start menu search
        "RuntimeBroker.exe",
        "ApplicationFrameHost.exe", // UWP frame
        "ShellExperienceHost.exe", // Shell overlays
        "dwm.exe" // Desktop Window Manager
    };

    /// <summary>
    ///     Returns the HWND of the foreground window if it is fullscreen; otherwise HWND.NULL.
    /// </summary>
    internal static HWND GetForegroundFullscreenWindow()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == HWND.NULL)
            return HWND.NULL;

        return IsWindowFullscreen(hWnd) ? hWnd : HWND.NULL;
    }

    /// <summary>
    ///     Returns the HWND of the foreground window if it is maximized or fullscreen; otherwise HWND.NULL.
    /// </summary>
    internal static HWND GetForegroundMaximizedOrFullscreenWindow()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == HWND.NULL || !IsWindowVisible(hWnd) || IsIconic(hWnd))
            return HWND.NULL;

        if (IsZoomed(hWnd)) // Maximized
            return hWnd;

        return IsWindowFullscreen(hWnd) ? hWnd : HWND.NULL;
    }

    /// <summary>
    ///     Determines whether the specified window occupies the full monitor area.
    /// </summary>
    internal static bool IsWindowFullscreen(HWND hWnd)
    {
        if (!IsWindowVisible(hWnd) || IsIconic(hWnd))
            return false;

        // Filter out desktop, shell, owned windows, and system UI
        if (hWnd == GetDesktopWindow() || hWnd == GetShellWindow())
            return false;

        /*if (GetWindow(hWnd, GetWindowCmd.GW_OWNER) != HWND.NULL) //Doesnt work for some game
            return false;*/

        if (IsSystemWindow(hWnd))
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

    /// <summary>
    ///     Checks if a window belongs to a known system UI class or process.
    /// </summary>
    private static bool IsSystemWindow(HWND hWnd)
    {
        // Check class name
        var className = new StringBuilder(256);
        if (GetClassName(hWnd, className, className.Capacity) > 0 &&
            IgnoredWindowClasses.Contains(className.ToString()))
            return true;

        // Check process name
        GetWindowThreadProcessId(hWnd, out var pid);
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            if (IgnoredProcessNames.Contains(proc.ProcessName + ".exe"))
                return true;
        }
        catch
        {
        }

        return false;
    }

    internal static string? GetForegroundFullscreenProcessName()
    {
        var hWnd = GetForegroundFullscreenWindow();
        return GetProcessNameFromHwnd(hWnd);
    }

    internal static string? GetProcessNameFromHwnd(HWND hWnd)
    {
        if (hWnd == HWND.NULL)
            return null;

        GetWindowThreadProcessId(hWnd, out var pid);
        if (pid == 0)
            return null;

        try
        {
            using var proc = Process.GetProcessById((int)pid);
            return proc.ProcessName + ".exe";
        }
        catch
        {
            return null; // Process might have exited
        }
    }
}