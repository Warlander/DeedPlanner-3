using System.Collections.Generic;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public readonly struct PackageDetails
    {
        public string Id { get; }
        public string Description { get; }
        public string LatestVersion { get; }
        public IReadOnlyList<string> Versions { get; }
        public string RepositoryUrl { get; }
        public string RegistryUrl { get; }

        public PackageDetails(string id, string description, string latestVersion, IReadOnlyList<string> versions, string repositoryUrl, string registryUrl)
        {
            Id = id;
            Description = description;
            LatestVersion = latestVersion;
            Versions = versions;
            RepositoryUrl = repositoryUrl;
            RegistryUrl = registryUrl;
        }
    }
}
