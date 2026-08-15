using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Logic.Saving
{
    /// Browser localStorage. Volatile (browser cleanup can wipe it), gzip+base64 envelopes with write time.
    public class LocalStorageSaveBackend : ISaveBackend
    {
        private const long StorageLimitChars = 4L * 1024 * 1024 + 512 * 1024; // 4.5 MB of a ~5 MB origin budget

        public string Id => "localstorage";
        public string DisplayName => "Browser storage";
        public SaveCapabilities Capabilities =>
            SaveCapabilities.Save | SaveCapabilities.Load | SaveCapabilities.Track | SaveCapabilities.Overwrite | SaveCapabilities.Delete;
        public bool IsVolatile => true;
        public bool CompressesOutput => true;
        public bool IsAvailable => true;

        public SaveFeasibility CheckSave(long payloadBytes)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            long projected = Utils.JavaScriptUtils.LocalStorageTotalSize() + payloadBytes * 2;
            if (projected > StorageLimitChars)
            {
                return new SaveFeasibility(false, StorageLimitChars,
                    "Not enough browser storage space left for this map. Export it as a file instead.");
            }
#endif
            return SaveFeasibility.Ok;
        }

        public string LocationHint(MapLocation location) => null;

        public Task<MapLocation?> SaveAsync(string payload, string suggestedName)
        {
            string key = SanitizeKeyName(suggestedName) + ".MAP";
            WriteEnvelope(key, payload);
            return Task.FromResult<MapLocation?>(new MapLocation(Id, key, suggestedName));
        }

        public Task OverwriteAsync(MapLocation target, string payload)
        {
            WriteEnvelope(target.Locator, payload);
            return Task.CompletedTask;
        }

        public Task<MapLocation?> PickLoadLocationAsync()
        {
            // the home screen card grid is the picker for browser saves
            throw new NotSupportedException("Browser saves are picked from the home screen");
        }

        public Task<string> LoadAsync(MapLocation source)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Utils.JavaScriptUtils.LocalStorageHasItem(source.Locator))
            {
                throw new InvalidOperationException("Browser save not found: " + source.Locator);
            }

            string envelopeJson = Utils.JavaScriptUtils.LocalStorageGetItem(source.Locator);
            Envelope envelope = JsonUtility.FromJson<Envelope>(envelopeJson);
            byte[] compressed = Convert.FromBase64String(envelope.d);
            return Task.FromResult(Encoding.UTF8.GetString(DecompressGzip(compressed)));
#else
            throw new NotSupportedException("LocalStorage backend is only available in WebGL builds");
#endif
        }

        public Task<TrackResult> TrackAsync(MapLocation target)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Utils.JavaScriptUtils.LocalStorageHasItem(target.Locator))
            {
                return Task.FromResult(new TrackResult(false, default, 0));
            }

            string envelopeJson = Utils.JavaScriptUtils.LocalStorageGetItem(target.Locator);
            Envelope envelope = JsonUtility.FromJson<Envelope>(envelopeJson);
            var writeTime = new DateTime(envelope.t, DateTimeKind.Utc);
            return Task.FromResult(new TrackResult(true, writeTime, envelope.d.Length));
#else
            return Task.FromResult(new TrackResult(false, default, 0));
#endif
        }

        public Task DeleteAsync(MapLocation target)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Utils.JavaScriptUtils.LocalStorageRemoveItem(target.Locator);
#endif
            return Task.CompletedTask;
        }

        private static void WriteEnvelope(string key, string payload)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            byte[] compressed = Compress(Encoding.UTF8.GetBytes(payload));
            var envelope = new Envelope
            {
                t = DateTime.UtcNow.Ticks,
                d = Convert.ToBase64String(compressed)
            };

            if (!Utils.JavaScriptUtils.LocalStorageSetItem(key, JsonUtility.ToJson(envelope)))
            {
                throw new InvalidOperationException(
                    "Browser storage quota exceeded. Export the map as a file to keep it safe.");
            }
#else
            throw new NotSupportedException("LocalStorage backend is only available in WebGL builds");
#endif
        }

        private static string SanitizeKeyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Untitled";
            }

            var builder = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                builder.Append(char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' ? c : '_');
            }

            string sanitized = builder.ToString().Trim();
            if (sanitized.Length > 64)
            {
                sanitized = sanitized.Substring(0, 64);
            }

            return sanitized.Length > 0 ? sanitized : "Untitled";
        }

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

        [Serializable]
        private class Envelope
        {
            public long t;
            public string d;
        }
    }
}
