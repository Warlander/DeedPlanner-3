using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Logic.Saving;

namespace Warlander.Deedplanner.Gui.Home
{
    /// Owns the home screen's content: categories, card grid population, statuses, recovery cards, thumbnails.
    public class HomeScreenCardCatalog
    {
        private readonly IHomeScreenView _view;
        private readonly ISaveCoordinator _saveCoordinator;
        private readonly IAutoSaveScheduler _autoSaveScheduler;

        private SaveBackendId? _selectedBackendId;
        private readonly Dictionary<MapLocation, MapLocation?> _recoveryMains =
            new Dictionary<MapLocation, MapLocation?>();

        public IReadOnlyDictionary<MapLocation, MapLocation?> RecoveryMains => _recoveryMains;

        public HomeScreenCardCatalog(IHomeScreenView view, ISaveCoordinator saveCoordinator,
            IAutoSaveScheduler autoSaveScheduler)
        {
            _view = view;
            _saveCoordinator = saveCoordinator;
            _autoSaveScheduler = autoSaveScheduler;
        }

        public void ResetCategory()
        {
            _selectedBackendId = null;
        }

        public void SelectCategory(SaveBackendId? backendId)
        {
            _selectedBackendId = backendId;
            Populate();
        }

        public void Populate()
        {
            _ = PopulateAsync();
        }

        private async Task PopulateAsync()
        {
            var categories = new List<HomeScreenCategory>();
            foreach (ISaveBackend backend in _saveCoordinator.Backends)
            {
                if (!backend.IsAvailable)
                {
                    continue;
                }

                string label = CategoryLabel(backend.Id);
                if (label != null)
                {
                    categories.Add(new HomeScreenCategory(backend.Id, label));
                }
            }

            _view.SetCategories(categories, _selectedBackendId);

            var cards = new List<HomeScreenCardData>();
            _recoveryMains.Clear();

            foreach (RecentMapEntry entry in _saveCoordinator.RecentMaps.Entries)
            {
                if (!VisibleInCategory(entry))
                {
                    continue;
                }

                MapLocation? slot = await _autoSaveScheduler.FindRecoverySlotAsync(entry.Location);
                if (slot.HasValue)
                {
                    cards.Add(await BuildRecoveryCardAsync(slot.Value, entry.Location.Locator));
                    _recoveryMains[slot.Value] = entry.Location;
                }
            }

            if (_selectedBackendId == null || _selectedBackendId == SaveBackendId.File)
            {
                MapLocation? untitledSlot = await _autoSaveScheduler.FindNeverSavedRecoveryAsync();
                if (untitledSlot.HasValue && !_recoveryMains.ContainsKey(untitledSlot.Value))
                {
                    cards.Add(await BuildRecoveryCardAsync(untitledSlot.Value, "never-saved map"));
                    _recoveryMains[untitledSlot.Value] = null;
                }
            }

            foreach (RecentMapEntry entry in _saveCoordinator.RecentMaps.Entries)
            {
                if (VisibleInCategory(entry))
                {
                    cards.Add(BuildCard(entry));
                }
            }

            _view.SetCards(cards);
            _ = RefreshCardStatusesAsync();
        }

        private bool VisibleInCategory(RecentMapEntry entry)
        {
            return _selectedBackendId == null || entry.Location.BackendId == _selectedBackendId;
        }

        private async Task<HomeScreenCardData> BuildRecoveryCardAsync(MapLocation slot, string originHint)
        {
            byte[] jpeg = await _saveCoordinator.ReadThumbnailAsync(slot);
            Texture2D thumbnail = jpeg != null ? ToTexture(jpeg) : null;
            DateTime slotWrite = File.GetLastWriteTimeUtc(slot.Locator);

            return new HomeScreenCardData(
                slot,
                "Recovered auto-save",
                FormatTime(slotWrite),
                originHint,
                "FILE",
                thumbnail,
                HomeScreenChip.Recovery,
                showDelete: false);
        }

