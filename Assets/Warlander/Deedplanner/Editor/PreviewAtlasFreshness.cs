using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Editor
{
    public static class PreviewAtlasFreshness
    {
        private static readonly string[] Categories = { "floors", "walls", "objects", "grounds" };

        public static bool IsFresh(out string reason)
        {
            string inputsHash;
            try
            {
                inputsHash = CalculateInputsHash();
            }
            catch (Exception exception)
            {
                reason = "Unable to hash objects.xml: " + exception.Message;
                return false;
            }

            foreach (string category in Categories)
            {
                string basePath = Path.Combine(PreviewThumbnailGenerator.OutputFolder, category);
                string pngPath = basePath + ".png";
                string manifestPath = basePath + ".json";
                if (!File.Exists(pngPath) || !File.Exists(manifestPath))
                {
                    reason = "Missing generated preview atlas: " + category;
                    return false;
                }

                PreviewAtlasManifest manifest;
                try
                {
                    manifest = JsonUtility.FromJson<PreviewAtlasManifest>(File.ReadAllText(manifestPath));
                }
                catch (Exception exception)
                {
                    reason = "Invalid preview manifest for " + category + ": " + exception.Message;
                    return false;
                }

                if (manifest == null || manifest.category != category ||
                    manifest.generatorVersion != PreviewThumbnailGenerator.GeneratorVersion ||
                    !string.Equals(manifest.inputsHash, inputsHash, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "Stale preview manifest: " + category;
                    return false;
                }
            }

            reason = null;
            return true;
        }

        public static string CalculateInputsHash()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "objects.xml");
            using SHA256 sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }
    }
}
