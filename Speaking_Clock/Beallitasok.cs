using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using NAudio.Wave;
using SharpConfig;
using Speaking_clock;
using Speaking_clock.Widgets;
using Telerik.WinControls.UI;
using Vanara.PInvoke;
using MethodInvoker = System.Windows.Forms.MethodInvoker;
using Task = System.Threading.Tasks.Task;
using Timer = System.Windows.Forms.Timer;

namespace Speaking_Clock;

public partial class Beallitasok : Form
{
    // Constants for key event flags
    private const User32.KEYEVENTF KeyeventfKeydown = 0; // Simulate key down event

    internal static readonly string Path = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
    internal static string SetttingsFileName = "settings.ini";
    internal static DateTime KovetkezoFigyelmeztetes;
    internal static bool WarningEnabled;
    internal static Configuration ConfigParser = Configuration.LoadFromFile($"{Path}\\{SetttingsFileName}");
    internal static Section BeszédSection = ConfigParser["Beszéd"];
    internal static Section IndításSection = ConfigParser["Indítás"];
    internal static Section SzinkronizálásSection = ConfigParser["Szinkronizálás"];
    internal static Section FigyelmeztetésSection = ConfigParser["Figyelmeztetés"];
    internal static Section HangfelismerésSection = ConfigParser["Hangfelismerés"];
    internal static Section HáttérképSection = ConfigParser["Háttérkép"];
    internal static Section RádióSection = ConfigParser["Rádió"];

    internal static Section GyorsmenüSection = ConfigParser["Gyorsmenü"];

    //internal static Section GmailSection = ConfigParser["Gmail"];
    internal static Section ScreenCaptureSection = ConfigParser["Képlopás"];
    internal static Section RSS_Reader_Section = ConfigParser["RSS_Olvasó"];
    internal static Section WidgetSection = ConfigParser["Widgetek"];
    internal static int CustomWarningHour, CustomWarningMinute;
    internal static bool CustomWarningRepeate;
    internal static bool PlayingRadio;
    private static RadioControl _radioControl;
    internal static float RadioVolume;
    internal static VirtualKeyboard Virtualkeyboard;
    internal static CustomWarningForm CustomWarningForm;
    internal static string Cordinates = "";
    internal static string Location = "";
    internal static string NameDays = "";
    internal static int LastNamedayIndex = -1;
    private static DateTime _nextWeatherCheck;
    private static byte[] _weatherSound;
    private static byte[] _namedaySound;
    private static byte[] _forecastSound;
    internal static QuickMenu QuickMenu;
    internal static FullScreenMenu FullscreenMenu;
    internal static bool FullScreenmenuOpened;
    internal static OverlayForm Overlay;
    internal static TimeOverlayForm TimeOverlay;
    internal static MemoryStream AlarmSound;
    internal static MemoryStream NotificationSound;
    internal static bool Lejátszás;

    internal static MemoryStream TimefileStream;
    internal static Dictionary<string, MemoryStream> ZipContent;
    internal static float BaseDpi = Utils.GetSystemDpi();
    private static JsonDocument _weatherData;
    internal static DateTime KovetkezoBeszed;
    internal static string VoskModel = "vosk-model-small-en-us-0.15";
    internal static List<string> RadioNames = new();
    internal static List<string> RandioUrLs = new();
    internal static string ZipPassword = Secrets.ZipPassword;
    internal static string RadioDataKey = Secrets.RadioDataKey;

    internal static string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    internal static NotifyIcon TrayIcon = new();

    internal static bool isVisibleCore;

    internal static string DefaultBrowerPath;
    internal static DateTime JelenlegiIdo;
    internal static int[] warningTimes = { 5, 10, 15, 20, 30, 45, 60 };
    internal static RSSReader rss1, rss2, rss3, rss4;
    internal static DotMatrixClock dotMatrix;
    internal static AnalogClock analogClock;
    internal static CalendarWidget calendarWidget;
    internal static NamedayWidget NamedayWidgetWidget;
    internal static RadioPlayerWidget radioPlayerWidget;
    private Widget_Setup _widgetForm;
    internal bool DefaultBrowserPlayingAudio;
    internal Timer? DelayedLoadTimer;
    internal bool Kimondva;
    private Mutex mutex;
    internal Nevjegy? Nevjegy;
    internal SystemInformation? SystemInformation;

    public Beallitasok()
    {
        InitializeComponent();
        Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        // Define a unique name for the Mutex
        mutex = new Mutex(true, "2DE96183-A1E5-408C-BBCC-CF513F65AF0C", out var isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show("Ez az alkalmazás már fut.", "Hiba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close(); // Exit the application
        }

        if (!File.Exists(SetttingsFileName)) Close();

        TrayIcon.Icon = Icon;
        JelenlegiIdo = DateTime.Now;
/*#if RELEASE
        TopMost = true;
#endif*/
        AnnounceTimeCheckBox.Checked = BeszédSection["Bekapcsolva"].BoolValue;
        voiceRecognitionCheckbox.Checked = BeszédSection["Bekapcsolva"].BoolValue;
        startup_checkbox.Checked = IndításSection["Automatikus"].BoolValue;
        hanypercenkent.Text = BeszédSection["Gyakoriság"].IntValue.ToString();
        Volume_trackBar.Value = BeszédSection["Hangerő"].IntValue;
        label3.Text = $"{Volume_trackBar.Value}%";
        twentyfour_checkbox.Checked = BeszédSection["24óra"].BoolValue;
        Synctime_checkBox.Checked = SzinkronizálásSection["Automatikus"].BoolValue;
        synctime_timer.Enabled = SzinkronizálásSection["Automatikus"].BoolValue;
        timerszerver_textBox.Text = SzinkronizálásSection["Szerver"].StringValue;
        ArrangeTimer.Checked = BeszédSection["Igazítás"].BoolValue;
        CustomWarningHour = FigyelmeztetésSection["Óra"].IntValue;
        CustomWarningMinute = FigyelmeztetésSection["Perc"].IntValue;
        voiceRecognitionCheckbox.Checked = HangfelismerésSection["Bekapcsolva"].BoolValue;
        NoiseFilteringcheckBox.Checked = HangfelismerésSection["Zajszűrés"].BoolValue;
        DailyWallpaperBox.Checked = HáttérképSection["Bekapcsolva"].BoolValue;
        RadioVolume = (float)(double)RádióSection["Hangerő"].IntValue / 100;
        OverlaycheckBox.Checked = GyorsmenüSection["Átfedés"].BoolValue;
        ScreenCapturecheckBox.Checked = ScreenCaptureSection["Bekapcsolva"].BoolValue;

        try
        {
            DefaultBrowerPath = Utils.GetDefaultBrowser();
            DefaultBrowserPlayingAudio = false;
        }
        catch (Exception e)
        {
            DefaultBrowerPath = "";
        }

        if (HangfelismerésSection["Eredmény_billentyű"].StringValue != "" &&
            HangfelismerésSection["Kiváltó_Szó"].StringValue != "")
        {
            TriggerWordtextBox.Text = HangfelismerésSection["Kiváltó_Szó"].StringValue;
            TriggerActiontextBox.Text = HangfelismerésSection["Eredmény_billentyű"].StringValue;
        }

        if (Environment.GetCommandLineArgs().Length > 0)
            if (Environment.GetCommandLineArgs().Any(x => x == "auto"))
            {
                DelayedLoadTimer = new Timer();
                DelayedLoadTimer.Interval = 5000;
                DelayedLoadTimer.Tick += DelayedLoadTimer_Tick;
                DelayedLoadTimer.Start();
                return;
                // }
            }

        PostLauchSetup();
        isVisibleCore = true;
        Visible = true;
#if DEBUG
        Utils.EncryptFile($"{Path}\\Fájlok\\RadioStations.dat", $"{Path}\\Fájlok\\RadioStations_enc.dat",
            RadioDataKey);
#endif
        Activate();
        Focus();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        Most_mod();
    }

    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(isVisibleCore);
    }

