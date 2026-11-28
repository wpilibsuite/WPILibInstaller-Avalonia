namespace WPILibInstaller.Utils
{
    internal static class ArchivePathUtils
    {
        public static string NormalizeEntryName(string entryName)
        {
            return entryName.Replace('\\', '/').TrimStart('.', '/');
        }

        public static string FolderPrefix(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                // A prefix that cannot match any real archive entry.
                return "!/";
            }

            return NormalizeEntryName(folder).TrimEnd('/') + "/";
        }

        public static string FilePath(string folder, string fileName)
        {
            return FolderPrefix(folder) + NormalizeEntryName(fileName);
        }

        public static bool Matches(string entryName, string prefix)
        {
            return prefix.Length > 0 && entryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
