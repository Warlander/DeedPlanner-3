using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic.Saving
{
    public class SaveCoordinator
    {
        private readonly MapHandler _mapHandler;
        private readonly DeedThumbnailCapture _thumbnailCapture;
        private readonly IReadOnlyList<ISaveBackend> _backends;
        private readonly RecentMapsStore _recentMaps;

        public MapLocation? CurrentLocation { get; private set; }
        public DateTime? LastSaveTimeUtc { get; private set; }
        public bool Busy { get; private set; }

        public IReadOnlyList<ISaveBackend> Backends => _backends;
        public RecentMapsStore RecentMaps => _recentMaps;

        /// Fired whenever CurrentLocation or LastSaveTimeUtc changes: save, quick save, load, new map.
        public event Action SaveStateChanged;

        public SaveCoordinator(MapHandler mapHandler, DeedThumbnailCapture thumbnailCapture,
            IReadOnlyList<ISaveBackend> backends, RecentMapsStore recentMaps)
        {
            _mapHandler = mapHandler;
            _thumbnailCapture = thumbnailCapture;
            _backends = backends;
            _recentMaps = recentMaps;
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
                CurrentLocation = loadedLocation;
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

        public void NewMap(int width = 25, int height = 25)
        {
            CurrentLocation = null;
            LastSaveTimeUtc = null;
            _mapHandler.CreateNewMap(width, height);
            SaveStateChanged?.Invoke();
        }
    }
}
