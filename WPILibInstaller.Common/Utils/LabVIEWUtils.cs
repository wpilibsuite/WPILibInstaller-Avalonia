using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WPILibInstaller.Utils
{
    public static class LabVIEWUtils
    {
        // MinimumSupportedLVVersion (25.0) in the add-ons' lvaddoninfo.json
        private const int MinimumYear = 2025;

        private static readonly Regex Year = new(@"\b(20\d\d)\b");

        // LabVIEW installs under either vendor folder in Program Files on Windows
        private static readonly string[] WindowsVendors = ["National Instruments", "NI"];

        // LabVIEW 2025 is version 25
        private const int MinimumMajorVersion = MinimumYear - 2000;

        // Whether LabVIEW 2025 or later is installed, so installing the LabVIEW add-ons is worth
        // asking for administrator rights.
        public static bool IsLabVIEWInstalled()
        {
            var installed = LabVIEWFolders().Any(dir =>
            {
                var year = Year.Match(Path.GetFileName(dir));
                return year.Success && int.Parse(year.Groups[1].Value, CultureInfo.InvariantCulture) >= MinimumYear;
            });
            return installed || (OperatingSystem.IsWindows() && IsRegisteredLabVIEWSupported());
        }

        // Every LabVIEW development system registers itself as the LabVIEW.Application ActiveX
        // server when it starts, so this also finds a LabVIEW that wasn't installed in Program
        // Files, or whose executable has another name.
        [SupportedOSPlatform("windows")]
        private static bool IsRegisteredLabVIEWSupported()
        {
            try
            {
                var exe = RegisteredLabVIEW();
                return exe != null && File.Exists(exe)
                    && FileVersionInfo.GetVersionInfo(exe).FileMajorPart >= MinimumMajorVersion;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                return false;
            }
        }

        // The executable in LocalServer32 of LabVIEW.Application's CLSID, which LabVIEW writes as
        // "<path to the executable> /Automation"
        [SupportedOSPlatform("windows")]
        private static string? RegisteredLabVIEW()
        {
            using var clsidKey = Registry.ClassesRoot.OpenSubKey(@"LabVIEW.Application\CLSID");
            if (clsidKey?.GetValue(null) is not string clsid)
            {
                return null;
            }
            using var serverKey = Registry.ClassesRoot.OpenSubKey($@"CLSID\{clsid}\LocalServer32");
            if (serverKey?.GetValue(null) is not string server)
            {
                return null;
            }
            server = server.Trim();
            if (server.StartsWith('"'))
            {
                var end = server.IndexOf('"', 1);
                return end > 0 ? server[1..end] : null;
            }
            var exeEnd = server.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            return exeEnd > 0 ? server[..(exeEnd + 4)] : server;
        }

        private static IEnumerable<string> LabVIEWFolders()
        {
            if (OperatingSystem.IsWindows())
            {
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                return WindowsVendors
                    .SelectMany(vendor => Directories(Path.Combine(programFiles, vendor), "LabVIEW *"));
            }
            if (OperatingSystem.IsMacOS())
            {
                return Directories("/Applications/National Instruments", "LabVIEW *");
            }
            // A LabVIEW that wasn't installed there still keeps its preferences in
            // ~/natinst/.config/LabVIEW-<year>[-<release>] once it has run
            var preferences = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "natinst", ".config");
            return Directories("/usr/local/natinst", "LabVIEW-*").Concat(Directories(preferences, "LabVIEW-*"));
        }

        private static IEnumerable<string> Directories(string path, string pattern) =>
            Directory.Exists(path) ? Directory.EnumerateDirectories(path, pattern) : [];
    }
}
