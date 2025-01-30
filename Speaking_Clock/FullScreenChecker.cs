using Vanara.PInvoke;

namespace Speaking_Clock;

internal class FullScreenChecker
{/// <summary>
/// Check if the active window is in fullscreen mode.
/// </summary>
/// <returns></returns>
    internal static bool IsAppInFullScreen()
    {
        // Get the handle of the active window
        var hWnd = User32.GetForegroundWindow();
        if (hWnd == IntPtr.Zero || !User32.IsWindowVisible(hWnd)) return false;

        // Exclude windows without titles
        if (User32.GetWindowTextLength(hWnd) == 0) return false;

        // Check if the window is minimized
        if (User32.IsIconic(hWnd)) return false;

        // Get window style and extended style
        var style = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_STYLE);
        var exStyle = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);

        // Get window dimensions
        if (!User32.GetWindowRect(hWnd, out var windowRect)) return false;

        // Determine the monitor the window is on
        var screen = Screen.AllScreens.FirstOrDefault(s => s.Bounds.IntersectsWith(new Rectangle(
            windowRect.left, windowRect.top,
            windowRect.right - windowRect.left, windowRect.bottom - windowRect.top
        )));
        if (screen == null || (!screen.Primary && !screen.DeviceName.Contains("DISPLAY"))) return false;

        // Check if the window covers the entire monitor area
        var isFullScreen = windowRect.left <= screen.Bounds.Left && windowRect.top <= screen.Bounds.Top &&
                           windowRect.right >= screen.Bounds.Right && windowRect.bottom >= screen.Bounds.Bottom;

        // Check if the window is borderless or uses overlapping style
        var isBorderless =
            (style & unchecked((int)User32.WindowStyles.WS_POPUP)) == unchecked((int)User32.WindowStyles.WS_POPUP) ||
            (style & unchecked((int)User32.WindowStyles.WS_OVERLAPPEDWINDOW)) !=
            unchecked((int)User32.WindowStyles.WS_OVERLAPPEDWINDOW);
        var isTopmost = (exStyle & (uint)User32.WindowStylesEx.WS_EX_TOPMOST) != 0;

        // Consider the window fullscreen if it covers the entire screen, is borderless, and is possibly topmost
        return isFullScreen && (isBorderless || isTopmost);
    }

    /// <summary>
    /// Check if the active window is in fullscreen or maximized mode.
    /// </summary>
    /// <returns></returns>
    internal static bool IsAppInFullScreenOrMaximized()
    {
        // Get the handle of the active window
        var hWnd = User32.GetForegroundWindow();
        if (hWnd == IntPtr.Zero) return false;

        // Check if the window is minimized
        if (User32.IsIconic(hWnd))
            return false;

        // Get window style and extended style
        var style = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_STYLE);
        var exStyle = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);

        // Get window dimensions
        if (!User32.GetWindowRect(hWnd, out var windowRect))
            return false;

        // Determine the monitor the window is on
        var screen = Screen.AllScreens.FirstOrDefault(s => s.Bounds.IntersectsWith(new Rectangle(windowRect.left,
            windowRect.top, windowRect.right - windowRect.left, windowRect.bottom - windowRect.top)));
        if (screen == null) return false;

        // Check if the window covers the entire monitor area
        var isFullScreen = windowRect.left <= screen.Bounds.Left && windowRect.top <= screen.Bounds.Top &&
                           windowRect.right >= screen.Bounds.Right && windowRect.bottom >= screen.Bounds.Bottom;

        // Check if the window is borderless or uses overlapping style
        var isBorderless =
            (style & unchecked((int)User32.WindowStyles.WS_POPUP)) == unchecked((int)User32.WindowStyles.WS_POPUP) ||
            (style & unchecked((int)User32.WindowStyles.WS_OVERLAPPEDWINDOW)) !=
            unchecked((int)User32.WindowStyles.WS_OVERLAPPEDWINDOW);
        var isTopmost = (exStyle & (uint)User32.WindowStylesEx.WS_EX_TOPMOST) != 0;

        // Check if the window is maximized
        var isMaximized = (style & (int)User32.WindowStyles.WS_MAXIMIZE) != 0;

        // Return true if the window is either fullscreen or maximized
        return (isFullScreen && (isBorderless || isTopmost)) || isMaximized;
    }
}