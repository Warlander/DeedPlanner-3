namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public readonly struct PackageSummary
    {
        public string Id { get; }
        public string Description { get; }
        public string LatestVersion { get; }
        public string RegistryUrl { get; }

        public PackageSummary(string id, string description, string latestVersion, string registryUrl)
        {
            Id = id;
            Description = description;
            LatestVersion = latestVersion;
            RegistryUrl = registryUrl;
        }
    }
}
