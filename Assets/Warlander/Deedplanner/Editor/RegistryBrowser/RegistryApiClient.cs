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
        private static readonly Regex CommitShaRegex = new Regex(
            @"""sha""\s*:\s*""([0-9a-f]{40})""",
            RegexOptions.Compiled);
        private static readonly Regex CommitMessageRegex = new Regex(
            @"""message""\s*:\s*""((?:[^""\\]|\\.)*)""",
            RegexOptions.Compiled);
        private static readonly Regex TagNameRegex = new Regex(
            @"""name""\s*:\s*""([^""]+)""",
            RegexOptions.Compiled);
        private static readonly Regex TagCommitShaRegex = new Regex(
            @"""commit""\s*:\s*\{[^}]*""sha""\s*:\s*""([0-9a-f]{40})""",
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
                else if (source == PackageSource.Embedded ||
                         (source == PackageSource.Local && GitEmbedOperations.IsEmbedded(pkg.name)))
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

        public async Task<IReadOnlyList<CommitInfo>> FetchCommitsAsync(string repositoryUrl, int count = 20)
        {
            string ownerRepo = ParseOwnerRepo(repositoryUrl);
            if (ownerRepo == null)
                return Array.Empty<CommitInfo>();

            string apiBase = $"https://api.github.com/repos/{ownerRepo}";

            Task<string> commitsTask = TryFetchGitHubApiAsync($"{apiBase}/commits?sha=master&per_page={count}");
            Task<string> tagsTask = TryFetchGitHubApiAsync($"{apiBase}/tags?per_page=100");
            await Task.WhenAll(commitsTask, tagsTask);

            string commitsJson = commitsTask.Result;

            // If master branch returned nothing, try main
            if (string.IsNullOrEmpty(commitsJson))
                commitsJson = await TryFetchGitHubApiAsync($"{apiBase}/commits?sha=main&per_page={count}");

            if (string.IsNullOrEmpty(commitsJson))
                return Array.Empty<CommitInfo>();

            var tagsBySha = ParseTagsBySha(tagsTask.Result ?? "");
            return ParseCommits(commitsJson, tagsBySha, count);
        }

        private static Dictionary<string, List<string>> ParseTagsBySha(string tagsJson)
        {
            var result = new Dictionary<string, List<string>>();
            if (string.IsNullOrEmpty(tagsJson))
                return result;

            MatchCollection nameMatches = TagNameRegex.Matches(tagsJson);
            MatchCollection shaMatches = TagCommitShaRegex.Matches(tagsJson);

            int pairCount = Math.Min(nameMatches.Count, shaMatches.Count);
            for (int i = 0; i < pairCount; i++)
            {
                string tagName = nameMatches[i].Groups[1].Value;
                string sha = shaMatches[i].Groups[1].Value;
                if (!result.TryGetValue(sha, out List<string> tags))
                {
                    tags = new List<string>();
                    result[sha] = tags;
                }
                tags.Add(tagName);
            }

            return result;
        }

        private static IReadOnlyList<CommitInfo> ParseCommits(string commitsJson, Dictionary<string, List<string>> tagsBySha, int maxCount)
        {
            var commits = new List<CommitInfo>();
            MatchCollection shaMatches = CommitShaRegex.Matches(commitsJson);
            MatchCollection msgMatches = CommitMessageRegex.Matches(commitsJson);

            // GitHub commit objects contain multiple sha fields (top-level, tree.sha, parents[].sha).
            // Top-level sha always appears BEFORE its commit.message. Tree/parent shas appear AFTER it.
            // A sha is top-level when no other sha appears between it and the next message match.
            int shaCount = shaMatches.Count;
            int msgIdx = 0;
            for (int shaIdx = 0; shaIdx < shaCount; shaIdx++)
            {
                if (commits.Count >= maxCount)
                    break;

                Match shaMatch = shaMatches[shaIdx];

                // Find the first message that comes after this sha
                while (msgIdx < msgMatches.Count && msgMatches[msgIdx].Index <= shaMatch.Index)
                    msgIdx++;

                if (msgIdx >= msgMatches.Count)
                    break;

                Match msgMatch = msgMatches[msgIdx];

                // Only pair when there is no other sha between this sha and the message.
                // Top-level sha: message comes before tree/parent shas.
                // Nested sha: the next sha also precedes the message.
                bool nextShaBeforeMsg = (shaIdx + 1 < shaCount) &&
                                        (shaMatches[shaIdx + 1].Index < msgMatch.Index);
                if (nextShaBeforeMsg)
                    continue;

                string sha = shaMatch.Groups[1].Value;
                string rawMessage = msgMatch.Groups[1].Value;
                string message = UnescapeJson(rawMessage).Split('\n')[0].Trim();

                string[] tags = tagsBySha.TryGetValue(sha, out List<string> tagList)
                    ? tagList.ToArray()
                    : Array.Empty<string>();

                commits.Add(new CommitInfo(sha, message, tags));
                msgIdx++;
            }

            return commits;
        }

        private static string UnescapeJson(string s)
        {
            return s
                .Replace("\\n", "\n")
                .Replace("\\r", "")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }

        private static string ParseOwnerRepo(string repositoryUrl)
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

            return parts[0] + "/" + parts[1];
        }

        private static async Task<string> TryFetchGitHubApiAsync(string url)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("User-Agent", "DeedPlanner-RegistryBrowser");
                request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
                HttpResponseMessage response = await HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
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
