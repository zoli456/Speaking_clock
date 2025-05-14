using System.Diagnostics;
using Telerik.WinControls.UI;
using Vanara.PInvoke;

namespace Speaking_Clock;

public partial class VirtualKeyboard : RadForm
{
    private bool _isDragging;

    public VirtualKeyboard()
    {
        InitializeComponent();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var pm = base.CreateParams;
            pm.ExStyle |=
                (int)(User32.WindowStylesEx.WS_EX_TOPMOST |
                      User32.WindowStylesEx.WS_EX_NOACTIVATE);
            return pm;
        }
    }


    protected override void WndProc(ref Message m)
    {
        const int waInactive = 0;
        if (m.Msg == (int)User32.WindowMessage.WM_MOUSEACTIVATE)
        {
            m.Result = (IntPtr)User32.WindowMessage.WM_NCACTIVATE;
            return;
        }

        if (m.Msg == (int)User32.WindowMessage.WM_ACTIVATE)
        {
            if (((int)m.WParam & 0xFFFF) != waInactive)
            {
                if (m.LParam != IntPtr.Zero)
                    User32.SetActiveWindow(m.LParam);
                else
                    User32.SetActiveWindow(IntPtr.Zero);
            }
        }
        else
        {
            // Check for the start of the drag (WM_MOVING)
            if (m.Msg == (int)User32.WindowMessage.WM_MOVING && !_isDragging)
            {
                _isDragging = true;
                Debug.WriteLine("Dragging started.");
            }
            // Check for the end of the drag (WM_EXITSIZEMOVE)
            else if (m.Msg == (int)User32.WindowMessage.WM_EXITSIZEMOVE && _isDragging)
            {
                _isDragging = false;
                Debug.WriteLine("Dragging stopped.");
                Beallitasok.GyorsmenüSection["Billentyűzet_PosX"].IntValue = Top;
                Beallitasok.GyorsmenüSection["Billentyűzet_PosY"].IntValue = Left;
                Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
            }
        }

        base.WndProc(ref m);
    }


    private void radCheckBox1_ToggleStateChanged(object sender, StateChangedEventArgs args)
    {
        if (radCheckBox1.Checked)
        {
            Beallitasok.Virtualkeyboard.ThemeName = "Fluent";
            Beallitasok.Virtualkeyboard.radVirtualKeyboard1.ThemeName = "Fluent";
        }
        else
        {
            Beallitasok.Virtualkeyboard.ThemeName = "Reset";
            Beallitasok.Virtualkeyboard.radVirtualKeyboard1.ThemeName = "Reset";
        }

        Beallitasok.GyorsmenüSection["Billentyűzet_Grey"].BoolValue = radCheckBox1.Checked;
        Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
    }
}