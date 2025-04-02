using System.Diagnostics;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace Speaking_Clock;

internal class KeyboardFunction
{
    private static bool _fired;
    private static bool _fired2;

    // Declare the hook variable as SafeHHOOK
    private static User32.SafeHHOOK _hookId;

    // Use the HookProc delegate from Vanara.PInvoke
    private static readonly User32.HookProc Proc = HookCallback;

    // Variables to track the key combination
    private static bool _ctrlPressed;
    private static bool _shiftPressed;

    private static bool _rshiftPressed;

    // Set up the hook using Vanara.PInvoke
    private static User32.SafeHHOOK SetHook(User32.HookProc proc)
    {
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            return User32.SetWindowsHookEx(User32.HookType.WH_KEYBOARD_LL, proc,
                Kernel32.GetModuleHandle(curModule.ModuleName));
        }
    }

    // The hook callback function
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            if (wParam == (int)User32.WindowMessage.WM_KEYDOWN)
            {
                var vkCode = Marshal.ReadInt32(lParam);

                // Detect Ctrl + Shift + Q
                if ((User32.VK)vkCode == User32.VK.VK_LCONTROL)
                    _ctrlPressed = true;

                if ((User32.VK)vkCode == User32.VK.VK_LSHIFT)
                    _shiftPressed = true;


                if ((User32.VK)vkCode == User32.VK.VK_RSHIFT)
                    _rshiftPressed = true;

                if (_ctrlPressed && _shiftPressed && (User32.VK)vkCode == User32.VK.VK_Q)
                {
                    if (!_fired)
                        CloseTopMostWindow();
                    _fired = true;
                    Debug.WriteLine("Ctrl + Shift + Q detected!");
                }

                if ((User32.VK)vkCode == User32.VK.VK_F9 && _rshiftPressed &&
                    Beallitasok.ScreenCaptureSection["Bekapcsolva"].BoolValue)
                {
                    if (!_fired2)
                        Task.Run(() =>
                            {
                                ScreenCapture.CaptureScreen(true);
                                if (!Beallitasok.PlayingRadio)
                                    Beallitasok.PlaySound(Beallitasok.NotificationSound);
                            }
                        );
                    _fired2 = true;
                }

                if ((User32.VK)vkCode == User32.VK.VK_F9 && Beallitasok.ScreenCaptureSection["Bekapcsolva"].BoolValue)
                {
                    if (!_fired2)
                        Task.Run(() =>
                            {
                                ScreenCapture.CaptureScreen(false);
                                if (!Beallitasok.PlayingRadio)
                                    Beallitasok.PlaySound(Beallitasok.NotificationSound);
                            }
                        );
                    _fired2 = true;
                }
            }
            else if (wParam == (int)User32.WindowMessage.WM_KEYUP)
            {
                var vkCode = Marshal.ReadInt32(lParam);

                // Reset key states when released
                if ((User32.VK)vkCode == User32.VK.VK_LCONTROL)
                    _ctrlPressed = false;

                if ((User32.VK)vkCode == User32.VK.VK_LSHIFT)
                    _shiftPressed = false;

                if ((User32.VK)vkCode == User32.VK.VK_RSHIFT)
                    _rshiftPressed = false;

                if (!_ctrlPressed && !_shiftPressed)
                    _fired = false;

                if ((User32.VK)vkCode == User32.VK.VK_F9 && _fired2) _fired2 = false;
            }
        }

        return User32.CallNextHookEx(_hookId.DangerousGetHandle(), nCode, wParam, lParam);
    }

    internal static void ActivateKeyboardHook()
    {
        _hookId = SetHook(Proc);
    }

    public static void CloseTopMostWindow()
    {
        // Get the handle of the foreground (topmost) window
        var hWnd = User32.GetForegroundWindow();

        if (hWnd != IntPtr.Zero)
        {
            // Send the WM_CLOSE message to the window to close it
            User32.PostMessage(hWnd, (int)User32.WindowMessage.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            Debug.WriteLine("Topmost window closed.");
        }
        else
        {
            Debug.WriteLine("No topmost window found.");
        }
    }
}