    internal static async Task Most_mod()
    {
        while (Lejátszás) await Task.Delay(100);
        if (OnlineRadioPlayer._playbackState == OnlineRadioPlayer.PlaybackState.Playing) return;
        SafeInvoke(SayItNowbutton, () => { SayItNowbutton.Enabled = false; });
        SpeechRecognition.DisableVoiceRecognition();
        if (DateTime.Now.Minute == 0)
        {
            await PlaySound(string.Concat("teljes_óra/",
                twentyfour_checkbox.Checked ? JelenlegiIdo.Hour : tizekkettoorasra_alakitas(JelenlegiIdo.Hour),
                " óra van.mp3"));
        }
        else
        {
            SafeInvoke(SayItNowbutton, () => { SayItNowbutton.Enabled = false; });
            await PlaySound(string.Concat("óra/",
                twentyfour_checkbox.Checked ? JelenlegiIdo.Hour : tizekkettoorasra_alakitas(JelenlegiIdo.Hour),
                " óra.mp3"));
            await PlaySound($"perc/{JelenlegiIdo.Minute} perc van.mp3");
        }

        SpeechRecognition.EnableVoiceRecognition();
        SafeInvoke(SayItNowbutton, () => { SayItNowbutton.Enabled = true; });
    }

    public static bool SafeInvoke(Control control, MethodInvoker method)
    {
        if (control != null && !control.IsDisposed && control.IsHandleCreated && control.FindForm().IsHandleCreated)
        {
            if (control.InvokeRequired)
                control.Invoke(method);
            else
                method();

            return true;
        }

        return false;
    }

    private static int tizekkettoorasra_alakitas(int hour)
    {
        if (hour > 12) return hour - 12;

        return hour;
    }

