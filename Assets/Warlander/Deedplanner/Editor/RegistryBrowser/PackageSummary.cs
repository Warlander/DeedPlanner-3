namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public readonly struct PackageSummary
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string LatestVersion { get; }
        public string RegistryUrl { get; }
        public PackageInstallStatus Status { get; }
        public string InstalledVersion { get; }

        public PackageSummary(string id, string displayName, string description, string latestVersion, string registryUrl, PackageInstallStatus status, string installedVersion)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            LatestVersion = latestVersion;
            RegistryUrl = registryUrl;
            Status = status;
            InstalledVersion = installedVersion;
        }
    }
}
