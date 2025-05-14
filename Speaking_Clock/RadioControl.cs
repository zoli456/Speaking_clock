using System.Diagnostics;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

namespace Speaking_Clock;

public partial class RadioControl : Form
{
    private readonly int Htcaption = 0x2;
    private bool _draggingEnabled;
    private ContextMenuStrip _stationMenu;
    private bool _trackbarMouseDown;
    private bool _trackbarScrolling;

    public RadioControl()
    {
        InitializeComponent();
    }

    private void SetFormAboveClock()
    {
        // Create an APPBARDATA structure to get taskbar info
        var appBarData = new APPBARDATA();
        appBarData.cbSize = (uint)Marshal.SizeOf(appBarData);

        // Get taskbar position using SHAppBarMessage with ABMsg.ABM_GETTASKBARPOS
        if (SHAppBarMessage(ABM.ABM_GETTASKBARPOS, ref appBarData) != IntPtr.Zero)
        {
            // SHAppBarMessage succeeded
            var taskbarRect = appBarData.rc;

            // Determine screen position of the clock (system tray)
            var screen = Screen.FromHandle(Handle); // Use current screen handle
            var taskbarPosition = GetTaskbarPosition(taskbarRect, screen);


            // Set form location depending on taskbar position
            switch (taskbarPosition)
            {
                case TaskbarPosition.Bottom:
                    Location = new Point(taskbarRect.right - Width - 50, taskbarRect.top - Height);
                    break;
                case TaskbarPosition.Top:
                    Location = new Point(taskbarRect.right - Width - 50, taskbarRect.bottom);
                    break;
                case TaskbarPosition.Left:
                    Location = new Point(taskbarRect.right, taskbarRect.bottom - Height - 50);
                    break;
                case TaskbarPosition.Right:
                    Location = new Point(taskbarRect.left - Width, taskbarRect.bottom - Height - 50);
                    break;
            }

            // Ensure form stays on top
            TopMost = true;
            _stationMenu = new ContextMenuStrip();
            PopulateStationMenu();
        }
        else
        {
            MessageBox.Show("Failed to get taskbar position.");
        }
    }

    private TaskbarPosition GetTaskbarPosition(RECT taskbarRect, Screen screen)
    {
        // Determine the width and height of the taskbar
        var taskbarWidth = taskbarRect.right - taskbarRect.left;
        var taskbarHeight = taskbarRect.bottom - taskbarRect.top;

        // Check if the taskbar is at the bottom or top (by comparing the height to the screen's height)
        if (taskbarHeight > taskbarWidth)
        {
            // The taskbar is on the left or right of the screen
            if (taskbarRect.left == screen.Bounds.Left)
                return TaskbarPosition.Left;
            if (taskbarRect.right == screen.Bounds.Right)
                return TaskbarPosition.Right;
        }
        else
        {
            // The taskbar is at the top or bottom of the screen
            if (taskbarRect.top == screen.Bounds.Top)
                return TaskbarPosition.Top;
            if (taskbarRect.bottom == screen.Bounds.Bottom)
                return TaskbarPosition.Bottom;
        }

        // Default case (should not occur in normal scenarios)
        return TaskbarPosition.Bottom;
    }

    private void RadioControl_Shown(object sender, EventArgs e)
    {
        SetFormAboveClock();
        Volumelabel.Text = $"{Beallitasok.RádióSection["Hangerő"].IntValue}%";
        OnlineRadioPlayer.SetVolume((float)((decimal)RadiotrackBar.Value / 100));
    }

    //OnlineRadioPlayer.waveOut.Volume = (float) ((decimal) RadioVolumetrackBar.Value / 100)
    private void button1_Click(object sender, EventArgs e)
    {
        Beallitasok.PlayingRadio = false;
        OnlineRadioPlayer.Stop();
        Beallitasok.SafeInvoke(Beallitasok.SayItNowbutton, () => { Beallitasok.SayItNowbutton.Enabled = true; });
        Dispose();
    }

    private void RadiotrackBar_Scroll(object sender, EventArgs e)
    {
        _trackbarScrolling = true;
        Volumelabel.Text = $"{RadiotrackBar.Value}%";
        OnlineRadioPlayer.SetVolume((float)((decimal)RadiotrackBar.Value / 100));
        // OnlineRadioPlayer.WaveOut.Volume = (float)((decimal)RadiotrackBar.Value / 100);
        Beallitasok.RadioVolume = (float)((decimal)RadiotrackBar.Value / 100);
        Beallitasok.RádióSection["Hangerő"].IntValue = RadiotrackBar.Value;
    }

    private void RadiotrackBar_MouseUp(object sender, MouseEventArgs e)
    {
        if (_trackbarMouseDown && _trackbarScrolling)
            Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");


        _trackbarMouseDown = false;
        _trackbarScrolling = false;
    }

    private void RadiotrackBar_MouseDown(object sender, MouseEventArgs e)
    {
        _trackbarMouseDown = true;
    }

    private void checkBox1_CheckedChanged(object sender, EventArgs e)
    {
        _draggingEnabled = checkBox1.Checked;
    }

    private void RadioControl_MouseDown(object sender, MouseEventArgs e)
    {
        if (_draggingEnabled && e.Button == MouseButtons.Left)
        {
            // Call ReleaseCapture to release the mouse capture
            User32.ReleaseCapture();

            // Send a message to the window to start dragging
            User32.SendMessage(Handle, (uint)User32.WindowMessage.WM_NCLBUTTONDOWN, (IntPtr)Htcaption, IntPtr.Zero);
        }

        if (e.Button == MouseButtons.Right) _stationMenu.Show(this, e.Location);
    }

    private void PopulateStationMenu()
    {
        _stationMenu.Items.Clear();

        if (Beallitasok.RadioNames.Count != Beallitasok.RandioUrLs.Count)
        {
            Debug.WriteLine("Error: Station names and URLs lists must have the same number of elements.");
            return;
        }

        for (var i = 0; i < Beallitasok.RadioNames.Count; i++)
        {
            var name = Beallitasok.RadioNames[i];
            var url = Beallitasok.RandioUrLs[i];

            var item = new ToolStripMenuItem(name)
            {
                Tag = url // Store the URL in the item's tag
            };

            item.Click += (s, e) => Beallitasok.StartRadio((string)((ToolStripMenuItem)s).Tag, name);
            _stationMenu.Items.Add(item);
        }
    }

    private enum TaskbarPosition
    {
        Top,
        Bottom,
        Left,
        Right
    }
}