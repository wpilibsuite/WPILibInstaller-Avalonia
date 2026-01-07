using System;
using System.Collections.Generic;
using System.IO;

namespace WPILibInstaller.Utils
{
    public static class InstallerFileUtils
    {
        public static bool TryFindInstallerFiles(out string? resourcesFile, out string? artifactsFile)
        {
            resourcesFile = null;
            artifactsFile = null;

            var artifactsExt = OperatingSystem.IsWindows() ? ".zip" : ".tar.gz";

            foreach (var dir in CandidateSearchDirectories())
            {
                Console.WriteLine($"Trying to autoload install files in {dir}...");
                if (!Directory.Exists(dir)) continue;

                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    var name = Path.GetFileName(file);

                    Console.WriteLine($"Checking {file}...");

                    if (resourcesFile == null &&
                        name.Contains("-resources", StringComparison.OrdinalIgnoreCase) &&
                        name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        resourcesFile = file;
                    }

                    if (artifactsFile == null &&
                        name.Contains("-artifacts", StringComparison.OrdinalIgnoreCase) &&
                        name.EndsWith(artifactsExt, StringComparison.OrdinalIgnoreCase))
                    {
                        artifactsFile = file;
                    }

                    if (resourcesFile != null && artifactsFile != null)
                        return true;
                }
            }

            return resourcesFile != null && artifactsFile != null;
        }

        public static string ComputeInstallDirectory(bool allUsers, string frcYear)
        {
            var homeDir = allUsers && OperatingSystem.IsWindows()
                ? Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments)
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return Path.Combine(homeDir, "wpilib", frcYear);
        }

        private static IEnumerable<string> CandidateSearchDirectories()
        {
            yield return Directory.GetCurrentDirectory();

            // macOS special locations
            if (OperatingSystem.IsMacOS())
                yield return "/Volumes/WPILibInstaller";
        }
    }
}