    internal static async Task<bool> PlaySound(string file)
    {
        Lejátszás = true;
        // Avoid creating new MemoryStream unless necessary
        if (!file.Contains("/"))
        {
            switch (file)
            {
                case "alarm.mp3":
                    TimefileStream = AlarmSound;
                    break;
                case "notification.mp3":
                    TimefileStream = NotificationSound;
                    break;
                default:
                {
                    Lejátszás = false;
                    SpeechRecognition.EnableVoiceRecognition();
                    return false; // Exit early if file is not recognized
                }
            }
        }
        else
        {
            if (ZipContent.ContainsKey(file))
            {
                TimefileStream = ZipContent[file];
            }
            else
            {
                Lejátszás = false;
                SpeechRecognition.EnableVoiceRecognition();
                return false; // Exit if file is not in zipContent
            }
        }

        TimefileStream.Position = 0;
        // Play in background task
        using (var rdr = new Mp3FileReader(TimefileStream))
        using (var wavStream = WaveFormatConversionStream.CreatePcmStream(rdr))
        using (var baStream = new BlockAlignReductionStream(wavStream))
        using (var waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
        {
            waveOut.Init(baStream);
            if (!PlayingRadio)
                waveOut.Volume = (float)((decimal)BeszédSection["Hangerő"].IntValue / 100);
            waveOut.Play();
            // Wait for the playback to finish
            while (waveOut.PlaybackState ==
                   PlaybackState.Playing) await Task.Delay(100); // Reduce the polling frequency

            // Cleanup after playback
            waveOut.Stop();
            waveOut.Dispose(); // Dispose stream and resources
            Lejátszás = false;
        }

        return true;
    }


    private void button2_Click(object sender, EventArgs e)
    {
        try
        {
            if (Volume_trackBar.Value > 100 || Volume_trackBar.Value < 0) throw new Exception("Hibás adat!");

            if (int.Parse(hanypercenkent.Text.Trim()) == 0) throw new Exception("Hibás adat!");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Hibás adatokat adtál meg!", "Hiba", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }


        BeszédSection["Bekapcsolva"].BoolValue = AnnounceTimeCheckBox.Checked;
        IndításSection["Automatikus"].BoolValue = startup_checkbox.Checked;
        BeszédSection["Gyakoriság"].IntValue = int.Parse(hanypercenkent.Text.Trim());
        BeszédSection["Hangerő"].IntValue = Volume_trackBar.Value;
        BeszédSection["24óra"].BoolValue = twentyfour_checkbox.Checked;
        BeszédSection["Hang"].StringValue = hangok_comboBox.SelectedItem.ToString();
        BeszédSection["Igazítás"].BoolValue = ArrangeTimer.Checked;
        SzinkronizálásSection["Automatikus"].BoolValue = Synctime_checkBox.Checked;
        SzinkronizálásSection["Szerver"].StringValue = SzinkronizálásSection["Szerver"].StringValue.Trim();
        synctime_timer.Enabled = Synctime_checkBox.Checked;
        HangfelismerésSection["Bekapcsolva"].BoolValue = voiceRecognitionCheckbox.Checked;
        HangfelismerésSection["Zajszűrés"].BoolValue = NoiseFilteringcheckBox.Checked;
        HáttérképSection["Bekapcsolva"].BoolValue = DailyWallpaperBox.Checked;
        ScreenCaptureSection["Bekapcsolva"].BoolValue = ScreenCapturecheckBox.Checked;
        GyorsmenüSection["Bekapcsolva"].BoolValue = QuickMenucheckBox.Checked;
        GyorsmenüSection["Átfedés"].BoolValue = OverlaycheckBox.Checked;
        CreateTask();
        if (BeszédSection["Bekapcsolva"].BoolValue)
            KovetkezoBeszed = DateTime.Now.AddMinutes(BeszédSection["Gyakoriság"].IntValue);

        if (TriggerActiontextBox.Text != "" && TriggerWordtextBox.Text != "")
        {
            if (Utils.CharToHexKeyCode(TriggerActiontextBox.Text.Trim().ToLower()) == 0x00)
            {
                TriggerActiontextBox.Text = "";
                TriggerWordtextBox.Text = "";
                HangfelismerésSection["Eredmény_billentyű"].StringValue = "";
                HangfelismerésSection["Kiváltó_Szó"].StringValue = "";
                MessageBox.Show("Nem engedélyezett billentyűt írtál be.", "Hiba", MessageBoxButtons.OK,
                    MessageBoxIcon.Stop);
            }
            else
            {
                HangfelismerésSection["Eredmény_billentyű"].StringValue = TriggerActiontextBox.Text.Trim().ToLower();
                HangfelismerésSection["Kiváltó_Szó"].StringValue = TriggerWordtextBox.Text.Trim().ToLower();
            }
        }

        ConfigParser.SaveToFile($"{Path}\\{SetttingsFileName}");
        ZipContent =
            Utils.LoadPasswordProtectedZipIntoMemory($"{Path}\\Hangok\\{BeszédSection["Hang"].StringValue}",
                ZipPassword);

        if (QuickMenucheckBox.Checked)
            //BlockMiddleClick.ActivateHook();
            MouseButtonPress.ActivateMouseHook();
        else
            //BlockMiddleClick.DeactivateHook();
            MouseButtonPress.DeactivateMouseHook();

        if (GyorsmenüSection["Átfedés"].BoolValue)
        {
            if (Overlay == null || TimeOverlay == null || Overlay.IsDisposed)
            {
                Overlay = new OverlayForm();
                TimeOverlay = new TimeOverlayForm();
            }
        }
        else
        {
            Overlay?.Dispose();
            //TimeOverlay?.Dispose();
        }

        if (HangfelismerésSection["Bekapcsolva"].BoolValue)
            SpeechRecognition.ActivateRecognition(VoskModel);
        else
            SpeechRecognition.DeactivateRecognition();
    }
    /// <summary>
    /// Create or delete the task that starts the application on startup
    /// </summary>
    private void CreateTask()
    {
        if (startup_checkbox.Checked)
            using (var td = TaskService.Instance.NewTask())
            {
                td.RegistrationInfo.Description = "Elindítja a Beszélő órát.";
                var lt = new LogonTrigger();
                lt.Delay = TimeSpan.FromSeconds(10);
                td.Triggers.Add(lt);
                td.Principal.RunLevel = TaskRunLevel.Highest;
                td.Actions.Add(Application.ExecutablePath, "auto");
                TaskService.Instance.RootFolder.RegisterTaskDefinition(
                    Assembly.GetExecutingAssembly().GetName().Name, td);
                lt.Dispose();
            }
        else
            using (var ts = new TaskService())
            {
                if (ts.GetTask(Assembly.GetExecutingAssembly().GetName().Name) != null)
                    ts.RootFolder.DeleteTask(Assembly.GetExecutingAssembly().GetName().Name);
            }
    }
    /// <summary>
    /// Set the application to start on startup
    /// </summary>
    private void SetStartup()
    {
        var rk = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        IndításSection["Automatikus"].BoolValue = startup_checkbox.Checked;
        if (startup_checkbox.Checked)
            rk.SetValue(Assembly.GetExecutingAssembly().GetName().Name, Application.ExecutablePath);
        else
            rk.DeleteValue(Assembly.GetExecutingAssembly().GetName().Name, false);

        rk.Dispose();
    }

    private void ResetWarnings()
    {
        ResetCustomButtons();
        // Get the last item in the collection
        var lastItem = _warnings.DropDownItems[_warnings.DropDownItems.Count - 1];

        // Check if it is a ToolStripMenuItem
        if (lastItem is ToolStripMenuItem menuItem)
        {
            menuItem.Checked = false;
            menuItem.Text = "Egyedi...";
        }
    }

    private void szamlalo_Tick(object sender, EventArgs e)
    {
        JelenlegiIdo = DateTime.Now;
        if (HangfelismerésSection["Bekapcsolva"].BoolValue && DefaultBrowerPath != "")
            if (DefaultBrowserPlayingAudio !=
                Utils.IsProcessPlayingAudio(
                    System.IO.Path.GetFileNameWithoutExtension(DefaultBrowerPath)))
            {
                DefaultBrowserPlayingAudio = !DefaultBrowserPlayingAudio;

                if (DefaultBrowserPlayingAudio)
                    SpeechRecognition.DisableVoiceRecognition();
                else
                    SpeechRecognition.EnableVoiceRecognition();
            }

        // Helper to reset all warnings
        // Handle Custom Warning
        /*if (FullScreenChecker.IsAppInFullScreenOrMaximized())
        {
            Debug.WriteLine("Maximalizálva!");
        }*/

        if (CustomWarningHour != -1 && CustomWarningMinute != -1)
        {
            if (JelenlegiIdo.Hour == CustomWarningHour && JelenlegiIdo.Minute == CustomWarningMinute)
            {
                if (!CustomWarningRepeate)
                    ResetWarnings();

                if (WarningEnabled)
                {
                    WarningEnabled = false;
                    if (!PlayingRadio)
                        PlaySound("alarm.mp3");
                    if (!FullScreenChecker.IsAppInFullScreen())
                        Utils.ShowAlert("Figyelmeztetés!", "Egy előre beállított figyelmeztető lejárt.",
                            30);

                    return;
                }
            }
            else if (CustomWarningRepeate)
            {
                WarningEnabled = true;
            }
        }
        // Handle Scheduled Warning
        else if (WarningEnabled && JelenlegiIdo.Hour == KovetkezoFigyelmeztetes.Hour &&
                 JelenlegiIdo.Minute == KovetkezoFigyelmeztetes.Minute
                )
        {
            WarningEnabled = false;
            ResetWarnings();
            if (!PlayingRadio)
                PlaySound("alarm.mp3");
            if (!FullScreenChecker.IsAppInFullScreen())
                Utils.ShowAlert("Visszaszámlálás lejárt!", "Egy előre beállított figyelmeztető visszaszámlálás lejárt.",
                    30);

            return;
        }

        // Handle "stick to whole" logic
        var shouldAnnounce = BeszédSection["Igazítás"].BoolValue
            ? JelenlegiIdo.Minute % BeszédSection["Gyakoriság"].IntValue == 0
            : JelenlegiIdo.Hour == KovetkezoBeszed.Hour && JelenlegiIdo.Minute == KovetkezoBeszed.Minute;

        if (shouldAnnounce && BeszédSection["Bekapcsolva"].BoolValue)
        {
            if (!Kimondva)
            {
                KovetkezoBeszed = JelenlegiIdo.AddMinutes(BeszédSection["Gyakoriság"].IntValue);
                Kimondva = true;
                Most_mod();
            }
        }
        else
        {
            Kimondva = false;
        }

        if (HáttérképSection["Bekapcsolva"].BoolValue &&
            JelenlegiIdo.Day != HáttérképSection["Utolsó_frissítés"].DateTimeValue.Day /*&& JelenlegiIdo.Hour > 9*/)
            try
            {
                DataServices.SetDailyWallpaperAsync();
                HáttérképSection["Utolsó_frissítés"].DateTimeValue = JelenlegiIdo;
                ConfigParser.SaveToFile($"{Path}\\{SetttingsFileName}");
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                HáttérképSection["Bekapcsolva"].BoolValue = false;
            }
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            isVisibleCore = false;
            Hide();
            e.Cancel = true;
        }
    }

    private void button3_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    private static void hangok_kereses()
    {
        hangok_comboBox?.Items?.Clear();
        var hangokMappaLista = Directory.GetFiles($"{Path}\\Hangok");
        foreach (var s in hangokMappaLista)
            if (!s.Contains("mp3"))
                hangok_comboBox.Items.Add(System.IO.Path.GetFileName(s));
    }

    private void pictureBox1_Click(object sender, EventArgs e)
    {
        hangok_kereses();
    }
    /// <summary>
    /// Get the current time from an NTP server
    /// </summary>
    /// <param name="ntpServer">The address of the NTP server.</param>
    /// <returns></returns>
    public static DateTime GetNetworkTime(string ntpServer)
    {
        const int daysTo1900 = 1900 * 365 + 95; // 95 = offset for leap-years etc.
        const long ticksPerSecond = 10000000L;
        const long ticksPerDay = 24 * 60 * 60 * ticksPerSecond;
        const long ticksTo1900 = daysTo1900 * ticksPerDay;

        var ntpData = new byte[48];
        ntpData[0] = 0x1B; // LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

        var addresses = Dns.GetHostEntry(ntpServer).AddressList;
        var ipEndPoint = new IPEndPoint(addresses[0], 123);
        var pingDuration = Stopwatch.GetTimestamp(); // temp access (JIT-Compiler need some time at first call)
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Connect(ipEndPoint);
            socket.ReceiveTimeout = 5000;
            socket.Send(ntpData);
            pingDuration = Stopwatch.GetTimestamp(); // after Send-Method to reduce WinSocket API-Call time

            socket.Receive(ntpData);
            pingDuration = Stopwatch.GetTimestamp() - pingDuration;
        }

        var pingTicks = pingDuration * ticksPerSecond / Stopwatch.Frequency;

        // optional: display response-time
        // Debug.WriteLine("{0:N2} ms", new TimeSpan(pingTicks).TotalMilliseconds);

        var intPart = ((long)ntpData[40] << 24) | ((long)ntpData[41] << 16) | ((long)ntpData[42] << 8) | ntpData[43];
        var fractPart = ((long)ntpData[44] << 24) | ((long)ntpData[45] << 16) | ((long)ntpData[46] << 8) | ntpData[47];
        var netTicks = intPart * ticksPerSecond + ((fractPart * ticksPerSecond) >> 32);

        var networkDateTime = new DateTime(ticksTo1900 + netTicks + pingTicks / 2);

        return networkDateTime.ToLocalTime(); // without ToLocalTime() = faster
    }

