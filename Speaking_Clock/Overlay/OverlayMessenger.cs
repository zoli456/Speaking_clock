using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Speaking_Clock;

namespace Speaking_clock.Overlay;

internal class OverlayMessenger
{
    private const string PipeName = "ClockOverlayPipe";
    private const char LINE_SEPARATOR = ';';
    private const char PART_SEPARATOR = '|';
    internal static string[] lastOptions = { "Első opció", "Második opció", "Harmadik opció" };
    private static string lastButtonText;

    private static NamedPipeServerStream pipe;
    private static StreamReader reader;
    private static StreamWriter writer;
    private static CancellationTokenSource readLoopCts;
    private static string ButtonTexts;

    internal static async Task RunServerLoop(CancellationToken token = default)
    {
        var warningText = new string[Beallitasok.warningTimes.Length + 1];
        for (var i = 0; i < Beallitasok.warningTimes.Length; i++)
            warningText[i] = $"{Beallitasok.warningTimes[i]} perc múlva";
        warningText[Beallitasok.warningTimes.Length] = "Kikapcsolás";
        lastOptions = warningText;
        while (!token.IsCancellationRequested)
        {
            try
            {
                Debug.WriteLine("Waiting for DLL to connect...");
                pipe = CreateNamedPipeWithSecurity(PipeName);
                await pipe.WaitForConnectionAsync(token);
                Debug.WriteLine("DLL connected.");
                reader = new StreamReader(pipe, Encoding.UTF8);
                writer = new StreamWriter(pipe, Encoding.UTF8)
                {
                    AutoFlush = true
                };
                while (DllInjector.CurrentProcess == null) await Task.Delay(10);
                await SendOptionsAsync(lastOptions);
                await SetButtonTextAsync("X");
                await SendOptionsAsync(lastOptions);
                await SendRadioStationsAsync(Beallitasok.RadioNames.ToArray());
                await SendRadioVolumeAsync(Beallitasok.RádióSection["Hangerő"].IntValue.ToString());
                ButtonTexts =
                    GetConfigForExecutable(Beallitasok.ÁtfedésSection["Gombok"].StringValue,
                        $"{DllInjector.CurrentProcess.ProcessName}.exe");
                if (ButtonTexts != "") await SendScreenButtonAsync(ButtonTexts);
                if (DllInjector.IsForcedExternal($"{DllInjector.CurrentProcess.ProcessName}.exe"))
                    await SetForcedExternal(true);
                else await SetForcedExternal(CheckForProblematicCases(DllInjector.CurrentProcess));

                if (Beallitasok.PlayingRadio)
                    if (Beallitasok.radioPlayerWidget != null && !Beallitasok.radioPlayerWidget.IsDisposed)
                        SendSetStationAsync(
                            Beallitasok.RadioNames.IndexOf(Beallitasok.radioPlayerWidget.currentRadioName));

                readLoopCts = new CancellationTokenSource();
                await ReadMessagesLoop(readLoopCts.Token);
            }
            catch (IOException)
            {
                Debug.WriteLine("Connection lost.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            finally
            {
                readLoopCts?.Cancel();
                pipe?.Dispose();
            }

            await Task.Delay(500, token);
        }
    }

    private static NamedPipeServerStream CreateNamedPipeWithSecurity(string name)
    {
        // Create security allowing Everyone and Authenticated Users
        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.WorldSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));
        ps.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        var psBytes = ps.GetSecurityDescriptorBinaryForm();
        var sa = new SECURITY_ATTRIBUTES();
        sa.nLength = Marshal.SizeOf(sa);
        sa.bInheritHandle = false;
        sa.lpSecurityDescriptor = Marshal.AllocHGlobal(psBytes.Length);
        Marshal.Copy(psBytes, 0, sa.lpSecurityDescriptor, psBytes.Length);

        // Call WinAPI CreateNamedPipe
        var fullPipeName = @"\\.\pipe\" + name;
        var handle = CreateNamedPipe(fullPipeName,
            PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED,
            PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT,
            1, 4096, 4096, 0, ref sa);

        if (handle.IsInvalid)
            throw new IOException("Unable to create named pipe, error: " + Marshal.GetLastWin32Error());

        return new NamedPipeServerStream(PipeDirection.InOut, true, false, handle);
    }

