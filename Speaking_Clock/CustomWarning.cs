using Telerik.WinControls.UI;

namespace Speaking_Clock;

public partial class CustomWarningForm : Form
{
    private int _customHour;
    private int _customMinutes;

    private int _customWarningMinutes;

    public CustomWarningForm()
    {
        InitializeComponent();
    }


    private void CustomWarning_Cancel_Click(object sender, EventArgs e)
    {
        Dispose();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        if (CustomWarning_Amount.Text != "")
        {
            _customWarningMinutes = 0;
            if (!int.TryParse(CustomWarning_Amount.Text, out _customWarningMinutes)) return;
            Beallitasok.SetWarningTime(_customWarningMinutes);
            Beallitasok.ResetCustomButtons();
        }
        else
        {
            if (Custom_Hour.Text != "" && Custom_Minutes.Text != "")
            {
                _customMinutes = 0;
                _customHour = 0;
            }

            if (!int.TryParse(Custom_Hour.Text, out _customHour) ||
                !int.TryParse(Custom_Minutes.Text, out _customMinutes)) return;
            if (_customHour is > 23 or < 0 || _customMinutes is < 0 or > 59) return;
            if (Repeate_checkbox.Checked)
            {
                Beallitasok.FigyelmeztetésSection["Óra"].IntValue = _customHour;
                Beallitasok.FigyelmeztetésSection["Perc"].IntValue = _customMinutes;
                Beallitasok.NotificationRepeate = true;
                Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.BasePath}\\{Beallitasok.SetttingsFileName}");
            }
            else
            {
                var TempDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.Now.Day, _customHour, _customMinutes, 0);
                if (TempDateTime < DateTime.Now)
                {
                    TempDateTime = TempDateTime.AddDays(1);
                }

                Beallitasok.NextNotificationDate = TempDateTime;
            }
        }

        if (Beallitasok.NextNotificationDate != null)
        {
            Beallitasok.NotificationEnabled = true;
            Beallitasok.ResetCustomButtons();
            var lastItem = Beallitasok._warnings.DropDownItems[Beallitasok._warnings.DropDownItems.Count - 1];
            if (lastItem is ToolStripMenuItem menuItem)
            {
                menuItem.Checked = true;
                menuItem.Text = $"Figyelmeztetés {Beallitasok.NextNotificationDate.Hour}:{Beallitasok.NextNotificationDate.Minute} kor";
            }

            (Beallitasok.QuickMenu.radApplicationMenu2.Items[Beallitasok.QuickMenu.radApplicationMenu2.Items.Count - 1]
                as
                RadMenuItem).IsChecked = true;
            (Beallitasok.QuickMenu.radApplicationMenu2.Items[Beallitasok.QuickMenu.radApplicationMenu2.Items.Count - 1]
                    as
                    RadMenuItem).Text = $"Figyelmeztetés {_customHour}:{_customMinutes} kor";
            Dispose();
        }
    }

    private void CustomWarning_Amount_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
            e.KeyChar != '.')
            e.Handled = true;
    }

    private void CustomWarning_Amount_Enter(object sender, EventArgs e)
    {
        Custom_Hour.Text = string.Empty;
        Custom_Minutes.Text = string.Empty;
        Repeate_checkbox.Enabled = false;
        Repeate_checkbox.Checked = false;
    }

    private void Custom_Hour_Enter(object sender, EventArgs e)
    {
        CustomWarning_Amount.Text = string.Empty;
        Repeate_checkbox.Enabled = true;
    }

    private void Custom_Minutes_Enter(object sender, EventArgs e)
    {
        CustomWarning_Amount.Text = string.Empty;
        Repeate_checkbox.Enabled = true;
    }
}