    private void button4_Click(object sender, EventArgs e)
    {
        button4.Enabled = false;
        try
        {
            ChangeDateTime(GetNetworkTime(SzinkronizálásSection["Szerver"].StringValue));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            MessageBox.Show("Nem sikerült a szinkronizálás!", "Hiba", MessageBoxButtons.OK, MessageBoxIcon.Error);
            button4.Enabled = true;
            return;
        }

        MessageBox.Show("Sikerült az időszikronizálás!", "Siker", MessageBoxButtons.OK, MessageBoxIcon.Information);
        button4.Enabled = true;
    }


    private static void ChangeDateTime(DateTime newDate)
    {
        var dateTime = newDate.ToUniversalTime();

        // Create a SYSTEMTIME structure from Vanara.PInvoke
        var st = new SYSTEMTIME
        {
            wYear = (ushort)dateTime.Year,
            wMonth = (ushort)dateTime.Month,
            wDay = (ushort)dateTime.Day,
            wHour = (ushort)dateTime.Hour,
            wMinute = (ushort)dateTime.Minute,
            wSecond = (ushort)dateTime.Second
        };

        // Use SetSystemTime from Kernel32 to set the system time
        if (!Kernel32.SetSystemTime(st)) throw new Win32Exception();
    }


    private void synctime_timer_Tick(object sender, EventArgs e)
    {
        Debug.WriteLine("Automatikus időszinkronizálás!");
        try
        {
            button4.Enabled = false;
            ChangeDateTime(GetNetworkTime(SzinkronizálásSection["Szerver"].StringValue));
            button4.Enabled = true;
        }
        catch (Exception ex)
        {
            button4.Enabled = true;
        }
    }

    private void button5_Click(object sender, EventArgs e)
    {
        if (Nevjegy == null || Nevjegy.IsDisposed)
        {
            Nevjegy = new Nevjegy();
            Nevjegy.Show();
        }
        else
        {
            Nevjegy?.Focus();
        }
    }

    private void Volume_trackBar_Scroll(object sender, EventArgs e)
    {
        label3.Text = $"{Volume_trackBar.Value}%";
    }


    internal static void SetWarningTime(int perc)
    {
        KovetkezoFigyelmeztetes = DateTime.Now.AddMinutes(perc);
        WarningEnabled = true;
    }

    internal static void DisableCustomWarning()
    {
        var lastItem = _warnings.DropDownItems[_warnings.DropDownItems.Count - 1];
        if (lastItem is ToolStripMenuItem menuItem)
        {
            menuItem.Checked = false;
            menuItem.Text = "Egyedi...";
        }

        var lastItem2 =
            QuickMenu.radApplicationMenu2.Items[QuickMenu.radApplicationMenu2.Items.Count - 1] as RadMenuItem;
        lastItem2.Text = "Egyedi...";
        lastItem2.IsChecked = false;
        WarningEnabled = false;
        if (CustomWarningHour == -1 || CustomWarningMinute == -1) return;
        FigyelmeztetésSection["Óra"].IntValue = -1;
        FigyelmeztetésSection["Perc"].IntValue = -1;
        CustomWarningHour = -1;
        CustomWarningMinute = -1;
        CustomWarningRepeate = false;
        ConfigParser.SaveToFile($"{Path}\\{SetttingsFileName}");
    }