    private static async Task SendOptionsAsync(string[] options)
    {
        lastOptions = options;
        if (pipe?.IsConnected == true)
        {
            var msg = "OPTIONS:" + string.Join(",", options);
            await writer.WriteLineAsync(msg);
            Debug.WriteLine("Sent: " + msg.Trim());
        }
    }

    private static async Task SendSetStationAsync(int station)
    {
        if (pipe?.IsConnected == true)
        {
            var msg = "RADIO_CURRENT:" + station;
            await writer.WriteLineAsync(msg);
            Debug.WriteLine("Sent: " + msg.Trim());
        }
    }

    private static async Task SendRadioStationsAsync(string[] options)
    {
        lastOptions = options;
        if (pipe?.IsConnected == true)
        {
            var msg = "RADIO_LIST:" + string.Join(",", options);
            await writer.WriteLineAsync(msg);
            Debug.WriteLine("Sent: " + msg.Trim());
        }
    }

    private static async Task SendRadioVolumeAsync(string radio_volume)
    {
        if (pipe?.IsConnected == true)
        {
            var msg = "RADIO_VOLUME:" + radio_volume;
            await writer.WriteLineAsync(msg);
            Debug.WriteLine("Sent: " + msg.Trim());
        }
    }

    public static async Task SendHeadlineAsync(string headline, string link)
    {
        if (pipe?.IsConnected == true)
        {
            var msg = "HEADLINE:" + headline + "|" + link + "\n";
            await writer.WriteLineAsync(msg);
            Debug.WriteLine("Sent: " + msg.Trim());
        }
    }

    public static async Task SendScreenButtonAsync(string buttons)
    {
        if (pipe?.IsConnected == true)
        {
            var msg = "BUTTONS:" + buttons + "\n";
            await writer.WriteLineAsync(msg);
            Debug.WriteLine("Sent: " + msg.Trim());
        }
    }

    public static async Task SendWeatherAsync(string weather)
    {
        if (pipe?.IsConnected == true)
        {
            var msg = "WEATHER:" + weather + "\n";
            await writer.WriteLineAsync(msg);
            Debug.WriteLine("Sent: " + msg.Trim());
        }
    }

    public static async Task SetButtonTextAsync(string text)
    {
        if (text == lastButtonText) return;
        lastButtonText = text;
        if (pipe?.IsConnected == true)
        {
            var msg = $"SET_BUTTON_TEXT:{text}";
            await writer.WriteLineAsync(msg);
            Debug.WriteLine("Sent: " + msg.Trim());
        }
    }

    public static async Task SetForcedExternal(bool status)
    {
        if (pipe?.IsConnected == true)
        {
            var msg = $"FORCE_EXTERNAL_OVERLAY:{status.ToString().ToLower()}";
            await writer.WriteLineAsync(msg);
            Debug.WriteLine("Sent: " + msg.Trim());
        }
    }

    private static async Task ReadMessagesLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && pipe?.IsConnected == true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                    break;

                Debug.WriteLine("Received: " + line);

                if (line.StartsWith("SELECTED_OPTION:", StringComparison.OrdinalIgnoreCase))
                    if (int.TryParse(line.Substring("SELECTED_OPTION:".Length), out var index))
                    {
                        Debug.WriteLine($"User selected option index: {index}");
                        if (Beallitasok.warningTimes.Length == index)
                        {
                            Beallitasok.DisableCustomNotification();
                            for (var i = 0; i < Beallitasok.warningTimes.Length; i++)
                                (Beallitasok._warnings.DropDownItems[i] as ToolStripMenuItem).Checked = false;
                        }
                        else
                        {
                            Beallitasok.DisableCustomNotification();
                            Beallitasok.SetWarningTime(Beallitasok.warningTimes[index]);
                            (Beallitasok._warnings.DropDownItems[index] as ToolStripMenuItem).Checked = true;
                            Debug.WriteLine($"Set a notification in {Beallitasok.warningTimes[index]} minutes.");
                        }
                    }

