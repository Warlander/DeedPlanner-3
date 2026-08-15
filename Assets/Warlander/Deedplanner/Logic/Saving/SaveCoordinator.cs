using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using VContainer;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic.Saving
{
    public class SaveCoordinator
    {
        private readonly MapHandler _mapHandler;
        private readonly DeedThumbnailCapture _thumbnailCapture;
        private readonly IReadOnlyList<ISaveBackend> _backends;
        private readonly RecentMapsStore _recentMaps;
        private readonly IObjectResolver _resolver;

        private AutoSaveScheduler _autoSaveScheduler;

        public MapLocation? CurrentLocation { get; private set; }
        public DateTime? LastSaveTimeUtc { get; private set; }
        public bool Busy { get; private set; }

        public IReadOnlyList<ISaveBackend> Backends => _backends;
        public RecentMapsStore RecentMaps => _recentMaps;

        /// Fired whenever CurrentLocation or LastSaveTimeUtc changes: save, quick save, load, new map.
        public event Action SaveStateChanged;

        public SaveCoordinator(MapHandler mapHandler, DeedThumbnailCapture thumbnailCapture,
            IReadOnlyList<ISaveBackend> backends, RecentMapsStore recentMaps, IObjectResolver resolver)
        {
            _mapHandler = mapHandler;
            _thumbnailCapture = thumbnailCapture;
            _backends = backends;
            _recentMaps = recentMaps;
            _resolver = resolver;
        }

        public ISaveBackend GetBackend(string id)
        {
            foreach (ISaveBackend backend in _backends)
            {
                if (backend.Id == id)
                {
                    return backend;
                }
            }

            return null;
        }

        public bool CanQuickSave =>
            !Busy && CurrentLocation.HasValue &&
            (GetBackend(CurrentLocation.Value.BackendId)?.Capabilities & SaveCapabilities.Overwrite) != 0;

        public string SerializeCurrentMap()
        {
            Map map = _mapHandler.Map;
            map.ThumbnailJpeg = _thumbnailCapture.CaptureJpeg(map);

            StringBuilder build = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;

            using (XmlWriter xmlWriter = XmlWriter.Create(build, settings))
            {
                XmlDocument document = new XmlDocument();
                map.Serialize(document, null);
                document.Save(xmlWriter);
            }

            return build.ToString();
        }

        /// Runs the backend's full save flow (picker included). Returns the saved location, null when cancelled.
        public async Task<MapLocation?> SaveAsync(string backendId)
        {
            ISaveBackend backend = GetBackend(backendId);
            Map map = _mapHandler.Map;
            if (backend == null || map == null || Busy)
            {
                return null;
            }

            Busy = true;
            try
            {
                string payload = SerializeCurrentMap();
                MapLocation? location = await backend.SaveAsync(payload, map.DisplayName);
                if (location.HasValue)
                {
                    map.DisplayName = location.Value.DisplayName;
                    map.ClearDirty();
                    CurrentLocation = location;
                    LastSaveTimeUtc = DateTime.UtcNow;
                    _recentMaps.Record(location.Value, map.ThumbnailJpeg);
                    SaveStateChanged?.Invoke();
                }

                return location;
            }
            finally
            {
                Busy = false;
            }
        }

        /// Overwrites the current location. Returns false when quick save is not possible.
        public async Task<bool> QuickSaveAsync()
        {
            if (!CanQuickSave)
            {
                return false;
            }

            Busy = true;
            try
            {
                string payload = SerializeCurrentMap();
                MapLocation location = CurrentLocation.Value;
                // display name may have changed since the original save
                location = new MapLocation(location.BackendId, location.Locator, _mapHandler.Map.DisplayName);
                await GetBackend(location.BackendId).OverwriteAsync(location, payload);
                _mapHandler.Map.ClearDirty();
                CurrentLocation = location;
                LastSaveTimeUtc = DateTime.UtcNow;
                _recentMaps.Record(location, _mapHandler.Map.ThumbnailJpeg);
                SaveStateChanged?.Invoke();
                return true;
            }
            finally
            {
                Busy = false;
            }
        }

        /// Picks a file with the backend and loads it. Returns false when cancelled or failed.
        public async Task<bool> PickAndLoadAsync(string backendId)
        {
            ISaveBackend backend = GetBackend(backendId);
            if (backend == null || Busy)
            {
                return false;
            }

            MapLocation? location = await backend.PickLoadLocationAsync();
            if (!location.HasValue)
            {
                return false;
            }

            return await LoadAsync(location.Value);
        }

        public async Task<bool> LoadAsync(MapLocation location)
        {
            ISaveBackend backend = GetBackend(location.BackendId);
            if (backend == null || Busy)
            {
                return false;
            }

            await AutoSaveBeforeDestructiveAsync();

            Busy = true;
            try
            {
                string payload = await backend.LoadAsync(location);
                _mapHandler.LoadMap(payload);
                if (_mapHandler.Map.DisplayName == "Untitled" && !string.IsNullOrEmpty(location.DisplayName))
                {
                    _mapHandler.Map.DisplayName = location.DisplayName;
                }

                if (_mapHandler.Map.ThumbnailJpeg == null)
                {
                    _mapHandler.Map.ThumbnailJpeg = _recentMaps.LoadThumbnail(location);
                }

                // location display name may come from the XML now
                var loadedLocation = new MapLocation(location.BackendId, location.Locator, _mapHandler.Map.DisplayName);
                // backends without Overwrite are export-style (Pastebin, browser downloads):
                // loading them creates an unsaved map, the identity is only kept for real saves
                CurrentLocation = (backend.Capabilities & SaveCapabilities.Overwrite) != 0 ? loadedLocation : (MapLocation?)null;
                LastSaveTimeUtc = null;
                _recentMaps.Record(loadedLocation, _mapHandler.Map.ThumbnailJpeg);
                SaveStateChanged?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load map from {location}: {e.Message}");
                return false;
            }
            finally
            {
                Busy = false;
            }
        }

        public async Task NewMapAsync(int width = 25, int height = 25)
        {
            await AutoSaveBeforeDestructiveAsync();
            CurrentLocation = null;
            LastSaveTimeUtc = null;
            _mapHandler.CreateNewMap(width, height);
            SaveStateChanged?.Invoke();
        }

        /// Deletes a save where the backend allows it, its auto-save slots, and its known-saves entry.
        /// When the deleted save is the current map's location, the map stays in-app as never-saved.
        public async Task DeleteSaveAsync(MapLocation location)
        {
            ISaveBackend backend = GetBackend(location.BackendId);
            if (backend != null && (backend.Capabilities & SaveCapabilities.Delete) != 0)
            {
                await backend.DeleteAsync(location);
                if (_autoSaveScheduler == null)
                {
                    _autoSaveScheduler = _resolver.Resolve<AutoSaveScheduler>();
                }

                await _autoSaveScheduler.DeleteSlotsAsync(location);
            }

            _recentMaps.Remove(location);

            if (CurrentLocation.HasValue &&
                CurrentLocation.Value.BackendId == location.BackendId &&
                CurrentLocation.Value.Locator == location.Locator)
            {
                CurrentLocation = null;
                LastSaveTimeUtc = null;
                SaveStateChanged?.Invoke();
            }
        }

        /// Loads a map from a web link (Pastebin/Drive/Dropbox). Pastebin links go through the backend
        /// (recorded, export semantics); other hosts load without a location identity.
        public async Task<bool> LoadFromWebAsync(string rawLink)
        {
            if (Busy)
            {
                return false;
            }

            string directLink = WebLinkUtils.ParseToDirectDownloadLink(rawLink);
            if (directLink.Contains("pastebin.com"))
            {
                int lastSlash = directLink.LastIndexOf('/');
                string name = lastSlash >= 0 && lastSlash < directLink.Length - 1
                    ? directLink.Substring(lastSlash + 1)
                    : "Shared map";
                return await LoadAsync(new MapLocation("pastebin", directLink, name));
            }

            try
            {
                await AutoSaveBeforeDestructiveAsync();
                await _mapHandler.LoadMapAsync(new Uri(directLink));
                CurrentLocation = null;
                LastSaveTimeUtc = null;
                SaveStateChanged?.Invoke();
                return _mapHandler.Map != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Unable to load map from {rawLink}: {e.Message}");
                return false;
            }
        }

        /// Loads an auto-save slot's content while keeping the main save's identity. Null main = never-saved map.
        public async Task<bool> LoadRecoveryAsync(MapLocation slot, MapLocation? mainLocation)
        {
            ISaveBackend backend = GetBackend(slot.BackendId);
            if (backend == null || Busy)
            {
                return false;
            }

            await AutoSaveBeforeDestructiveAsync();

            Busy = true;
            try
            {
                string payload = await backend.LoadAsync(slot);
                _mapHandler.LoadMap(payload);
                // recovered content differs from the main save, the user should re-save
                _mapHandler.Map.MarkDirty();
                CurrentLocation = mainLocation;
                LastSaveTimeUtc = null;
                SaveStateChanged?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to recover auto-save from {slot}: {e.Message}");
                return false;
            }
            finally
            {
                Busy = false;
            }
        }

        /// Writes an auto-save slot without touching CurrentLocation, dirty state, or the recent list.
        public async Task AutoSaveToAsync(MapLocation slot)
        {
            ISaveBackend backend = GetBackend(slot.BackendId);
            if (backend == null || Busy)
            {
                return;
            }

            Busy = true;
            try
            {
                string payload = SerializeCurrentMap();
                await backend.OverwriteAsync(slot, payload);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Auto-save failed: {e.Message}");
            }
            finally
            {
                Busy = false;
            }
        }

        /// Thumbnail bytes from any save location, without loading the map. Null when absent.
        public async Task<byte[]> ReadThumbnailAsync(MapLocation location)
        {
            ISaveBackend backend = GetBackend(location.BackendId);
            if (backend == null)
            {
                return null;
            }

            try
            {
                string payload = await backend.LoadAsync(location);
                var document = new XmlDocument();
                document.LoadXml(payload);
                XmlElement screenshot = document.DocumentElement?["screenshot"];
                if (screenshot == null || screenshot.GetAttribute("format") != "jpeg")
                {
                    return null;
                }

                return Convert.FromBase64String(screenshot.InnerText);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to read thumbnail from {location}: {e.Message}");
                return null;
            }
        }

        public async Task ResizeMapAsync(int left, int right, int bottom, int top)
        {
            await AutoSaveBeforeDestructiveAsync();
            _mapHandler.ResizeMap(left, right, bottom, top);
        }

        public async Task ClearMapAsync()
        {
            await AutoSaveBeforeDestructiveAsync();
            _mapHandler.ClearMap();
        }

        public async Task PrepareForQuitAsync()
        {
            await AutoSaveBeforeDestructiveAsync();
        }

        private async Task AutoSaveBeforeDestructiveAsync()
        {
            if (_autoSaveScheduler == null)
            {
                _autoSaveScheduler = _resolver.Resolve<AutoSaveScheduler>();
            }

            await _autoSaveScheduler.AutoSaveNowAsync();
        }
    }
}
