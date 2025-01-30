namespace Speaking_Clock;

public partial class SystemInformation : Form
{
    public SystemInformation()
    {
        InitializeComponent();
#if RELEASE
        TopMost = true;
#endif
    }
}