using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Data;
using VContainer.Unity;

namespace Warlander.Deedplanner.Logic.Saving
{
    public class AutoSaveScheduler : IInitializable, ITickable, IDisposable
    {
        public const float IntervalSeconds = 300f;
        public const int SlotCount = 2;

        private readonly SaveCoordinator _saveCoordinator;
        private readonly MapHandler _mapHandler;

        private float _elapsed;

        public AutoSaveScheduler(SaveCoordinator saveCoordinator, MapHandler mapHandler)
        {
            _saveCoordinator = saveCoordinator;
            _mapHandler = mapHandler;
        }

        public void Initialize() { }

        public void Dispose() { }

        public void Tick()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= IntervalSeconds)
            {
                _elapsed = 0;
                _ = AutoSaveNowAsync();
            }
        }

        public async Task AutoSaveNowAsync()
        {
            Map map = _mapHandler.Map;
            if (map == null || !map.IsDirty || _saveCoordinator.Busy)
            {
                return;
            }

            MapLocation? slot = await NextSlotLocationAsync(map);
            if (!slot.HasValue)
            {
                return;
            }

            await _saveCoordinator.AutoSaveToAsync(slot.Value);
        }

        /// Newest auto-save slot for an entry when it is newer than the main save. Null otherwise.
        public async Task<MapLocation?> FindRecoverySlotAsync(MapLocation mainLocation)
        {
            ISaveBackend backend = _saveCoordinator.GetBackend(mainLocation.BackendId);
            if (backend == null || (backend.Capabilities & SaveCapabilities.Track) == 0)
            {
                return null;
            }

            MapLocation? newest = await NewestSlotAsync(mainLocation.BackendId, SlotLocatorsFor(mainLocation));
            if (!newest.HasValue)
            {
                return null;
            }

            TrackResult mainTrack = await backend.TrackAsync(mainLocation);
            if (!mainTrack.Exists)
            {
                return newest;
            }

            TrackResult slotTrack = await backend.TrackAsync(newest.Value);
            return slotTrack.WriteTimeUtc > mainTrack.WriteTimeUtc ? newest : null;
        }

        /// Auto-save slot from a never-saved map, when one exists.
        public async Task<MapLocation?> FindNeverSavedRecoveryAsync()
        {
            string backendId = NeverSavedBackendId();
            if (backendId == null)
            {
                return null;
            }

            return await NewestSlotAsync(backendId, SlotLocatorsForNeverSaved(backendId));
        }

        /// Deletes all auto-save slots of a save. Used when the save itself is deleted.
        public async Task DeleteSlotsAsync(MapLocation mainLocation)
        {
            ISaveBackend backend = _saveCoordinator.GetBackend(mainLocation.BackendId);
            if (backend == null || (backend.Capabilities & SaveCapabilities.Delete) == 0)
            {
                return;
            }

            foreach (string slot in SlotLocatorsFor(mainLocation))
            {
                await backend.DeleteAsync(new MapLocation(mainLocation.BackendId, slot, null));
            }
        }

        private async Task<MapLocation?> NextSlotLocationAsync(Map map)
        {
            MapLocation? current = _saveCoordinator.CurrentLocation;
            if (current.HasValue)
            {
                ISaveBackend backend = _saveCoordinator.GetBackend(current.Value.BackendId);
                if (backend == null || (backend.Capabilities & SaveCapabilities.Overwrite) == 0)
                {
                    return null;
                }

                string locator = await OldestOrEmptySlotAsync(current.Value.BackendId, SlotLocatorsFor(current.Value));
                return new MapLocation(current.Value.BackendId, locator, current.Value.DisplayName);
            }

            string neverSavedBackendId = NeverSavedBackendId();
            if (neverSavedBackendId == null)
            {
                return null;
            }

            string neverSavedLocator = await OldestOrEmptySlotAsync(neverSavedBackendId, SlotLocatorsForNeverSaved(neverSavedBackendId));
            return new MapLocation(neverSavedBackendId, neverSavedLocator, map.DisplayName);
        }

        private string NeverSavedBackendId()
        {
            if (_saveCoordinator.GetBackend("file") != null)
            {
                return "file";
            }

            return _saveCoordinator.GetBackend("localstorage") != null ? "localstorage" : null;
        }

        private static string[] SlotLocatorsFor(MapLocation mainLocation)
        {
            var locators = new string[SlotCount];
            if (mainLocation.BackendId == "file")
            {
                string directory = Path.GetDirectoryName(mainLocation.Locator);
                string baseName = Path.GetFileNameWithoutExtension(mainLocation.Locator);
                for (int i = 0; i < SlotCount; i++)
                {
                    locators[i] = Path.Combine(directory, $"{baseName}.auto{i + 1}.MAP");
                }
            }
            else
            {
                string baseName = mainLocation.Locator;
                if (baseName.EndsWith(".MAP", StringComparison.OrdinalIgnoreCase))
                {
                    baseName = baseName.Substring(0, baseName.Length - 4);
                }

                for (int i = 0; i < SlotCount; i++)
                {
                    locators[i] = $"{baseName}.auto{i + 1}.MAP";
                }
            }

            return locators;
        }

        private static string[] SlotLocatorsForNeverSaved(string backendId)
        {
            var locators = new string[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                string slotName = $"Untitled.auto{i + 1}.MAP";
                if (backendId == "file")
                {
                    string directory = Path.Combine(Application.persistentDataPath, "Autosaves");
                    Directory.CreateDirectory(directory);
                    locators[i] = Path.Combine(directory, slotName);
                }
                else
                {
                    locators[i] = slotName;
                }
            }

            return locators;
        }

        private async Task<string> OldestOrEmptySlotAsync(string backendId, string[] slots)
        {
            string oldest = null;
            DateTime oldestWrite = DateTime.MaxValue;
            foreach (string slot in slots)
            {
                TrackResult track = await _saveCoordinator.GetBackend(backendId).TrackAsync(
                    new MapLocation(backendId, slot, null));
                if (!track.Exists)
                {
                    return slot;
                }

                if (track.WriteTimeUtc < oldestWrite)
                {
                    oldestWrite = track.WriteTimeUtc;
                    oldest = slot;
                }
            }

            return oldest;
        }

        private async Task<MapLocation?> NewestSlotAsync(string backendId, string[] slots)
        {
            string newest = null;
            DateTime newestWrite = DateTime.MinValue;
            foreach (string slot in slots)
            {
                TrackResult track = await _saveCoordinator.GetBackend(backendId).TrackAsync(
                    new MapLocation(backendId, slot, null));
                if (track.Exists && track.WriteTimeUtc > newestWrite)
                {
                    newestWrite = track.WriteTimeUtc;
                    newest = slot;
                }
            }

            if (newest == null)
            {
                return null;
            }

            string name = Path.GetFileNameWithoutExtension(newest);
            return new MapLocation(backendId, newest, name);
        }
    }
}
