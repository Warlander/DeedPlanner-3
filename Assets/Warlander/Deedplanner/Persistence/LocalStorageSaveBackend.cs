using Warlander.Deedplanner.Platform.Web;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Persistence.Compression;

namespace Warlander.Deedplanner.Persistence
{
    /// Browser localStorage. Volatile (browser cleanup can wipe it), gzip+base64 envelopes with write time.
    public class LocalStorageSaveBackend : ISaveBackend
    {
        private const long StorageLimitChars = 4L * 1024 * 1024 + 512 * 1024; // 4.5 MB of a ~5 MB origin budget

        public SaveBackendId Id => SaveBackendId.LocalStorage;
        public string DisplayName => "Browser storage";
        public SaveCapabilities Capabilities =>
            SaveCapabilities.Save | SaveCapabilities.Load | SaveCapabilities.Track | SaveCapabilities.Overwrite |
            SaveCapabilities.Delete | SaveCapabilities.List;
        public bool IsVolatile => true;
        public string VolatileWarning =>
            "Browser storage can be wiped. Clearing site data, private browsing, or browser cleanup tools will delete maps saved here. Export important maps as files.";
        public bool CompressesOutput => true;
        public bool IsAvailable => true;

        private readonly IByteCompressor _compressor;
        private readonly ISaveNameSanitizer _nameSanitizer;

        public LocalStorageSaveBackend(IByteCompressor compressor, ISaveNameSanitizer nameSanitizer)
        {
            _compressor = compressor;
            _nameSanitizer = nameSanitizer;
        }

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
            string key = _nameSanitizer.Sanitize(suggestedName) + ".MAP";
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
            return Task.FromResult(Encoding.UTF8.GetString(_compressor.Decompress(compressed)));
#else
            throw new NotSupportedException("LocalStorage backend is only available in WebGL builds");
#endif
        }

        public Task<SaveLocationStatus> TrackAsync(MapLocation target)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Utils.JavaScriptUtils.LocalStorageHasItem(target.Locator))
            {
                return Task.FromResult(new SaveLocationStatus(false, default, 0));
            }

            string envelopeJson = Utils.JavaScriptUtils.LocalStorageGetItem(target.Locator);
            Envelope envelope = JsonUtility.FromJson<Envelope>(envelopeJson);
            var writeTime = new DateTime(envelope.t, DateTimeKind.Utc);
            return Task.FromResult(new SaveLocationStatus(true, writeTime, envelope.d.Length));
#else
            return Task.FromResult(new SaveLocationStatus(false, default, 0));
#endif
        }

        public Task DeleteAsync(MapLocation target)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Utils.JavaScriptUtils.LocalStorageRemoveItem(target.Locator);
#endif
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SavedMapInfo>> ListSavesAsync()
        {
            var saves = new List<SavedMapInfo>();
#if UNITY_WEBGL && !UNITY_EDITOR
            foreach (string key in Utils.JavaScriptUtils.LocalStorageGetKeys())
            {
                if (!key.EndsWith(".MAP", StringComparison.OrdinalIgnoreCase) || key.Contains(".auto"))
                {
                    continue;
                }

                string envelopeJson = Utils.JavaScriptUtils.LocalStorageGetItem(key);
                Envelope envelope = JsonUtility.FromJson<Envelope>(envelopeJson);
                var writeTime = new DateTime(envelope.t, DateTimeKind.Utc);
                string displayName = key.Substring(0, key.Length - ".MAP".Length);
                saves.Add(new SavedMapInfo(new MapLocation(Id, key, displayName), writeTime));
            }
#endif
            return Task.FromResult<IReadOnlyList<SavedMapInfo>>(saves);
        }

        private void WriteEnvelope(string key, string payload)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            byte[] compressed = _compressor.Compress(Encoding.UTF8.GetBytes(payload));
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

        [Serializable]
        private class Envelope
        {
            public long t;
            public string d;
        }
    }
}
