using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UpmPackageInfo = UnityEditor.PackageManager.PackageInfo;
using StatusCode = UnityEditor.PackageManager.StatusCode;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public class RegistryApiClient
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly Regex ChangelogSectionRegex = new Regex(
            @"## \[(\d+\.\d+(?:\.\d+)?)\][^\n]*\n([\s\S]*?)(?=\n## \[|$)",
            RegexOptions.Compiled);
        private static readonly Regex ChangelogFieldRegex = new Regex(
            @"""changelog""\s*:\s*""(https?://[^""\\]+)""",
            RegexOptions.Compiled);


        public async Task<IReadOnlyList<PackageSummary>> FetchPackagesAsync(IReadOnlyList<RegistryScope> registries)
        {
            SearchRequest searchRequest = Client.SearchAll(false);
            ListRequest listRequest = Client.List(false);

            UpmPackageInfo[] searchResults = await WaitForRequestAsync(searchRequest);
            PackageCollection installedPackages = await WaitForListRequestAsync(listRequest);

            var installedSources = new Dictionary<string, PackageSource>();
            var installedVersions = new Dictionary<string, string>();
            foreach (UpmPackageInfo pkg in installedPackages)
            {
                installedSources[pkg.name] = pkg.source;
                installedVersions[pkg.name] = pkg.version;
            }

            var summaries = new List<PackageSummary>();
            foreach (UpmPackageInfo pkg in searchResults)
            {
                RegistryScope match = registries.FirstOrDefault(r => pkg.name.StartsWith(r.Scope));
                if (match.Scope == null)
                    continue;

                PackageInstallStatus status;
                if (!installedSources.TryGetValue(pkg.name, out PackageSource source))
                    status = PackageInstallStatus.NotInProject;
                else if (source == PackageSource.Embedded)
                    status = PackageInstallStatus.Embedded;
                else
                    status = PackageInstallStatus.InstalledFromRegistry;

                string installedVersion = installedVersions.TryGetValue(pkg.name, out string iv) ? iv : null;
                summaries.Add(new PackageSummary(pkg.name, pkg.displayName, pkg.description, pkg.version, match.RegistryUrl, status, installedVersion));
            }

            return summaries;
        }

        public async Task<PackageDetails> FetchPackageDetailsAsync(string id, string registryUrl)
        {
            SearchRequest request = Client.Search(id, false);
            UpmPackageInfo[] results = await WaitForRequestAsync(request);

            if (results == null || results.Length == 0)
                return new PackageDetails(id, "", "", "", Array.Empty<string>(), "", "", registryUrl);

            UpmPackageInfo pkg = results[0];

            string[] allVersions = pkg.versions?.all ?? Array.Empty<string>();
            string[] versionsCopy = (string[])allVersions.Clone();
            Array.Reverse(versionsCopy);

            string repositoryUrl = pkg.repository?.url ?? "";
            string changelogUrl = await FetchChangelogUrlAsync(registryUrl, pkg.name);

            return new PackageDetails(pkg.name, pkg.displayName, pkg.description, pkg.version, versionsCopy, repositoryUrl, changelogUrl, registryUrl);
        }

        public async Task AddPackageAsync(string id, string version)
        {
            AddRequest request = Client.Add($"{id}@{version}");
            await WaitForAddRequestAsync(request);
        }

        public async Task RemovePackageAsync(string id)
        {
            RemoveRequest request = Client.Remove(id);
            await WaitForRemoveRequestAsync(request);
        }

        public async Task<IReadOnlyDictionary<string, string>> FetchChangelogsAsync(string changelogUrl, string repositoryUrl)
        {
            string content = null;

            if (!string.IsNullOrEmpty(changelogUrl))
            {
                string fetchUrl = changelogUrl.Contains("github.com") ? NormalizeGitHubUrl(changelogUrl) : changelogUrl;
                content = await TryFetchAsync(fetchUrl);
            }

            if (content == null)
            {
                string rawBase = ParseGitHubRawBase(repositoryUrl);
                if (rawBase != null)
                    content = await TryFetchAsync(rawBase + "/main/CHANGELOG.md")
                           ?? await TryFetchAsync(rawBase + "/master/CHANGELOG.md");
            }

            if (content == null)
                return new Dictionary<string, string>();

            var changelogs = new Dictionary<string, string>();
            foreach (Match match in ChangelogSectionRegex.Matches(content))
                changelogs[match.Groups[1].Value] = match.Groups[2].Value.Trim();
            return changelogs;
        }

        private async Task<string> FetchChangelogUrlAsync(string registryUrl, string id)
        {
            try
            {
                string json = await TryFetchAsync(registryUrl.TrimEnd('/') + "/" + id);
                if (json == null)
                    return "";

                Match match = ChangelogFieldRegex.Match(json);
                return match.Success ? match.Groups[1].Value : "";
            }
            catch
            {
                return "";
            }
        }

        private static string NormalizeGitHubUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            // https://github.com/{owner}/{repo}/blob/{branch}/{path}
            // https://github.com/{owner}/{repo}/raw/{branch}/{path}
            // → https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}
            int idx = url.IndexOf("github.com/", StringComparison.Ordinal);
            if (idx < 0 || url.StartsWith("https://raw.githubusercontent.com"))
                return url;

            string after = url.Substring(idx + "github.com/".Length);
            string[] parts = after.Split('/');
            if (parts.Length < 4)
                return url;

            string segment = parts[2]; // "blob" or "raw"
            if (segment != "blob" && segment != "raw")
                return url;

            string owner = parts[0];
            string repo = parts[1];
            string rest = string.Join("/", parts, 3, parts.Length - 3);
            return "https://raw.githubusercontent.com/" + owner + "/" + repo + "/" + rest;
        }

        private static string ParseGitHubRawBase(string repositoryUrl)
        {
            if (string.IsNullOrEmpty(repositoryUrl))
                return null;

            string url = repositoryUrl;
            if (url.StartsWith("git+"))
                url = url.Substring(4);
            if (url.EndsWith(".git"))
                url = url.Substring(0, url.Length - 4);

            if (url.StartsWith("git@github.com:"))
                url = "https://github.com/" + url.Substring("git@github.com:".Length);

            if (!url.Contains("github.com"))
                return null;

            int idx = url.IndexOf("github.com/", StringComparison.Ordinal);
            if (idx < 0)
                return null;

            string ownerRepo = url.Substring(idx + "github.com/".Length).TrimEnd('/');
            string[] parts = ownerRepo.Split('/');
            if (parts.Length < 2)
                return null;

            return "https://raw.githubusercontent.com/" + parts[0] + "/" + parts[1];
        }

        private static async Task<string> TryFetchAsync(string url)
        {
            try
            {
                HttpResponseMessage response = await HttpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        private static Task<UpmPackageInfo[]> WaitForRequestAsync(SearchRequest request)
        {
            var tcs = new TaskCompletionSource<UpmPackageInfo[]>();

            void CheckCompletion()
            {
                if (!request.IsCompleted)
                    return;

                EditorApplication.update -= CheckCompletion;

                if (request.Status == StatusCode.Success)
                    tcs.SetResult(request.Result);
                else
                    tcs.SetException(new Exception(request.Error?.message ?? "Unknown error"));
            }

            EditorApplication.update += CheckCompletion;
            return tcs.Task;
        }

        private static Task<PackageCollection> WaitForListRequestAsync(ListRequest request)
        {
            var tcs = new TaskCompletionSource<PackageCollection>();

            void CheckCompletion()
            {
                if (!request.IsCompleted)
                    return;

                EditorApplication.update -= CheckCompletion;

                if (request.Status == StatusCode.Success)
                    tcs.SetResult(request.Result);
                else
                    tcs.SetException(new Exception(request.Error?.message ?? "Unknown error"));
            }

            EditorApplication.update += CheckCompletion;
            return tcs.Task;
        }

        private static Task WaitForAddRequestAsync(AddRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();

            void CheckCompletion()
            {
                if (!request.IsCompleted)
                    return;

                EditorApplication.update -= CheckCompletion;

                if (request.Status == StatusCode.Success)
                    tcs.SetResult(true);
                else
                    tcs.SetException(new Exception(request.Error?.message ?? "Unknown error"));
            }

            EditorApplication.update += CheckCompletion;
            return tcs.Task;
        }

        private static Task WaitForRemoveRequestAsync(RemoveRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();

            void CheckCompletion()
            {
                if (!request.IsCompleted)
                    return;

                EditorApplication.update -= CheckCompletion;

                if (request.Status == StatusCode.Success)
                    tcs.SetResult(true);
                else
                    tcs.SetException(new Exception(request.Error?.message ?? "Unknown error"));
            }

            EditorApplication.update += CheckCompletion;
            return tcs.Task;
        }
    }
}
