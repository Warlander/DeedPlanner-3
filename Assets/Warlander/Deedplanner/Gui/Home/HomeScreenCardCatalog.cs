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

            List<CardItem> items = await CollectSaveItemsAsync();
            await ResolveWriteTimesAsync(items);
            items.Sort((a, b) => b.SortTimeUtc.CompareTo(a.SortTimeUtc));

            foreach (CardItem item in items)
            {
                cards.Add(item.Entry != null ? BuildCard(item) : BuildDiscoveredCard(item));
            }

            _view.SetCards(cards);
            _ = LoadMissingThumbnailsAsync(items);
        }

        private async Task<List<CardItem>> CollectSaveItemsAsync()
        {
            var items = new List<CardItem>();
            var knownLocators = new HashSet<string>();

            foreach (RecentMapEntry entry in _saveCoordinator.RecentMaps.Entries)
            {
                if (!VisibleInCategory(entry))
                {
                    continue;
                }

                knownLocators.Add(LocationKey(entry.Location));
                items.Add(new CardItem
                {
                    Entry = entry,
                    Location = entry.Location,
                    SortTimeUtc = entry.LastOpenedUtc
                });
            }

            foreach (ISaveBackend backend in _saveCoordinator.Backends)
            {
                if (!backend.IsAvailable || (backend.Capabilities & SaveCapabilities.List) == 0)
                {
                    continue;
                }

                if (_selectedBackendId != null && backend.Id != _selectedBackendId)
                {
                    continue;
                }

                foreach (SavedMapInfo info in await backend.ListSavesAsync())
                {
                    if (!knownLocators.Add(LocationKey(info.Location)))
                    {
                        continue;
                    }

                    items.Add(new CardItem
                    {
                        Location = info.Location,
                        SortTimeUtc = info.WriteTimeUtc
                    });
                }
            }

            return items;
        }

        private async Task ResolveWriteTimesAsync(List<CardItem> items)
        {
            var tasks = new List<Task>();
            foreach (CardItem item in items)
            {
                if (item.Entry == null)
                {
                    continue;
                }

                ISaveBackend backend = _saveCoordinator.GetBackend(item.Location.BackendId);
                if (backend == null || (backend.Capabilities & SaveCapabilities.Track) == 0)
                {
                    continue;
                }

                tasks.Add(TrackItemAsync(item, backend));
            }

            await Task.WhenAll(tasks);
        }

        private async Task TrackItemAsync(CardItem item, ISaveBackend backend)
        {
            try
            {
                SaveLocationStatus status = await backend.TrackAsync(item.Location);
                item.Exists = status.Exists;
                if (status.Exists)
                {
                    item.SortTimeUtc = status.WriteTimeUtc;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to track {item.Location}: {e.Message}");
            }
        }

        private static string LocationKey(MapLocation location)
        {
            return location.BackendId + "|" + location.Locator;
        }

        private async Task LoadMissingThumbnailsAsync(List<CardItem> items)
        {
            foreach (CardItem item in items)
            {
                if (!item.NeedsThumbnail)
                {
                    continue;
                }

                byte[] jpeg = await _saveCoordinator.ReadThumbnailAsync(item.Location);
                if (jpeg == null)
                {
                    continue;
                }

                _saveCoordinator.RecentMaps.StoreThumbnail(item.Location, jpeg);

                HomeScreenCardData card = BuildDiscoveredCard(item);
                _view.UpdateCard(item.Location, new HomeScreenCardData(
                    card.Location, card.Name, card.TimeText, card.LocationHint,
                    card.BadgeText, ToTexture(jpeg), card.Chip, card.ShowDelete));
            }
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

        private HomeScreenCardData BuildCard(CardItem item)
        {
            RecentMapEntry entry = item.Entry;
            ISaveBackend backend = _saveCoordinator.GetBackend(entry.Location.BackendId);
            bool trackable = backend != null && (backend.Capabilities & SaveCapabilities.Track) != 0;

            HomeScreenChip chip = HomeScreenChip.None;
            if (backend != null && backend.IsVolatile)
            {
                chip = HomeScreenChip.Volatile;
            }
            else if (!trackable)
            {
                chip = HomeScreenChip.Unknown;
            }
            else if (!item.Exists)
            {
                chip = HomeScreenChip.Missing;
            }

            return new HomeScreenCardData(
                entry.Location,
                entry.Location.DisplayName,
                FormatTime(item.SortTimeUtc),
                backend?.LocationHint(entry.Location),
                BadgeLabel(entry.Location.BackendId),
                LoadThumbnailTexture(entry),
                chip);
        }

        private HomeScreenCardData BuildDiscoveredCard(CardItem item)
        {
            ISaveBackend backend = _saveCoordinator.GetBackend(item.Location.BackendId);

            Texture2D thumbnail = null;
            byte[] jpeg = _saveCoordinator.RecentMaps.LoadThumbnail(item.Location);
            if (jpeg != null)
            {
                thumbnail = ToTexture(jpeg);
            }
            else
            {
                item.NeedsThumbnail = true;
            }

            return new HomeScreenCardData(
                item.Location,
                item.Location.DisplayName,
                FormatTime(item.SortTimeUtc),
                backend?.LocationHint(item.Location),
                BadgeLabel(item.Location.BackendId),
                thumbnail,
                backend != null && backend.IsVolatile ? HomeScreenChip.Volatile : HomeScreenChip.None);
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

        private class CardItem
        {
            public RecentMapEntry Entry;
            public MapLocation Location;
            public DateTime SortTimeUtc;
            public bool Exists = true;
            public bool NeedsThumbnail;
        }
    }
}
