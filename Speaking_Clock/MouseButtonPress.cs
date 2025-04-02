using System.ComponentModel;
using static Vanara.PInvoke.User32;

namespace Speaking_Clock;

internal static class MouseButtonPress
{
    private static SafeHHOOK _hookHandle;
    private static HookProc _mouseProc;

    /// <summary>
    ///     Activate the mouse hook.
    /// </summary>
    /// <exception cref="Win32Exception"></exception>
    internal static void ActivateMouseHook()
    {
        if (_hookHandle == null)
        {
            _mouseProc = HookCallback;
            // Set the low-level mouse hook
            _hookHandle = SetWindowsHookEx(HookType.WH_MOUSE_LL, _mouseProc, IntPtr.Zero);
            if (_hookHandle.IsInvalid) throw new Win32Exception("Failed to set mouse hook.");
        }
    }

    internal static void DeactivateMouseHook()
    {
        // Unhook the mouse hook if it's set
        if (_hookHandle != null && !_hookHandle.IsClosed)
        {
            _hookHandle.Close();
            _hookHandle = null;
        }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // Check if the callback should process this event
        if (nCode >= 0)
        {
            if (wParam == (IntPtr)WindowMessage.WM_MBUTTONDOWN)
                // Block the event by returning a non-zero value
                return 1;

            if (wParam == (IntPtr)WindowMessage.WM_MBUTTONUP)
            {
                if (Beallitasok.QuickMenu == null || Beallitasok.QuickMenu.IsDisposed)
                {
                    Beallitasok.QuickMenu = new QuickMenu();
                    Beallitasok.QuickMenu.StartPosition = FormStartPosition.Manual;
                    Beallitasok.QuickMenu.Location = new Point(Cursor.Position.X - Beallitasok.QuickMenu.Width / 2,
                        Cursor.Position.Y - Beallitasok.QuickMenu.Height / 2);
                    Beallitasok.QuickMenu.Show();
                }

                // Block the event by returning a non-zero value
                return 1;
            }
        }

        // Pass unhandled events to the next hook in the chain
        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }
}