using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ToolsUpdater;

// Installs the LabVIEW add-ons for WPILib (https://github.com/ni/wpilib-labview) into LabVIEW's
// standard add-ons folder, where LabVIEW 2024 Q1 and later find them. The installer extracts them
// into labview/lvaddons, then runs "ToolsUpdater --install-labview" with administrator rights,
// because the add-ons folder is a system folder.
internal static class LabVIEWAddons
{
    // LabVIEW's LVAddons folder on each OS. On Windows, LabVIEW uses the Path value of
    // HKLM\SOFTWARE\National Instruments\LabVIEW\LVAddons (64-bit view) when it's set.
    public static string DefaultLocation =>
        OperatingSystem.IsWindows() ? RegisteredWindowsLocation() ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NI", "LVAddons")
        : OperatingSystem.IsMacOS() ? "/Library/Application Support/National Instruments/LVAddons"
        : "/usr/local/natinst/share/lvaddons";

    [SupportedOSPlatform("windows")]
    private static string? RegisteredWindowsLocation()
    {
        using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = hklm.OpenSubKey(@"SOFTWARE\National Instruments\LabVIEW\LVAddons");
        return key?.GetValue("Path") is string path && !string.IsNullOrWhiteSpace(path) ? path : null;
    }

    // Copies each add-on (source/<addon>) into target, first deleting that add-on's folder there, so
    // nothing from an earlier install (files or versions) is left behind. Other add-ons are left alone.
    public static bool Install(string source, string target)
    {
        if (!Directory.Exists(source))
        {
            Console.WriteLine($"No LabVIEW add-ons in {source}");
            return false;
        }

        try
        {
            foreach (var addon in Directory.EnumerateDirectories(source))
            {
                var dest = Path.Combine(target, Path.GetFileName(addon));
                if (Directory.Exists(dest))
                {
                    Directory.Delete(dest, recursive: true);
                }
                CopyDirectory(addon, dest);
                Console.WriteLine($"Installed {dest}");
            }
            return true;
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            Console.WriteLine($"Couldn't install the LabVIEW add-ons into {target}: {e.Message}");
            return false;
        }
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
        }
        foreach (var dir in Directory.EnumerateDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
        }
    }
}