        private HomeScreenCardData BuildCard(RecentMapEntry entry)
        {
            ISaveBackend backend = _saveCoordinator.GetBackend(entry.Location.BackendId);
            bool trackable = backend != null && (backend.Capabilities & SaveCapabilities.Track) != 0;
            bool volatileBackend = backend != null && backend.IsVolatile;

            HomeScreenChip chip = HomeScreenChip.None;
            if (volatileBackend)
            {
                chip = HomeScreenChip.Volatile;
            }
            else if (!trackable)
            {
                chip = HomeScreenChip.Unknown;
            }

            return new HomeScreenCardData(
                entry.Location,
                entry.Location.DisplayName,
                FormatTime(entry.LastOpenedUtc),
                backend?.LocationHint(entry.Location),
                BadgeLabel(entry.Location.BackendId),
                LoadThumbnailTexture(entry),
                chip);
        }

        private async Task RefreshCardStatusesAsync()
        {
            foreach (RecentMapEntry entry in _saveCoordinator.RecentMaps.Entries)
            {
                if (!VisibleInCategory(entry))
                {
                    continue;
                }

                ISaveBackend backend = _saveCoordinator.GetBackend(entry.Location.BackendId);
                if (backend == null || (backend.Capabilities & SaveCapabilities.Track) == 0)
                {
                    continue;
                }

                try
                {
                    SaveLocationStatus status = await backend.TrackAsync(entry.Location);
                    if (!status.Exists)
                    {
                        _view.UpdateCard(entry.Location, WithChip(entry, HomeScreenChip.Missing));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to track {entry.Location}: {e.Message}");
                }
            }
        }

        private HomeScreenCardData WithChip(RecentMapEntry entry, HomeScreenChip chip)
        {
            return new HomeScreenCardData(
                entry.Location,
                entry.Location.DisplayName,
                FormatTime(entry.LastOpenedUtc),
                _saveCoordinator.GetBackend(entry.Location.BackendId)?.LocationHint(entry.Location),
                BadgeLabel(entry.Location.BackendId),
                LoadThumbnailTexture(entry),
                chip);
        }

        private Texture2D LoadThumbnailTexture(RecentMapEntry entry)
        {
            if (!entry.HasThumbnail)
            {
                return null;
            }

            byte[] jpeg = _saveCoordinator.RecentMaps.LoadThumbnail(entry.Location);
            return jpeg != null ? ToTexture(jpeg) : null;
        }

        private static Texture2D ToTexture(byte[] jpeg)
        {
            Texture2D texture = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(texture, jpeg))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            return texture;
        }

        private static string FormatTime(DateTime utc)
        {
            DateTime local = utc.ToLocalTime();
            DateTime now = DateTime.Now;
            if (local.Date == now.Date)
            {
                return "Today " + local.ToString("HH:mm");
            }

            if (local.Date == now.Date.AddDays(-1))
            {
                return "Yesterday";
            }

            int days = (now.Date - local.Date).Days;
            return days < 30 ? days + " days ago" : local.ToString("yyyy-MM-dd");
        }

        private static string CategoryLabel(SaveBackendId backendId)
        {
            switch (backendId)
            {
                case SaveBackendId.File: return "Local files";
                case SaveBackendId.SteamCloud: return "Steam Cloud";
                case SaveBackendId.LocalStorage: return "Browser storage";
                case SaveBackendId.Pastebin: return "Pastebin";
                default: return null;
            }
        }

        private static string BadgeLabel(SaveBackendId backendId)
        {
            switch (backendId)
            {
                case SaveBackendId.File: return "FILE";
                case SaveBackendId.SteamCloud: return "STEAM";
                case SaveBackendId.LocalStorage: return "BROWSER";
                case SaveBackendId.Pastebin: return "PASTE";
                case SaveBackendId.WebFile: return "FILE";
                default: return backendId.ToString().ToUpperInvariant();
            }
        }
    }
}
