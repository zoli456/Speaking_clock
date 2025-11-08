using System.ComponentModel;
using System.Diagnostics;
using Speaking_Clock;
using Vanara.PInvoke;

namespace Speaking_clock.Overlay;

/// <summary>
///     Provides functionality to inject a DLL into a remote process using the LoadLibrary method.
///     It automatically handles both 32-bit and 64-bit processes.
/// </summary>
public static class DllInjector
{
    private static readonly string dll32Path = "SpeakingClockOverlayx86.dll";
    private static readonly string dll64Path = "SpeakingClockOverlayx64.dll";
    internal static Process CurrentProcess;

    internal static void InjectToForeground()
    {
        // Get process ID from window handle
        User32.GetWindowThreadProcessId(FullScreenChecker.GetForegroundFullscreenWindow(), out var pid);
        if (pid == 0)
        {
            Debug.WriteLine("No process found for foreground window.");
            return;
        }


        try
        {
            CurrentProcess = Process.GetProcessById((int)pid);
        }
        catch (ArgumentException)
        {
            Debug.WriteLine("Process no longer exists.");
            return;
        }

        // Determine if target process is 32-bit
        var is32Bit = IsProcess32Bit(CurrentProcess);

        // Pick correct DLL and injector
        var dllPath = is32Bit ? dll32Path : dll64Path;
        var injectorExe = is32Bit ? "Injector_x86.exe" : "Injector_x64.exe";

        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Beallitasok.BasePath, "DllInjector", injectorExe),
            Arguments =
                $"inject \"{CurrentProcess.ProcessName}.exe\" \"{Path.Combine(Beallitasok.BasePath, "DllInjector", dllPath)}\" 1000",
            WorkingDirectory = Path.Combine(Beallitasok.BasePath, "DllInjector"),
            CreateNoWindow = true,
            UseShellExecute = true, // Required for Verb
            Verb = "runas", // Run as administrator
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(startInfo);

        Debug.WriteLine($"Injected {dllPath} into {CurrentProcess.ProcessName}.exe ({(is32Bit ? "x86" : "x64")}).");
        CurrentProcess.WaitForExit();
        Beallitasok.DeactivateLegacyOverlay();
        Beallitasok.DLL_Injected = false;
        Debug.WriteLine($"{CurrentProcess.ProcessName} exited, resetting DLL injection state.");
    }


    private static bool IsProcess32Bit(Process process)
    {
        if (!Environment.Is64BitOperatingSystem)
            return true;

        if (!Kernel32.IsWow64Process(process.Handle, out var isWow64))
            throw new Win32Exception();

        return isWow64;
    }

    internal static bool IsForceSimpleOverlay(string procName)
    {
        var UserBlockedProcesses = Beallitasok.ÁtfedésSection["UseSimpleOverlay"].StringValue.Split(";");
        string[] blockedProcesses =
        {
            "chrome", "msedge", "firefox", "opera", "brave", "iexplore",
            Path.GetFileNameWithoutExtension(Beallitasok.DefaultBrowerPath)
        };

        return blockedProcesses.Contains(Path.GetFileNameWithoutExtension(procName),
                   StringComparer.OrdinalIgnoreCase) ||
               UserBlockedProcesses.Contains(Path.GetFileName(procName), StringComparer.OrdinalIgnoreCase);
    }

    internal static bool IsShowSimpleOverlay(string procName)
    {
        string[] blockedProcesses =
        {
            "explorer", "SearchUI", "RuntimeBroker", "ApplicationFrameHost", "ShellExperienceHost", "dwm", "idlewatch",
            "steamwebhelper", "League of Legends", "zoom", "ms-teams", "teams", "discord", "spotify"
        };

        return blockedProcesses.Any(builtIn =>
            Path.GetFileNameWithoutExtension(procName)
                .StartsWith(builtIn, StringComparison.OrdinalIgnoreCase));
    }

    internal static bool IsForcedExternal(string procName)
    {
        var ForcedProcesses = Beallitasok.ÁtfedésSection["DontHookRendering"].StringValue
            .Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string[] ForcedProcessesBuiltIn =
        {
            "hd-player", "vlc", "potplayer", "mpc-hc", "wmplayer", "PowerDVD"
        };

        return ForcedProcessesBuiltIn.Any(builtIn =>
                   Path.GetFileNameWithoutExtension(procName)
                       .StartsWith(builtIn, StringComparison.OrdinalIgnoreCase))
               || ForcedProcesses.Contains(Path.GetFileName(procName), StringComparer.OrdinalIgnoreCase);
    }
}