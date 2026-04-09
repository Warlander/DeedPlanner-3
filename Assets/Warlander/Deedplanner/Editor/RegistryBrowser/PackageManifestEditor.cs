using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public static class PackageManifestEditor
    {
        private static string ManifestPath
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));

        public static void SetRegistryVersion(string packageId, string version)
        {
            string content = File.ReadAllText(ManifestPath);
            string escaped = Regex.Escape(packageId);
            var regex = new Regex($@"(""{escaped}""\s*:\s*)""[^""]*""");
            string updated = regex.Replace(content, $"$1\"{version}\"");
            File.WriteAllText(ManifestPath, updated);
        }

        public static void SetEmbeddedPath(string packageId)
            => SetRegistryVersion(packageId, $"file:Embeds/{packageId}");

        public static void AddOrUpdateDependency(string packageId, string versionOrPath)
        {
            string content = File.ReadAllText(ManifestPath);
            string escaped = Regex.Escape(packageId);

            if (Regex.IsMatch(content, $@"""{escaped}"""))
            {
                SetRegistryVersion(packageId, versionOrPath);
                return;
            }

            // Insert as the last entry in the dependencies block.
            // Match the last key-value pair before the closing } of dependencies.
            var insertRegex = new Regex(
                @"(""[^""]+"":\s*""[^""]*"")(\s*\n(\s*)\})",
                RegexOptions.RightToLeft);
            string updated = insertRegex.Replace(
                content,
                $"$1,\n$3\"{packageId}\": \"{versionOrPath}\"$2",
                count: 1);
            File.WriteAllText(ManifestPath, updated);
        }
    }
}
