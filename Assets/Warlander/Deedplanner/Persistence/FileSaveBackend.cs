using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SimpleFileBrowser;
using Warlander.Deedplanner.Persistence.Compression;

namespace Warlander.Deedplanner.Persistence
{
    public class FileSaveBackend : ISaveBackend
    {
        public SaveBackendId Id => SaveBackendId.File;
        public string DisplayName => "File on this computer";
        public SaveCapabilities Capabilities =>
            SaveCapabilities.Save | SaveCapabilities.Load | SaveCapabilities.Track | SaveCapabilities.Overwrite | SaveCapabilities.Delete;
        public bool IsVolatile => false;
        public string VolatileWarning => null;
        public bool CompressesOutput => false;
        public bool IsAvailable => true;

        private readonly IByteCompressor _compressor;

        public FileSaveBackend(IByteCompressor compressor)
        {
            _compressor = compressor;
        }

        public SaveFeasibility CheckSave(long payloadBytes) => SaveFeasibility.Ok;

        public string LocationHint(MapLocation location) => Path.GetDirectoryName(location.Locator);

        public Task<MapLocation?> SaveAsync(string payload, string suggestedName)
        {
            TaskCompletionSource<MapLocation?> completion = new TaskCompletionSource<MapLocation?>();

            FileBrowser.SetFilters(false, new FileBrowser.Filter("DeedPlanner 3 save", "MAP"));
            FileBrowser.ShowSaveDialog(
                paths =>
                {
                    string path = paths[0];
                    if (string.IsNullOrEmpty(path))
                    {
                        completion.SetResult(null);
                        return;
                    }

                    if (!path.EndsWith(".MAP", StringComparison.OrdinalIgnoreCase))
                    {
                        path += ".MAP";
                    }

                    WriteAllTextSafe(path, payload);
                    completion.SetResult(new MapLocation(Id, path, Path.GetFileNameWithoutExtension(path)));
                },
                () => completion.SetResult(null),
                FileBrowser.PickMode.Files,
                initialFilename: suggestedName,
                title: "Save Map",
                saveButtonText: "Save");

            return completion.Task;
        }

        public Task OverwriteAsync(MapLocation target, string payload)
        {
            WriteAllTextSafe(target.Locator, payload);
            return Task.CompletedTask;
        }

        public Task<MapLocation?> PickLoadLocationAsync()
        {
            TaskCompletionSource<MapLocation?> completion = new TaskCompletionSource<MapLocation?>();

            FileBrowser.SetFilters(false, new FileBrowser.Filter("DeedPlanner 3 save", "MAP"));
            FileBrowser.ShowLoadDialog(
                paths =>
                {
                    if (paths == null || paths.Length != 1 || string.IsNullOrEmpty(paths[0]))
                    {
                        completion.SetResult(null);
                        return;
                    }

                    string path = paths[0];
                    completion.SetResult(new MapLocation(Id, path, Path.GetFileNameWithoutExtension(path)));
                },
                () => completion.SetResult(null),
                FileBrowser.PickMode.Files,
                title: "Load Map",
                loadButtonText: "Load");

            return completion.Task;
        }

        public Task<string> LoadAsync(MapLocation source)
        {
            byte[] data = File.ReadAllBytes(source.Locator);
            if (_compressor.IsCompressed(data))
            {
                data = _compressor.Decompress(data);
            }

            return Task.FromResult(Encoding.UTF8.GetString(data));
        }

        public Task<SaveLocationStatus> TrackAsync(MapLocation target)
        {
            if (!File.Exists(target.Locator))
            {
                return Task.FromResult(new SaveLocationStatus(false, default, 0));
            }

            FileInfo info = new FileInfo(target.Locator);
            return Task.FromResult(new SaveLocationStatus(true, info.LastWriteTimeUtc, info.Length));
        }

        public Task DeleteAsync(MapLocation target)
        {
            if (File.Exists(target.Locator))
            {
                File.Delete(target.Locator);
            }

            return Task.CompletedTask;
        }

        // file paths are unbounded — recents + the file dialog are the picker, nothing to enumerate
        public Task<IReadOnlyList<SavedMapInfo>> ListSavesAsync() =>
            Task.FromResult<IReadOnlyList<SavedMapInfo>>(Array.Empty<SavedMapInfo>());

        // temp file, then atomic replace: a crash mid-write can never corrupt the last good save
        private static void WriteAllTextSafe(string path, string payload)
        {
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, payload, new UTF8Encoding(false));

            if (!File.Exists(path))
            {
                File.Move(tempPath, path);
                return;
            }

            try
            {
                File.Replace(tempPath, path, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Delete(path);
                File.Move(tempPath, path);
            }
        }
    }
}