    private void ExitButton_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    // Function to simulate a real key press
    internal static void SimulateRealKeyPress(byte keyCode)
    {
        // Simulate key down event
        User32.keybd_event(keyCode, 0, KeyeventfKeydown,
            IntPtr.Zero); // Using IntPtr.Zero for the last argument

        // Wait for a small delay to simulate real key press duration
        Thread.Sleep(50); // Adjust this delay as needed

        // Simulate key up event
        User32.keybd_event(keyCode, 0, User32.KEYEVENTF.KEYEVENTF_KEYUP,
            IntPtr.Zero); // Using IntPtr.Zero for the last argument
    }

    internal static async Task AnnounceWeather()
    {
        if (OnlineRadioPlayer._playbackState == OnlineRadioPlayer.PlaybackState.Playing) return;

        if (string.IsNullOrEmpty(Location))
            try
            {
                var locationJson = await DataServices.GetLocationByIpAsync();
                using var locationData = JsonDocument.Parse(locationJson);
                var root = locationData.RootElement;

                Cordinates = root.GetProperty("loc").GetString();
                Location = root.GetProperty("city").GetString();

                if (string.IsNullOrEmpty(Location))
                    throw new Exception("Failed to retrieve location from IP.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving location by IP: {ex.Message}");
                return;
            }

        if (_weatherSound != null && _nextWeatherCheck >= DateTime.Now)
        {
            DataServices.PlayStream(_weatherSound, 1.3f);
            return;
        }

        if (_nextWeatherCheck < DateTime.Now)
            try
            {
                var weatherJson = await DataServices.GetWeatherdotComAsync(Cordinates,Secrets.WeatherdotcomApiKey);
                _weatherData = JsonDocument.Parse(weatherJson);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching weather data: {ex.Message}");
                return;
            }

        try
        {
            var current = _weatherData.RootElement.GetProperty("v3-wx-observations-current");
            var temperature = current.GetProperty("temperature").GetInt32();
            var condition = current.GetProperty("wxPhraseLong").GetString();

            var temperatureText = temperature >= 0
                ? $"Jelenleg {temperature} fok van és az időjárás {condition}."
                : $"Jelenleg mínusz {temperature} fok van és az időjárás {condition}.";

            _weatherSound = await DataServices.ConvertTextToSpeech(temperatureText);
            _nextWeatherCheck = DateTimeOffset.FromUnixTimeSeconds(current.GetProperty("expirationTimeUtc").GetInt64())
                .LocalDateTime;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error processing weather data: {ex.Message}");
            return;
        }

        DataServices.PlayStream(_weatherSound, 1.3f);
    }

    internal static async Task AnnounceForecast()
    {
        if (OnlineRadioPlayer._playbackState == OnlineRadioPlayer.PlaybackState.Playing) return;

        if (string.IsNullOrEmpty(Location))
            try
            {
                var locationJson = await DataServices.GetLocationByIpAsync();
                using var locationData = JsonDocument.Parse(locationJson);
                var root = locationData.RootElement;

                Cordinates = root.GetProperty("loc").GetString();
                Location = root.GetProperty("city").GetString();

                if (string.IsNullOrEmpty(Location))
                    throw new Exception("Failed to retrieve location from IP.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving location by IP: {ex.Message}");
                return;
            }

        if (_forecastSound != null && _nextWeatherCheck >= DateTime.Now)
        {
            DataServices.PlayStream(_forecastSound, 1.3f);
            return;
        }

        if (_nextWeatherCheck < DateTime.Now)
            try
            {
                var weatherJson = await DataServices.GetWeatherdotComAsync(Cordinates, Secrets.WeatherapiApiKey);
                _weatherData = JsonDocument.Parse(weatherJson);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching weather data: {ex.Message}");
                return;
            }

        try
        {
            var current = _weatherData.RootElement.GetProperty("v3-wx-observations-current");
            var forecast = _weatherData.RootElement.GetProperty("v3-wx-forecast-daily-3day").GetProperty("daypart")[0];

            var dataindex = 0;
            for (var i = 0; i < forecast.GetProperty("daypartName").GetArrayLength(); i++)
                if (forecast.GetProperty("daypartName")[i].GetString() == "Holnap")
                {
                    dataindex = i;
                    break;
                }

            var minTemp = forecast.GetProperty("temperature")[dataindex + 1].GetInt32() >= 0
                ? forecast.GetProperty("temperature")[dataindex + 1].GetInt32().ToString()
                : $"mínusz {forecast.GetProperty("temperature")[dataindex + 1].GetInt32()}";

            var maxTemp = forecast.GetProperty("temperature")[dataindex].GetInt32() >= 0
                ? forecast.GetProperty("temperature")[dataindex].GetInt32().ToString()
                : $"mínusz {forecast.GetProperty("temperature")[dataindex].GetInt32()}";

            var temperatureText =
                $"Holnap a hőmérséklet {minTemp} és {maxTemp} fok között lesz. Holnapi időjárás: {forecast.GetProperty("wxPhraseLong")[dataindex].GetString()}. Csapadék valószínűsége {forecast.GetProperty("precipChance")[dataindex].GetInt32()} százalék.";

            _forecastSound = await DataServices.ConvertTextToSpeech(temperatureText);
            _nextWeatherCheck = DateTimeOffset.FromUnixTimeSeconds(current.GetProperty("expirationTimeUtc").GetInt64())
                .LocalDateTime;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error processing weather data: {ex.Message}");
            return;
        }

        DataServices.PlayStream(_forecastSound, 1.3f);
    }


