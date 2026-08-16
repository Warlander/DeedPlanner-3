using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Logic.Saving
{
    public class PastebinSaveBackend : ISaveBackend
    {
        private const string ApiEndpoint = "https://pastebin.com/api/api_post.php";
        private const string ApiDevKey = "24844c99ae9971a2da79a2f7d0da7642";
        private const long PasteLimitBytes = 512 * 1024;

        public SaveBackendId Id => SaveBackendId.Pastebin;
        public string DisplayName => "Pastebin";
        public SaveCapabilities Capabilities => SaveCapabilities.Save | SaveCapabilities.Load;
        public bool IsVolatile => true;
        public string VolatileWarning =>
            "Pastebin is not permanent storage. Pastes can be removed by Pastebin at any time. Keep a local file copy of any map you care about.";
        public bool CompressesOutput => true;
        public bool IsAvailable => true;

        public SaveFeasibility CheckSave(long payloadBytes)
        {
            if (payloadBytes > PasteLimitBytes)
            {
                return new SaveFeasibility(false, PasteLimitBytes,
                    $"Map exceeds Pastebin's {PasteLimitBytes / 1024} KB paste limit");
            }

            return SaveFeasibility.Ok;
        }

        public string LocationHint(MapLocation location)
        {
            int lastSlash = location.Locator.LastIndexOf('/');
            return lastSlash >= 0 && lastSlash < location.Locator.Length - 1
                ? location.Locator.Substring(lastSlash + 1)
                : location.Locator;
        }

        public async Task<MapLocation?> SaveAsync(string payload, string suggestedName)
        {
            byte[] compressed = Compress(Encoding.UTF8.GetBytes(payload));
            string base64 = Convert.ToBase64String(compressed);

            WWWForm form = new WWWForm();
            form.AddField("api_dev_key", ApiDevKey);
            form.AddField("api_paste_private", "1");
            form.AddField("api_paste_name", suggestedName);
            form.AddField("api_option", "paste");
            form.AddField("api_paste_expire_date", "N");
            form.AddField("api_paste_code", base64);

            using (UnityWebRequest request = UnityWebRequest.Post(ApiEndpoint, form))
            {
                TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
                request.SendWebRequest().completed += _ => completion.SetResult(true);
                await completion.Task;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new InvalidOperationException("Pastebin upload failed: " + request.error);
                }

                string response = request.downloadHandler.text;
                if (response.Contains("Bad API request"))
                {
                    throw new InvalidOperationException("Pastebin upload rejected: " + response);
                }

                return new MapLocation(Id, response, suggestedName);
            }
        }

        public Task OverwriteAsync(MapLocation target, string payload)
        {
            throw new NotSupportedException("Pastebin does not support overwrite");
        }

        public Task<MapLocation?> PickLoadLocationAsync()
        {
            throw new NotSupportedException("Pastebin locations cannot be picked from a list");
        }

        public async Task<string> LoadAsync(MapLocation source)
        {
            byte[] data = await WebUtils.ReadUrlToByteArrayAsync(source.Locator);
            string text = Encoding.UTF8.GetString(data);

            try
            {
                byte[] compressed = Convert.FromBase64String(text);
                return Encoding.UTF8.GetString(DecompressGzip(compressed));
            }
            catch
            {
                return text;
            }
        }

        public Task<SaveLocationStatus> TrackAsync(MapLocation target)
        {
            throw new NotSupportedException("Pastebin does not support tracking");
        }

        public Task DeleteAsync(MapLocation target)
        {
            throw new NotSupportedException("Anonymous pastes cannot be deleted via the Pastebin API");
        }

        public Task<IReadOnlyList<SavedMapInfo>> ListSavesAsync() =>
            Task.FromResult<IReadOnlyList<SavedMapInfo>>(Array.Empty<SavedMapInfo>());

        private static byte[] Compress(byte[] raw)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream stream = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    stream.Write(raw, 0, raw.Length);
                }

                return memory.ToArray();
            }
        }

        private static byte[] DecompressGzip(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }
    }
}
