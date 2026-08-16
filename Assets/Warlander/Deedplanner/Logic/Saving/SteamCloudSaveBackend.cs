using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using Warlander.Deedplanner.Steam;

namespace Warlander.Deedplanner.Logic.Saving
{
    public class SteamCloudSaveBackend : ISaveBackend
    {
        public const int MaxMainSaves = 50;

        private readonly ISteamConnection _connection;

        public string Id => "steamcloud";
        public string DisplayName => "Steam Cloud";
        public SaveCapabilities Capabilities =>
            SaveCapabilities.Save | SaveCapabilities.Load | SaveCapabilities.Track | SaveCapabilities.Overwrite | SaveCapabilities.Delete;
        public bool IsVolatile => false;
        public bool CompressesOutput => true;
        public bool IsAvailable => _connection.Connected;

        public SteamCloudSaveBackend(ISteamConnection connection)
        {
            _connection = connection;
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

        public string LocationHint(MapLocation location) => null;

        public Task<MapLocation?> SaveAsync(string payload, string suggestedName)
        {
            string fileName = SanitizeFileName(suggestedName) + ".MAP";
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

            return Task.FromResult(Encoding.UTF8.GetString(DecompressGzip(compressed)));
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

        private static void WriteCloudFile(string fileName, string payload)
        {
            byte[] data = Compress(Encoding.UTF8.GetBytes(payload));
            if (!SteamRemoteStorage.FileWrite(fileName, data, data.Length))
            {
                throw new InvalidOperationException("Steam Cloud write failed: " + fileName);
            }
        }

        private static string SanitizeFileName(string name)
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
    }
}
