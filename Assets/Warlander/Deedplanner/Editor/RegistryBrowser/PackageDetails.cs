using System.Collections.Generic;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public readonly struct PackageDetails
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string LatestVersion { get; }
        public IReadOnlyList<string> Versions { get; }
        public string RepositoryUrl { get; }
        public string ChangelogUrl { get; }
        public string RegistryUrl { get; }

        public PackageDetails(string id, string displayName, string description, string latestVersion, IReadOnlyList<string> versions, string repositoryUrl, string changelogUrl, string registryUrl)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            LatestVersion = latestVersion;
            Versions = versions;
            RepositoryUrl = repositoryUrl;
            ChangelogUrl = changelogUrl;
            RegistryUrl = registryUrl;
        }
    }
}
