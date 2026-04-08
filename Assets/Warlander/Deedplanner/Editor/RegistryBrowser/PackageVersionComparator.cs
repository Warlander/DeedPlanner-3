namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    internal enum VersionUpdateLevel { None, Patch, Minor, Major }

    internal static class PackageVersionComparator
    {
        internal static VersionUpdateLevel Compare(string installed, string latest)
        {
            if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(latest))
                return VersionUpdateLevel.None;

            if (!TryParse(installed, out int instMajor, out int instMinor, out int instPatch))
                return VersionUpdateLevel.None;
            if (!TryParse(latest, out int latMajor, out int latMinor, out int latPatch))
                return VersionUpdateLevel.None;

            if (latMajor > instMajor)
                return VersionUpdateLevel.Major;
            if (latMajor < instMajor)
                return VersionUpdateLevel.None;

            if (latMinor > instMinor)
                return VersionUpdateLevel.Minor;
            if (latMinor < instMinor)
                return VersionUpdateLevel.None;

            if (latPatch > instPatch)
                return VersionUpdateLevel.Patch;

            return VersionUpdateLevel.None;
        }

        private static bool TryParse(string version, out int major, out int minor, out int patch)
        {
            major = 0;
            minor = 0;
            patch = 0;

            if (string.IsNullOrEmpty(version))
                return false;

            string[] parts = version.Split('.');
            if (parts.Length < 1 || !int.TryParse(parts[0], out major))
                return false;
            if (parts.Length >= 2 && !int.TryParse(parts[1], out minor))
                return false;
            if (parts.Length >= 3)
            {
                // Strip pre-release suffixes like "-preview.1"
                string patchStr = parts[2];
                int dashIndex = patchStr.IndexOf('-');
                if (dashIndex >= 0)
                    patchStr = patchStr.Substring(0, dashIndex);
                if (!int.TryParse(patchStr, out patch))
                    return false;
            }

            return true;
        }
    }
}
