using System.Reflection;

namespace Speaking_Clock;

internal partial class Nevjegy : Form
{
    public Nevjegy()
    {
        InitializeComponent();
        Text = string.Format("Névjegy {0}", AssemblyTitle);
        labelProductName.Text = AssemblyProduct;
        labelVersion.Text = string.Format("Verzió {0} Release", AssemblyVersion);
#if DEBUG
        labelVersion.Text = string.Format("Verzió {0} Debug", AssemblyVersion);
#endif
        labelCopyright.Text = AssemblyCopyright;
        labelCompanyName.Text = AssemblyCompany;
        textBoxDescription.Text =
            $"Nem csak egy beszélő óra{Environment.NewLine}Weboldal: www.beszeloora.infy.uk"; //AssemblyDescription;
#if RELEASE
        //TopMost = true;
#endif
        Focus();
    }

    private void okButton_Click(object sender, EventArgs e)
    {
        Close();
    }

    #region Assembly Attribute Accessors

    public string AssemblyTitle
    {
        get
        {
            var attributes = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                var titleAttribute = (AssemblyTitleAttribute)attributes[0];
                if (titleAttribute.Title != "") return titleAttribute.Title;
            }

            return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
        }
    }

    public string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    public string AssemblyDescription
    {
        get
        {
            var attributes = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            if (attributes.Length == 0) return "";

            return ((AssemblyDescriptionAttribute)attributes[0]).Description;
        }
    }

    public string AssemblyProduct
    {
        get
        {
            var attributes = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            if (attributes.Length == 0) return "";

            return ((AssemblyProductAttribute)attributes[0]).Product;
        }
    }

    public string AssemblyCopyright
    {
        get
        {
            var attributes = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            if (attributes.Length == 0) return "";

            return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
    }

    public string AssemblyCompany
    {
        get
        {
            var attributes = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            if (attributes.Length == 0) return "";

            return ((AssemblyCompanyAttribute)attributes[0]).Company;
        }
    }

    #endregion
}