    internal static async Task AnnounceNameDay()
    {
        if (OnlineRadioPlayer._playbackState == OnlineRadioPlayer.PlaybackState.Playing) return;
        try
        {
            if (string.IsNullOrEmpty(NameDays) || _namedaySound == null || LastNamedayIndex != DateTime.Now.Day)
            {
                var root = JsonDocument.Parse(await DataServices.GetNamedaysAsync()).RootElement;

                if (!root.TryGetProperty("nev1", out var nev1))
                {
                    Debug.WriteLine("Error: 'nev1' key is missing in the response.");
                    return;
                }

                NameDays = string.Join(", ", nev1.EnumerateArray().Select(n => n.GetString()));
                LastNamedayIndex = DateTime.Now.Day;

                try
                {
                    _namedaySound =
                        await DataServices.ConvertTextToSpeech(
                            $"Mai napon a névnapját ünnepli: {NameDays.Replace(", ", " és ")}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during text-to-speech conversion: {ex.Message}");
                    return;
                }


                try
                {
                    DataServices.PlayStream(_namedaySound, 1.3f);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during audio playback: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("Old");


                try
                {
                    DataServices.PlayStream(_namedaySound, 1.3f);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during audio playback: {ex.Message}");
                }
            }
        }
        catch (HttpRequestException httpEx)
        {
            Debug.WriteLine($"Network error: {httpEx.Message}");
        }
        catch (JsonException jsonEx)
        {
            Debug.WriteLine($"JSON parsing error: {jsonEx.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }


    private void beállításokToolStripMenuItem_Click(object sender, EventArgs e)
    {
        isVisibleCore = true;
        Visible = !Visible;
    }

    private void holnapiIdőjárásToolStripMenuItem_Click(object sender, EventArgs e)
    {
        AnnounceForecast();
    }

    private void maiIdőjárásToolStripMenuItem_Click(object sender, EventArgs e)
    {
        AnnounceWeather();
    }

    private void maiNévnapToolStripMenuItem_Click(object sender, EventArgs e)
    {
        AnnounceNameDay();
    }


    internal static async Task StartRadio(string url, string randioName)
    {
        if (WidgetSection["Rádió_Bekapcsolva"].BoolValue && radioPlayerWidget._isPlaying) return;
        while (Lejátszás) await Task.Delay(100);
        PlayingRadio = true;
        SafeInvoke(SayItNowbutton, () => { SayItNowbutton.Enabled = false; });
        if (_radioControl == null || _radioControl.IsDisposed)
            _radioControl = new RadioControl();
        _radioControl.Radiolabel.Text = randioName;
        if (RádióSection["Hangerő"].IntValue == -1)
        {
            Debug.WriteLine("Új érték");
            RádióSection["Hangerő"].IntValue = BeszédSection["Hangerő"].IntValue;
            RadioVolume = (float)(double)RádióSection["Hangerő"].IntValue / 100;
            ConfigParser.SaveToFile($"{Path}\\{SetttingsFileName}");
        }

        RadioControl.RadiotrackBar.Value = RádióSection["Hangerő"].IntValue;
        OnlineRadioPlayer.PlayStreamAsync(url);
        _radioControl.Show();
    }

    private void DelayedLoadTimer_Tick(object sender, EventArgs e)
    {
        DelayedLoadTimer.Enabled = false;
        PostLauchSetup();
        DelayedLoadTimer.Dispose();
    }

    private async void button6_Click(object sender, EventArgs e)
    {
        /* if (File.Exists("UserData") && GmailSection["Bekapcsolva"].BoolValue)
         {
             File.Delete("UserData");
             MailTimer.Enabled = false;
             Gmailbutton.Text = "Gmail \u274c";
             GmailSection["Bekapcsolva"].BoolValue = false;
             GmailSection["Utolsó_ellenőrzés"].StringValue = "";
             ConfigParser.SaveToFile($"{Path}\\{SetttingsFileName}");
             GmailMailChecker.DisposeData();
             MessageBox.Show("Gmail értesítések sikeresen kikapcsolva!", "Információ", MessageBoxButtons.OK,
                 MessageBoxIcon.Information);
         }
         else
         {
             await GmailMailChecker.InitializeGmailServiceAsync();
             MailTimer.Enabled = true;
             Gmailbutton.Text = "Gmail \u2714\ufe0f";
             GmailSection["Bekapcsolva"].BoolValue = true;
             GmailSection["Utolsó_ellenőrzés"].DateTimeValue = DateTime.Now;
             ConfigParser.SaveToFile($"{Path}\\{SetttingsFileName}");
             MessageBox.Show("Sikerült bekapcsolni a Gmail értesítéseket!", "Információ", MessageBoxButtons.OK,
                 MessageBoxIcon.Information);
         }*/
    }

    private void MailTimer_Tick(object sender, EventArgs e)
    {
        /* if (!FullScreenChecker.IsAppInFullScreen())
             GmailMailChecker.CheckForUnreadEmailsAsync(GmailSection["Utolsó_ellenőrzés"].DateTimeValue);*/
    }

    internal static void ResetCustomButtons()
    {
        foreach (ToolStripItem item in _warnings.DropDownItems)
            if (item is ToolStripMenuItem menuItem)
                menuItem.Checked = false; // Uncheck the item

        foreach (RadMenuItem item in QuickMenu.radApplicationMenu2.Items)
            if (item is RadMenuItem menuItem)
                menuItem.IsChecked = false; // Uncheck the item
    }

    private static void PostLauchSetup()
    {
        Task.Run(async () =>
        {
            if (RádióSection["Utolsó_frissítés"].DateTimeValue < DateTime.Now)
            {
                Debug.WriteLine("Updating radio stations...");
                try
                {
                    await DataServices.DownloadFileAsync(
                        "https://raw.githubusercontent.com/zoli456/Speaking_clock/main/stations/RadioStations_enc.dat",
                        $"{Path}\\Fájlok\\RadioStations.dat");
                    RádióSection["Utolsó_frissítés"].DateTimeValue = DateTime.Now.AddDays(2);
                    ConfigParser.SaveToFile($"{Path}\\{SetttingsFileName}");
                    Debug.WriteLine("Updating radios was successful.");
                }
                catch (Exception ex)
                {
                }
            }


            if (File.Exists($"{Path}\\Fájlok\\RadioStations.dat"))
            {
                var file2 = Encoding.UTF8
                    .GetString(Utils.DecryptFileToMemory($"{Path}\\Fájlok\\RadioStations.dat", RadioDataKey))
                    .Split("\n");
                for (var i = 0; i < file2.Length; i++)
                {
                    var temp = file2[i].Split("\t");
                    RadioNames.Add(temp[0]);
                    RandioUrLs.Add(temp[1]);
                }

                for (var i = 0; i < RadioNames.Count; i++)
                {
                    var index = i;
                    var subMenuItem = new ToolStripMenuItem { Text = RadioNames[index] };
                    subMenuItem.Click += (sender, e) => { StartRadio(RandioUrLs[index], RadioNames[index]); };
                    rádióToolStripMenuItem.DropDownItems.Add(subMenuItem);
                }
            }
        });

        for (var i = 0; i < warningTimes.Length; i++)
        {
            var index = i;
            var subMenuItem = new ToolStripMenuItem { Text = $"{warningTimes[index]} perc múlva" };
            subMenuItem.Click += (sender, e) =>
            {
                var clickedItem = sender as ToolStripMenuItem;

                if (!subMenuItem.Checked)
                {
                    DisableCustomWarning();
                    SetWarningTime(warningTimes[index]);
                    Debug.WriteLine($"Set a notification in {warningTimes[index]} minutes.");
                    // Cast the sender to ToolStripMenuItem
                    if (clickedItem != null && clickedItem.Owner != null)
                        // Iterate through all the items in the same menu
                        foreach (ToolStripItem item in clickedItem.Owner.Items)
                            if (item is ToolStripMenuItem menuItem)
                                // Check all items except the clicked one
                                menuItem.Checked = menuItem == clickedItem;
                }
                else
                {
                    (_warnings.DropDownItems[index] as ToolStripMenuItem).Checked =
                        false;
                    DisableCustomWarning();
                }
            };
            _warnings.DropDownItems.Add(subMenuItem);
        }

        var CustomsubMenuItem = new ToolStripMenuItem { Text = "Egyedi..." };
        CustomsubMenuItem.Click += (sender, e) =>
        {
            if (CustomsubMenuItem.Checked)
            {
                DisableCustomWarning();
            }
            else
            {
                CustomWarningForm = new CustomWarningForm();
                CustomWarningForm.Show();
            }
        };
        _warnings.DropDownItems.Add(CustomsubMenuItem);
        if (CustomWarningMinute != -1 && CustomWarningHour != -1)
        {
            WarningEnabled = true;
            var lastItem = _warnings.DropDownItems[_warnings.DropDownItems.Count - 1];

            // Check if it is a ToolStripMenuItem
            if (lastItem is ToolStripMenuItem menuItem)
            {
                menuItem.Checked = true;
                menuItem.Text = $"Figyelmeztetés {CustomWarningHour}:{CustomWarningMinute} kor";
            }

            CustomWarningRepeate = true;
        }

        AlarmSound = new MemoryStream(File.ReadAllBytes($"{Path}\\Hangok\\alarm.mp3"));
        NotificationSound = new MemoryStream(File.ReadAllBytes($"{Path}\\Hangok\\notification.mp3"));
        hangok_kereses();
        hangok_comboBox.SelectedIndex = hangok_comboBox.FindStringExact(BeszédSection["Hang"].StringValue);
        ZipContent =
            Utils.LoadPasswordProtectedZipIntoMemory($"{Path}\\Hangok\\{BeszédSection["Hang"].StringValue}",
                ZipPassword);

        /* if (GmailSection["Bekapcsolva"].BoolValue)
         {
             GmailMailChecker.InitializeGmailServiceAsync();
             MailTimer.Enabled = true;
             Gmailbutton.Text = "Gmail \u2714\ufe0f";
         }
         else
         {
             Gmailbutton.Text = "Gmail \u274c";
         }*/

        if (HangfelismerésSection["Bekapcsolva"].BoolValue) SpeechRecognition.ActivateRecognition(VoskModel);
        Gmailbutton.Enabled = true;
        Applybutton.Enabled = true;


        QuickMenu = new QuickMenu();
        QuickMenu.Dispose();

        TrayIcon.Text = "Beszélő óra";
        TrayIcon.ContextMenuStrip = TimerMenu;


        if (SzinkronizálásSection["Automatikus"].BoolValue)
        {
            Debug.WriteLine("Automatikus időszinkronizálás!");
            try
            {
                button4.Enabled = false;
                ChangeDateTime(GetNetworkTime(SzinkronizálásSection["Szerver"].StringValue));
                button4.Enabled = true;
            }
            catch (Exception ex)
            {
                button4.Enabled = true;
            }
        }

        if (RSS_Reader_Section["Olvasó_1_Bekapcsolva"].BoolValue)
        {
            Debug.WriteLine("RSS olvasó 1 bekapcsolva!");
            rss1 = new RSSReader(1, RSS_Reader_Section["Olvasó_1_URL"].StringValue,
                RSS_Reader_Section["Olvasó_1_Név"].StringValue, "blue",
                RSS_Reader_Section["Olvasó_1_Pos_X"].IntValue,
                RSS_Reader_Section["Olvasó_1_Pos_Y"].IntValue);
        }

        if (RSS_Reader_Section["Olvasó_2_Bekapcsolva"].BoolValue)
        {
            Debug.WriteLine("RSS olvasó 2 bekapcsolva!");
            rss2 = new RSSReader(2, RSS_Reader_Section["Olvasó_2_URL"].StringValue,
                RSS_Reader_Section["Olvasó_2_Név"].StringValue, "red",
                RSS_Reader_Section["Olvasó_2_Pos_X"].IntValue,
                RSS_Reader_Section["Olvasó_2_Pos_Y"].IntValue, 601000);
        }

        if (RSS_Reader_Section["Olvasó_3_Bekapcsolva"].BoolValue)
        {
            Debug.WriteLine("RSS olvasó 3 bekapcsolva!");
            rss3 = new RSSReader(3, RSS_Reader_Section["Olvasó_3_URL"].StringValue,
                RSS_Reader_Section["Olvasó_3_Név"].StringValue, "orange",
                RSS_Reader_Section["Olvasó_3_Pos_X"].IntValue,
                RSS_Reader_Section["Olvasó_3_Pos_Y"].IntValue, 602000);
        }

        if (RSS_Reader_Section["Olvasó_4_Bekapcsolva"].BoolValue)
        {
            Debug.WriteLine("RSS olvasó 4 bekapcsolva!");
            rss4 = new RSSReader(4, RSS_Reader_Section["Olvasó_4_URL"].StringValue,
                RSS_Reader_Section["Olvasó_4_Név"].StringValue, "green",
                RSS_Reader_Section["Olvasó_4_Pos_X"].IntValue,
                RSS_Reader_Section["Olvasó_4_Pos_Y"].IntValue, 603000);
        }

        if (WidgetSection["Analóg_Bekapcsolva"].BoolValue)
        {
            Debug.WriteLine("Analóg óra bekapcsolva!");
            analogClock = new AnalogClock(WidgetSection["Analóg_X"].IntValue, WidgetSection["Analóg_Y"].IntValue,
                WidgetSection["Analóg_méret"].FloatValue);
        }

        if (WidgetSection["Pontozott_Bekapcsolva"].BoolValue)
        {
            Debug.WriteLine("Pontozott óra bekapcsolva!");
            dotMatrix = new DotMatrixClock(WidgetSection["Pontozott_másodperc"].BoolValue,
                WidgetSection["Pontozott_X"].IntValue,
                WidgetSection["Pontozott_Y"].IntValue, WidgetSection["Pontozott_pont_méret"].IntValue,
                WidgetSection["Pontozott_pont_távolság"].IntValue, WidgetSection["Pontozott_szám_távolság"].IntValue);
        }

        if (WidgetSection["Naptár_Bekapcsolva"].BoolValue)
        {
            Debug.WriteLine("Naptár bekapcsolva!");
            calendarWidget = new CalendarWidget(WidgetSection["Naptár_X"].IntValue, WidgetSection["Naptár_Y"].IntValue);
        }

        if (WidgetSection["Névnap_Bekapcsolva"].BoolValue)
        {
            Debug.WriteLine("Névnap bekapcsolva!");
            NamedayWidgetWidget =
                new NamedayWidget(WidgetSection["Névnap_X"].IntValue, WidgetSection["Névnap_Y"].IntValue);
        }

        if (WidgetSection["Rádió_Bekapcsolva"].BoolValue)
        {
            Debug.WriteLine("Rádió bekapcsolva!");
            radioPlayerWidget =
                new RadioPlayerWidget(WidgetSection["Rádió_X"].IntValue, WidgetSection["Rádió_Y"].IntValue);
        }


        if (BeszédSection["Bekapcsolva"].BoolValue)
            KovetkezoBeszed = DateTime.Now.AddMinutes(BeszédSection["Gyakoriság"].IntValue);
        TrayIcon.Visible = true;
        szamlalo.Enabled = true;

        if (GyorsmenüSection["Átfedés"].BoolValue)
        {
            Overlay = new OverlayForm();
            TimeOverlay = new TimeOverlayForm();
        }

        KeyboardFunction.ActivateKeyboardHook();
        if (GyorsmenüSection["Bekapcsolva"].BoolValue)
        {
            QuickMenucheckBox.Checked = true;
            MouseButtonPress.ActivateMouseHook();
        }
    }

    private void hardwareToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (SystemInformation == null || SystemInformation.IsDisposed)
        {
            SystemInformation = new SystemInformation();
        }
        else
        {
            SystemInformation.Dispose();
            SystemInformation = new SystemInformation();
        }

        SystemInformation.InfotextBox.Text += ComputerInfo.WindowsNevEsVerzio() + Environment.NewLine;
        SystemInformation.InfotextBox.Text +=
            $"Windows aktivációs állapot:  {ComputerInfo.WindowsAktivaciosAllapot()}" + Environment.NewLine;
        SystemInformation.InfotextBox.Text += $"TPM bekapcsolva: {ComputerInfo.IsTpmEnabled()}" + Environment.NewLine;
        SystemInformation.InfotextBox.Text +=
            $"Biztonságos rendszerindítás bekapcsolva: {ComputerInfo.IsSecureBootEnabled()}" + Environment.NewLine;
        SystemInformation.InfotextBox.Text += $"Víruskereső(k):  {string.Join(", ", ComputerInfo.VirusScannerNev())}" +
                                              Environment.NewLine;
        SystemInformation.InfotextBox.Text += $"Alaplap:  {ComputerInfo.AlaplapNev()}" + Environment.NewLine;
        SystemInformation.InfotextBox.Text += $"Processzor:  {ComputerInfo.ProcesszorNev()}" + Environment.NewLine;
        SystemInformation.InfotextBox.Text += $"RAM:  {ComputerInfo.RamMeret()}" + Environment.NewLine;
        SystemInformation.InfotextBox.Text += $"Videókártya:  {ComputerInfo.VgaNev()}" + Environment.NewLine;
        SystemInformation.InfotextBox.Text += $"Videókártya driver verzió:  {ComputerInfo.VgaDriverVerzio()}";
        SystemInformation.Show();
    }

    private void futóFolyamatokToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (SystemInformation == null || SystemInformation.IsDisposed)
        {
            SystemInformation = new SystemInformation();
        }
        else
        {
            SystemInformation.Dispose();
            SystemInformation = new SystemInformation();
        }

        var lista = Utils.RendezesElsoBetuAlapjan(ComputerInfo.FutoFolyamatokListazasa());
        foreach (var elem in lista) SystemInformation.InfotextBox.Text += elem + Environment.NewLine;
        SystemInformation.Show();
    }

    private void telepítettProgramokToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (SystemInformation == null || SystemInformation.IsDisposed)
        {
            SystemInformation = new SystemInformation();
        }
        else
        {
            SystemInformation.Dispose();
            SystemInformation = new SystemInformation();
        }

        var lista = Utils.RendezesElsoBetuAlapjan(ComputerInfo.TelepitettProgramokListazasa());
        foreach (var elem in lista) SystemInformation.InfotextBox.Text += elem + Environment.NewLine;
        SystemInformation.Show();
    }

    private void cKönytárakTelepítéseToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Utils.DownloadAndInstallLatestRelease("abbodi1406", "vcredist", 1, "/y");
    }

