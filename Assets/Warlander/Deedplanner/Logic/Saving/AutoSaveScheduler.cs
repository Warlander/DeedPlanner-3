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

            MapLocation? slot = NextSlotLocation(map);
            if (!slot.HasValue)
            {
                return;
            }

            await _saveCoordinator.AutoSaveToAsync(slot.Value);
        }

        /// Newest auto-save slot for an entry when it is newer than the main save. Null otherwise.
        public MapLocation? FindRecoverySlot(MapLocation mainLocation)
        {
            if (mainLocation.BackendId != "file")
            {
                return null;
            }

            MapLocation? newest = NewestSlot(SlotPathsFor(mainLocation), out DateTime newestWrite);
            if (!newest.HasValue || !File.Exists(mainLocation.Locator))
            {
                return newest;
            }

            return newestWrite > File.GetLastWriteTimeUtc(mainLocation.Locator) ? newest : null;
        }

        /// Auto-save slot from a never-saved map, when one exists.
        public MapLocation? FindNeverSavedRecovery()
        {
            return NewestSlot(SlotPathsForNeverSaved(), out _);
        }

        private MapLocation? NextSlotLocation(Map map)
        {
            MapLocation? current = _saveCoordinator.CurrentLocation;
            if (current.HasValue)
            {
                if (current.Value.BackendId != "file")
                {
                    return null;
                }

                string[] slots = SlotPathsFor(current.Value);
                return new MapLocation("file", OldestOrEmptySlot(slots), current.Value.DisplayName);
            }

            return new MapLocation("file", OldestOrEmptySlot(SlotPathsForNeverSaved()), map.DisplayName);
        }

        private static string[] SlotPathsFor(MapLocation mainLocation)
        {
            string directory = Path.GetDirectoryName(mainLocation.Locator);
            string baseName = Path.GetFileNameWithoutExtension(mainLocation.Locator);
            var paths = new string[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                paths[i] = Path.Combine(directory, $"{baseName}.auto{i + 1}.MAP");
            }

            return paths;
        }

        private static string[] SlotPathsForNeverSaved()
        {
            string directory = Path.Combine(Application.persistentDataPath, "Autosaves");
            Directory.CreateDirectory(directory);
            var paths = new string[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                paths[i] = Path.Combine(directory, $"Untitled.auto{i + 1}.MAP");
            }

            return paths;
        }

        private static string OldestOrEmptySlot(string[] slots)
        {
            string oldest = null;
            DateTime oldestWrite = DateTime.MaxValue;
            foreach (string slot in slots)
            {
                if (!File.Exists(slot))
                {
                    return slot;
                }

                DateTime write = File.GetLastWriteTimeUtc(slot);
                if (write < oldestWrite)
                {
                    oldestWrite = write;
                    oldest = slot;
                }
            }

            return oldest;
        }

        private static MapLocation? NewestSlot(string[] slots, out DateTime newestWrite)
        {
            string newest = null;
            newestWrite = DateTime.MinValue;
            foreach (string slot in slots)
            {
                if (!File.Exists(slot))
                {
                    continue;
                }

                DateTime write = File.GetLastWriteTimeUtc(slot);
                if (write > newestWrite)
                {
                    newestWrite = write;
                    newest = slot;
                }
            }

            return newest != null
                ? new MapLocation("file", newest, Path.GetFileNameWithoutExtension(newest))
                : null;
        }
    }
}
