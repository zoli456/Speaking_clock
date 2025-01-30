using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Speaking_Clock;

internal static class ComputerInfo
{
    private static readonly List<string> Returnlist = new();
    /// <summary>
    /// Get the name of the operating system and its version.
    /// </summary>
    /// <returns></returns>
    internal static string WindowsNevEsVerzio()
    {
        try
        {
            // OS Name and Version
            var osName = "Ismeretlen";
            var osVersion = "Ismeretlen";
            var buildNumber = "Ismeretlen";

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                foreach (var obj in searcher.Get())
                {
                    osName = obj["Caption"]?.ToString() ?? "Ismeretlen";
                    osVersion = obj["Version"]?.ToString() ?? "Ismeretlen";
                    buildNumber = obj["BuildNumber"]?.ToString() ?? "Ismeretlen";
                    break; // Only one object is expected
                }
            }

            // Architecture
            var architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

            // Format the output
            return $"{osName} ({architecture}), Verzió: {osVersion}, Build szám: {buildNumber}";
        }
        catch (Exception ex)
        {
            return $"Error retrieving Windows details: {ex.Message}";
        }
    }
    /// <summary>
    /// Get the name of the processor.
    /// </summary>
    /// <returns></returns>
    internal static string ProcesszorNev()
    {
        using (var kereso = new ManagementObjectSearcher("select Name from Win32_Processor"))
        {
            foreach (var obj in kereso.Get()) return obj["Name"].ToString();
        }

        return "Ismeretlen processzor";
    }
    /// <summary>
    /// Get the amount of RAM installed in the system.
    /// </summary>
    /// <returns></returns>
    internal static string RamMeret()
    {
        using (var kereso = new ManagementObjectSearcher("select TotalPhysicalMemory from Win32_ComputerSystem"))
        {
            foreach (var obj in kereso.Get())
            {
                var byteMeret = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                var gb = byteMeret / (1024 * 1024 * 1024.0);
                return $"{gb:F2} GB";
            }
        }

        return "Ismeretlen RAM méret";
    }
    /// <summary>
    /// Get the name of the graphics card.
    /// </summary>
    /// <returns></returns>
    internal static string VgaNev()
    {
        using (var kereso = new ManagementObjectSearcher("select Name, AdapterRAM from Win32_VideoController"))
        {
            foreach (var obj in kereso.Get())
            {
                var nev = obj["Name"]?.ToString() ?? "Ismeretlen VGA";
                return $"{nev}";
            }
        }

        return "Ismeretlen VGA";
    }

    /// <summary>
    /// Get the Windows activation status.
    /// </summary>
    /// <returns></returns>
    internal static string WindowsAktivaciosAllapot()
    {
        using (var kereso = new ManagementObjectSearcher(
                   "select LicenseStatus from SoftwareLicensingProduct where PartialProductKey is not null and ApplicationId='55c92734-d682-4d71-983e-d6ec3f16059f'"))
        {
            foreach (var obj in kereso.Get())
            {
                var allapot = Convert.ToInt32(obj["LicenseStatus"]);
                switch (allapot)
                {
                    case 0: return "Nincs licencelve";
                    case 1: return "Licencelve";
                    case 2: return "Kezdeti türelmi időszak";
                    case 3: return "Toleranciaidőn kívüli türelmi idő";
                    case 4: return "Nem eredeti türelmi időszak";
                    case 5: return "Értesítés";
                    case 6: return "Kiterjesztett türelmi idő";
                    default: return "Ismeretlen aktivációs állapot";
                }
            }
        }

        return "Ismeretlen aktivációs állapot";
    }
    /// <summary>
    /// Get the name of the motherboard.
    /// </summary>
    /// <returns></returns>
    internal static string AlaplapNev()
    {
        using (var kereso = new ManagementObjectSearcher("select Product, Manufacturer from Win32_BaseBoard"))
        {
            foreach (var obj in kereso.Get())
            {
                var gyarto = obj["Manufacturer"]?.ToString() ?? "Ismeretlen gyártó";
                var termek = obj["Product"]?.ToString() ?? "Ismeretlen termék";
                return $"{gyarto} {termek}";
            }
        }

        return "Ismeretlen alaplap";
    }
    /// <summary>
    /// Get the list of running processes.
    /// </summary>
    /// <returns></returns>
    internal static List<string> FutoFolyamatokListazasa()
    {
        Returnlist.Clear();
        Debug.WriteLine("Futó folyamatok:");
        foreach (var folyamat in Process.GetProcesses())
            try
            {
                Returnlist.Add($"{folyamat.ProcessName} (ID: {folyamat.Id})");
            }
            catch
            {
                Returnlist.Add("Hiba a folyamat információinak lekérdezésekor.");
            }

        return Returnlist;
    }
    /// <summary>
    /// Get the list of installed programs.
    /// </summary>
    /// <returns></returns>
    internal static string VgaDriverVerzio()
    {
        using (var kereso = new ManagementObjectSearcher("select DriverVersion from Win32_VideoController"))
        {
            foreach (var obj in kereso.Get()) return obj["DriverVersion"]?.ToString() ?? "Ismeretlen driver verzió";
        }

        return "Ismeretlen driver verzió";
    }
    /// <summary>
    /// Get the list of installed programs.
    /// </summary>
    /// <returns></returns>
    internal static List<string> TelepitettProgramokListazasa()
    {
        Returnlist.Clear();
        Debug.WriteLine("Telepített programok:");
        var registryKulcs = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        using (var kulcs = Registry.LocalMachine.OpenSubKey(registryKulcs))
        {
            if (kulcs != null)
                foreach (var alkulcsNev in kulcs.GetSubKeyNames())
                    using (var alKulcs = kulcs.OpenSubKey(alkulcsNev))
                    {
                        var megjelenesiNev = alKulcs?.GetValue("DisplayName")?.ToString();
                        if (!string.IsNullOrEmpty(megjelenesiNev)) Returnlist.Add(megjelenesiNev);
                    }
        }

        return Returnlist;
    }
    /// <summary>
    /// Get the list of installed antivirus software.
    /// </summary>
    /// <returns></returns>
    internal static List<string> VirusScannerNev()
    {
        Returnlist.Clear();
        try
        {
            using (var kereso = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct"))
            {
                foreach (var obj in kereso.Get())
                    Returnlist.Add(obj["displayName"]?.ToString() ?? "Ismeretlen víruskereső");
            }
        }
        catch (Exception ex)
        {
            Returnlist.Add($"Hiba a víruskereső információ lekérdezésekor: {ex.Message}");
        }

        return Returnlist;
    }
    /// <summary>
    /// Check if Secure Boot is enabled on the system.
    /// </summary>
    /// <returns></returns>
    internal static string IsSecureBootEnabled()
    {
        try
        {
            // Check Secure Boot state in the Windows Registry
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State"))
            {
                if (key != null)
                {
                    var value = key.GetValue("UEFISecureBootEnabled");
                    if (value != null) return (int)value == 1 ? "Igen" : "Nem"; // 1 indicates Secure Boot is enabled
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking Secure Boot status: {ex.Message}");
            return "Hiba";
        }

        return "Hiba";
    }
    /// <summary>
    /// Check if TPM is enabled on the system
    /// </summary>
    /// <returns></returns>
    internal static string IsTpmEnabled()
    {
        var tpmEnabled = false;

        try
        {
            using (var searcher =
                   new ManagementObjectSearcher(@"root\CIMv2\Security\MicrosoftTpm", "SELECT * FROM Win32_Tpm"))
            {
                foreach (var obj in searcher.Get())
                    if (obj["IsEnabled_InitialValue"] != null)
                        tpmEnabled = Convert.ToBoolean(obj["IsEnabled_InitialValue"]);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking TPM status: {ex.Message}");
            return "Hiba";
        }

        return tpmEnabled ? "Igen" : "Nem";
    }
}