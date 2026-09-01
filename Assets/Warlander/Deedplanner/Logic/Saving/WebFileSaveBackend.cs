using Warlander.Deedplanner.Platform.Web;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Warlander.Deedplanner.Logic.Saving
{
    /// WebGL file downloads through the browser. Save-only; the browser owns the files.
    public class WebFileSaveBackend : ISaveBackend
    {
        public SaveBackendId Id => SaveBackendId.WebFile;
        public string DisplayName => "File download";
        public SaveCapabilities Capabilities => SaveCapabilities.Save | SaveCapabilities.Load;
        public bool IsVolatile => false;
        public string VolatileWarning => null;
        public bool CompressesOutput => false;
        public bool IsAvailable => true;

        public SaveFeasibility CheckSave(long payloadBytes) => SaveFeasibility.Ok;

        public string LocationHint(MapLocation location) => null;

        public Task<MapLocation?> SaveAsync(string payload, string suggestedName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string fileName = suggestedName + ".MAP";
            Utils.JavaScriptUtils.DownloadNative(fileName, payload);
            return Task.FromResult<MapLocation?>(new MapLocation(Id, fileName, suggestedName));
#else
            throw new NotSupportedException("WebFile backend is only available in WebGL builds");
#endif
        }

        public Task OverwriteAsync(MapLocation target, string payload)
        {
            throw new NotSupportedException("WebFile does not support overwrite");
        }

        public Task<MapLocation?> PickLoadLocationAsync()
        {
            throw new NotSupportedException("WebFile locations cannot be picked");
        }

        public Task<string> LoadAsync(MapLocation source)
        {
            throw new NotSupportedException("WebFile loads happen through the browser file picker");
        }

        public Task<SaveLocationStatus> TrackAsync(MapLocation target)
        {
            throw new NotSupportedException("WebFile does not support tracking");
        }

        public Task DeleteAsync(MapLocation target)
        {
            throw new NotSupportedException("WebFile downloads cannot be deleted");
        }

        public Task<IReadOnlyList<SavedMapInfo>> ListSavesAsync() =>
            Task.FromResult<IReadOnlyList<SavedMapInfo>>(Array.Empty<SavedMapInfo>());
    }
}