    private void directX9TelepítéseToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Utils.DownloadAndInstallDirectX9();
    }

    private void directPlayTelepítésToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Utils.EnableWindowsFeature("DirectPlay");
    }

    private void net35TelepítésToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Utils.EnableWindowsFeature("NetFx3");
    }

    private void hozzáadásADefenderKivételekhezToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Utils.AddFolderToDefenderExclusions();
    }

    private void játékJavítás1TelepítésToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Utils.ExtractPasswordProtectedZip($"{Path}\\Fájlok\\GameFix1", "Amjgw9LRXWsXyu5Sw7YE");
    }

    private void játékJavítás2TelepítésToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Utils.ExtractPasswordProtectedZip($"{Path}\\Fájlok\\GameFix2", "Amjgw9LRXWsXyu5Sw7YE");
    }

    private void winrarTelepítéseToolStripMenuItem_Click(object sender, EventArgs e)
    {
        DataServices.DownloadAndInstallWinrar();
    }

    private void zipTelepítéseToolStripMenuItem_Click(object sender, EventArgs e)
    {
        DataServices.DownloadAndInstall7Zip();
    }

    private async void nVIDIAVezérlőLetöltésToolStripMenuItem_Click(object sender, EventArgs e)
    {
        /*MessageBox.Show("Hamarosan megfog kezdődni a driver letöltése és utána el fog indulni.", "Információ",
            MessageBoxButtons.OK, MessageBoxIcon.Information);*/
        try
        {
            //DataServices.DownloadAndInstallNvidia();
            var driver = new Driver
            {
                Channel = DriverChannel.GameReady,
                Edition = DriverEdition.DCH
            };
            Process.Start(new ProcessStartInfo
            {
                FileName = await NvidiaDriverFinder.GetLatestNvidiaDriverForGpuAsync(
                    ComputerInfo.VgaNev().Replace("NVIDIA ", ""),
                    driver, ComputerInfo.WindowsNevEsVerzio().Contains("Windows 10")),
                UseShellExecute = true
            });
        }
        catch (Exception a)
        {
            Debug.WriteLine(a);
            MessageBox.Show("Nem sikerül letölteni a drivert.", "Hiba", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void button1_Click_1(object sender, EventArgs e)
    {
        if (_widgetForm == null || _widgetForm.IsDisposed)
        {
            _widgetForm = new Widget_Setup();
            _widgetForm.Show();
        }
        else
        {
            _widgetForm?.Focus();
        }
    }
}