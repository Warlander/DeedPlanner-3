using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Warlander.Deedplanner.Rendering.Assets;

namespace Warlander.Deedplanner.Editor
{
    public static class PreviewAtlasFreshness
    {
        private static readonly string[] Categories = { "floors", "walls", "objects", "grounds", "roofs" };

        public static bool IsFresh(out string reason)
        {
            string inputsHash;
            try
            {
                inputsHash = CalculateInputsHash();
            }
            catch (Exception exception)
            {
                reason = "Unable to hash preview inputs: " + exception.Message;
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
            var paths = new List<string>
            {
                "Assets/StreamingAssets/objects.xml",
                "Assets/Materials/ModelShader.shadergraph"
            };
            paths.AddRange(Directory.GetFiles(Application.streamingAssetsPath, "*_n.dds", SearchOption.AllDirectories));
            paths.Sort(StringComparer.Ordinal);
            using SHA256 sha256 = SHA256.Create();
            foreach (string path in paths)
            {
                byte[] bytes = File.ReadAllBytes(path);
                sha256.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }
            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(sha256.Hash).Replace("-", string.Empty);
        }
    }
}