                if (line.StartsWith("Activate_legacy", StringComparison.OrdinalIgnoreCase))
                    Beallitasok.ActivateLegacyOverlay();
                if (line.StartsWith("Deactivate_legacy", StringComparison.OrdinalIgnoreCase))
                    Beallitasok.DeactivateLegacyOverlay();
                if (line.StartsWith("BUTTON_CLICKED:", StringComparison.OrdinalIgnoreCase))
                    Beallitasok.SimulateRealKeyPress(
                        Utils.CharToHexKeyCode(line.Split(":")[1]));
                if (line.StartsWith("HEADLINE_CLICKED:", StringComparison.OrdinalIgnoreCase))
                {
                    var temp = line.IndexOf(':');
                    Utils.OpenWebpageAndReturn(line.Substring(temp + 1), DllInjector.CurrentProcess);
                }

                if (line.StartsWith("Radio_selected:", StringComparison.OrdinalIgnoreCase))
                    ChangeRadioStation(int.Parse(line.Split(":")[1]));
                if (line.StartsWith("Radio_volume_changed:", StringComparison.OrdinalIgnoreCase))
                    ChangeRadioVolume(int.Parse(line.Split(":")[1]));
                if (line.StartsWith("Radio_stop:", StringComparison.OrdinalIgnoreCase)) StopRadio();
            }
        }
        catch (IOException)
        {
            Debug.WriteLine("Read loop: connection lost.");
        }
    }

    private static void ChangeRadioStation(int id)
    {
        if (Beallitasok.radioPlayerWidget != null && !Beallitasok.radioPlayerWidget.IsDisposed)
        {
            Beallitasok.radioPlayerWidget.PlayRadio(Beallitasok.RandioUrLs[id], Beallitasok.RadioNames[id]);
        }
        else
        {
            if (Beallitasok.PlayingRadio) OnlineRadioPlayer.Stop();
            OnlineRadioPlayer.PlayStreamAsync(Beallitasok.RandioUrLs[id]);
            Beallitasok.PlayingRadio = true;
        }
    }

    private static void ChangeRadioVolume(int number)
    {
        if (Beallitasok.radioPlayerWidget != null && !Beallitasok.radioPlayerWidget.IsDisposed)
            Beallitasok.radioPlayerWidget.ChangeVolume((float)number / 100);
        if (Beallitasok.PlayingRadio)
            OnlineRadioPlayer.SetVolume((float)number / 100);
        else
            Beallitasok.RádióSection["Hangerő"].IntValue = number;
    }

    private static void StopRadio()
    {
        if (Beallitasok.PlayingRadio)
        {
            if (Beallitasok.radioPlayerWidget != null && !Beallitasok.radioPlayerWidget.IsDisposed)
                Beallitasok.radioPlayerWidget.PauseRadio();
            else
                OnlineRadioPlayer.Stop();
        }

        Beallitasok.PlayingRadio = false;
    }


    /// <summary>
    ///     Converts multi-line button configuration text from a TextBox into a single line for storage.
    ///     Each line is separated by a semicolon.
    /// </summary>
    /// <param name="multiLineConfig">The string content from the TextBox (e.g., "app.exe|Button1|ID1\r\napp.exe|Button2|ID2").</param>
    /// <returns>A single-line string representation (e.g., "app.exe|Button1|ID1;app.exe|Button2|ID2").</returns>
    public static string ConvertToSingleLine(string multiLineConfig)
    {
        if (string.IsNullOrWhiteSpace(multiLineConfig)) return string.Empty;

        var lines = multiLineConfig.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var nonEmptyLines = lines.Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line));
        return string.Join(LINE_SEPARATOR.ToString(), nonEmptyLines);
    }

    /// <summary>
    ///     Converts a single-line configuration string back into a multi-line format for display in a TextBox.
    /// </summary>
    /// <param name="singleLineConfig">The single-line string from storage (e.g., "app.exe|Button1|ID1;app.exe|Button2|ID2").</param>
    /// <returns>A multi-line string formatted for a TextBox (e.g., "app.exe|Button1|ID1\r\napp.exe|Button2|ID2").</returns>
    public static string ConvertToMultiLine(string singleLineConfig)
    {
        if (string.IsNullOrWhiteSpace(singleLineConfig)) return string.Empty;
        var lines = singleLineConfig.Split(LINE_SEPARATOR);
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    ///     Finds all button configurations for a specific executable and formats them for the named pipe.
    /// </summary>
    /// <param name="multiLineConfig">The full multi-line configuration text from the TextBox.</param>
    /// <param name="executableName">The name of the target executable (e.g., "Kam_remake.exe").</param>
    /// <returns>
    ///     A comma-separated string of button text and IDs (e.g., "Gyorsítás|F8,Lassítás|F9"). Returns an empty string if
    ///     no match is found.
    /// </returns>
    public static string GetConfigForExecutable(string multiLineConfig, string executableName)
    {
        if (string.IsNullOrWhiteSpace(multiLineConfig) || string.IsNullOrWhiteSpace(executableName))
            return string.Empty;

        var buttonParts = new List<string>();

        var lines = multiLineConfig.Split(new[] { LINE_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(PART_SEPARATOR);

            // Check if the line is valid (3 parts) and matches the executable name (case-insensitive)
            if (parts.Length == 3 && parts[0].Trim().Equals(executableName, StringComparison.OrdinalIgnoreCase))
            {
                var buttonText = parts[1].Trim();
                var buttonId = parts[2].Trim();
                buttonParts.Add($"{buttonText}{PART_SEPARATOR}{buttonId}");
            }
        }

        return string.Join(",", buttonParts);
    }

    /// <summary>
    ///     Checks if UnityPlayer.dll and version.dll exist in the given process's folder.
    /// </summary>
    /// <param name="process">The target process.</param>
    /// <returns>
    ///     True if both UnityPlayer.dll and version.dll are found in the process folder; otherwise false.
    /// </returns>
    public static bool CheckForProblematicCases(Process process)
    {
        /* if (process == null)
             throw new ArgumentNullException(nameof(process));

         try
         {
             var processFolder = Path.GetDirectoryName(process.MainModule.FileName);
             if (string.IsNullOrEmpty(processFolder))
                 return false;

             var unityDllPath = Path.Combine(processFolder, "UnityPlayer.dll");
             var versionDllPath = Path.Combine(processFolder, "version.dll");
             var winhttpDllPath = Path.Combine(processFolder, "winhttp.dll");

             var unityExists = File.Exists(unityDllPath);
             var versionExists = File.Exists(versionDllPath);
             var winhttpExists = File.Exists(winhttpDllPath);

             return unityExists && (versionExists || winhttpExists);
         }
         catch (Win32Exception)
         {
             return false;
         }
         catch (Exception ex)
         {
             Debug.WriteLine($"Error checking DLLs for process {process.ProcessName}: {ex}");
             return false;
         }*/
        return false;
    }

    #region WinAPI

    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        [MarshalAs(UnmanagedType.Bool)] public bool bInheritHandle;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern SafePipeHandle CreateNamedPipe(
        string lpName,
        uint dwOpenMode,
        uint dwPipeMode,
        uint nMaxInstances,
        uint nOutBufferSize,
        uint nInBufferSize,
        uint nDefaultTimeOut,
        ref SECURITY_ATTRIBUTES securityAttributes);

    private const uint PIPE_ACCESS_DUPLEX = 0x00000003;
    private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
    private const uint PIPE_TYPE_BYTE = 0x00000000;
    private const uint PIPE_READMODE_BYTE = 0x00000000;
    private const uint PIPE_WAIT = 0x00000000;

    #endregion
}