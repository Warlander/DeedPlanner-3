using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task<IReadOnlyList<PackageSummary>> FetchPackagesAsync(IReadOnlyList<RegistryScope> registries)
        {
            SearchRequest request = Client.SearchAll(false);
            UpmPackageInfo[] results = await WaitForRequestAsync(request);

            var summaries = new List<PackageSummary>();
            foreach (UpmPackageInfo pkg in results)
            {
                RegistryScope match = registries.FirstOrDefault(r => pkg.name.StartsWith(r.Scope));
                if (match.Scope != null)
                    summaries.Add(new PackageSummary(pkg.name, pkg.description, pkg.version, match.RegistryUrl));
            }

            return summaries;
        }

        public async Task<PackageDetails> FetchPackageDetailsAsync(string id, string registryUrl)
        {
            SearchRequest request = Client.Search(id, false);
            UpmPackageInfo[] results = await WaitForRequestAsync(request);

            if (results == null || results.Length == 0)
                return new PackageDetails(id, "", "", Array.Empty<string>(), "", registryUrl);

            UpmPackageInfo pkg = results[0];

            string[] allVersions = pkg.versions?.all ?? Array.Empty<string>();
            string[] versionsCopy = (string[])allVersions.Clone();
            Array.Reverse(versionsCopy);

            string repositoryUrl = pkg.repository?.url ?? "";

            return new PackageDetails(pkg.name, pkg.description, pkg.version, versionsCopy, repositoryUrl, registryUrl);
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
    }
}
