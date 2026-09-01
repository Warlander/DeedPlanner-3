#if !DISABLESTEAMWORKS
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using Warlander.Deedplanner.Persistence.Compression;
using Warlander.Deedplanner.Platform.Steam;

namespace Warlander.Deedplanner.Persistence
{
    public class SteamCloudSaveBackend : ISaveBackend
    {
        public const int MaxMainSaves = 50;

        private readonly ISteamConnection _connection;
        private readonly IByteCompressor _compressor;
        private readonly ISaveNameSanitizer _nameSanitizer;

        public SaveBackendId Id => SaveBackendId.SteamCloud;
        public string DisplayName => "Steam Cloud";
        public SaveCapabilities Capabilities =>
            SaveCapabilities.Save | SaveCapabilities.Load | SaveCapabilities.Track | SaveCapabilities.Overwrite |
            SaveCapabilities.Delete | SaveCapabilities.List;
        public bool IsVolatile => false;
        public string VolatileWarning => null;
        public bool CompressesOutput => true;
        public bool IsAvailable => _connection.Connected;

        public SteamCloudSaveBackend(ISteamConnection connection, IByteCompressor compressor, ISaveNameSanitizer nameSanitizer)
        {
            _connection = connection;
            _compressor = compressor;
            _nameSanitizer = nameSanitizer;
        }

        public SaveFeasibility CheckSave(long payloadBytes)
        {
            int mainSaves = 0;
            int fileCount = SteamRemoteStorage.GetFileCount();
            for (int i = 0; i < fileCount; i++)
            {
                string name = SteamRemoteStorage.GetFileNameAndSize(i, out _);
                if (name.EndsWith(".MAP", StringComparison.OrdinalIgnoreCase) && !name.Contains(".auto"))
                {
                    mainSaves++;
                }
            }

            if (mainSaves >= MaxMainSaves)
            {
                return new SaveFeasibility(false, MaxMainSaves,
                    $"Steam Cloud save limit reached ({MaxMainSaves} saves). Delete old saves to make room.");
            }

            return SaveFeasibility.Ok;
        }

        public string LocationHint(MapLocation location) => _connection.GetName();

        public Task<MapLocation?> SaveAsync(string payload, string suggestedName)
        {
            string fileName = _nameSanitizer.Sanitize(suggestedName) + ".MAP";
            WriteCloudFile(fileName, payload);
            return Task.FromResult<MapLocation?>(new MapLocation(Id, fileName, suggestedName));
        }

        public Task OverwriteAsync(MapLocation target, string payload)
        {
            WriteCloudFile(target.Locator, payload);
            return Task.CompletedTask;
        }

        public Task<MapLocation?> PickLoadLocationAsync()
        {
            // the home screen card grid is the picker for cloud saves
            throw new NotSupportedException("Steam Cloud saves are picked from the home screen");
        }

        public Task<string> LoadAsync(MapLocation source)
        {
            int size = SteamRemoteStorage.GetFileSize(source.Locator);
            if (size <= 0)
            {
                throw new InvalidOperationException("Steam Cloud file not found: " + source.Locator);
            }

            byte[] compressed = new byte[size];
            int read = SteamRemoteStorage.FileRead(source.Locator, compressed, size);
            if (read != size)
            {
                throw new InvalidOperationException("Failed to read Steam Cloud file: " + source.Locator);
            }

            return Task.FromResult(Encoding.UTF8.GetString(_compressor.Decompress(compressed)));
        }

        public Task<SaveLocationStatus> TrackAsync(MapLocation target)
        {
            if (!SteamRemoteStorage.FileExists(target.Locator))
            {
                return Task.FromResult(new SaveLocationStatus(false, default, 0));
            }

            long unixTime = SteamRemoteStorage.GetFileTimestamp(target.Locator);
            var writeTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
            return Task.FromResult(new SaveLocationStatus(true, writeTime, SteamRemoteStorage.GetFileSize(target.Locator)));
        }

        public Task DeleteAsync(MapLocation target)
        {
            if (SteamRemoteStorage.FileExists(target.Locator))
            {
                SteamRemoteStorage.FileDelete(target.Locator);
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SavedMapInfo>> ListSavesAsync()
        {
            var saves = new List<SavedMapInfo>();
            int fileCount = SteamRemoteStorage.GetFileCount();
            for (int i = 0; i < fileCount; i++)
            {
                string name = SteamRemoteStorage.GetFileNameAndSize(i, out _);
                if (!name.EndsWith(".MAP", StringComparison.OrdinalIgnoreCase) || name.Contains(".auto"))
                {
                    continue;
                }

                long unixTime = SteamRemoteStorage.GetFileTimestamp(name);
                DateTime writeTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
                string displayName = name.Substring(0, name.Length - ".MAP".Length);
                saves.Add(new SavedMapInfo(new MapLocation(Id, name, displayName), writeTime));
            }

            return Task.FromResult<IReadOnlyList<SavedMapInfo>>(saves);
        }

        private void WriteCloudFile(string fileName, string payload)
        {
            byte[] data = _compressor.Compress(Encoding.UTF8.GetBytes(payload));
            if (!SteamRemoteStorage.FileWrite(fileName, data, data.Length))
            {
                throw new InvalidOperationException("Steam Cloud write failed: " + fileName);
            }
        }
    }
}
#endif
