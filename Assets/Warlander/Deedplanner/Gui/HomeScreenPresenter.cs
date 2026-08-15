using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Warlander.Deedplanner.Logic.Saving;
using Warlander.Deedplanner.Settings;
using VContainer.Unity;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Gui
{
    public class HomeScreenPresenter : IInitializable, IDisposable, ITickable
    {
        private readonly IHomeScreenView _view;
        private readonly SaveCoordinator _saveCoordinator;
        private readonly AutoSaveScheduler _autoSaveScheduler;
        private readonly WindowCoordinator _windowCoordinator;
        private readonly DPSettings _settings;

        private string _selectedBackendId;
        private readonly Dictionary<MapLocation, MapLocation?> _recoveryMains =
            new Dictionary<MapLocation, MapLocation?>();

        public HomeScreenPresenter(IHomeScreenView view, SaveCoordinator saveCoordinator,
            AutoSaveScheduler autoSaveScheduler, WindowCoordinator windowCoordinator, DPSettings settings)
        {
            _view = view;
            _saveCoordinator = saveCoordinator;
            _autoSaveScheduler = autoSaveScheduler;
            _windowCoordinator = windowCoordinator;
            _settings = settings;
        }

        public void Initialize()
        {
            _view.BackClicked += OnBack;
            _view.NewDeedClicked += OnNewDeed;
            _view.LoadClicked += OnLoad;
            _view.WebLinkClicked += OnWebLink;
            _view.AboutClicked += OnAbout;
            _view.QuitClicked += OnQuit;
            _view.CategoryClicked += OnCategory;
            _view.CardClicked += OnCard;

            _view.SetLoadButtonVisible(true);
        }

        public void Dispose() { }

        public void Tick()
        {
            if (_view.Visible && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnBack();
            }
        }

        /// Hides the screen without touching the current map: at startup the blank default waits
        /// behind it, in-session this simply returns to the deed being edited.
        private void OnBack()
        {
            _view.Hide();
        }

        public void ShowHomeScreen()
        {
            _selectedBackendId = null;
            _view.Show();
            Populate();
        }

        private void OnNewDeed()
        {
            _ = _saveCoordinator.NewMapAsync();
            _view.Hide();
        }

        private async void OnLoad()
        {
            if (_saveCoordinator.GetBackend("file") != null)
            {
                bool loaded = await _saveCoordinator.PickAndLoadAsync("file");
                if (loaded)
                {
                    _view.Hide();
                }
            }
            else
            {
                // no file backend (WebGL): the load window provides the browser file picker
                _windowCoordinator.CreateWindowExclusive(WindowNames.LoadMapWindow);
                _view.Hide();
            }
        }

        private void OnWebLink()
        {
            _windowCoordinator.CreateWindowExclusive(WindowNames.LoadMapWindow);
            _view.Hide();
        }

        private void OnAbout()
        {
            _windowCoordinator.CreateWindow(WindowNames.CreditsWindow);
        }

        private void OnQuit()
        {
            _ = QuitAsync();
        }

        private async System.Threading.Tasks.Task QuitAsync()
        {
            await _saveCoordinator.PrepareForQuitAsync();
            _settings.Save();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        private void OnCategory(string backendId)
        {
            _selectedBackendId = backendId;
            Populate();
        }

        private async void OnCard(MapLocation location)
        {
            if (location.BackendId == "webfile")
            {
                // browser downloads cannot be re-read; the load window provides the file picker
                _windowCoordinator.CreateWindowExclusive(WindowNames.LoadMapWindow);
                _view.Hide();
                return;
            }

            if (_recoveryMains.TryGetValue(location, out MapLocation? mainLocation))
            {
                bool recovered = await _saveCoordinator.LoadRecoveryAsync(location, mainLocation);
                if (recovered)
                {
                    _view.Hide();
                }
                else
                {
                    Populate();
                }

                return;
            }

            bool loaded = await _saveCoordinator.LoadAsync(location);
            if (loaded)
            {
                _view.Hide();
            }
            else
            {
                Populate();
            }
        }

        private void Populate()
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
                if (_selectedBackendId != null && entry.Location.BackendId != _selectedBackendId)
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

            if (_selectedBackendId == null || _selectedBackendId == "file")
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
                if (_selectedBackendId != null && entry.Location.BackendId != _selectedBackendId)
                {
                    continue;
                }

                cards.Add(BuildCard(entry));
            }

            _view.SetCards(cards);
            _ = RefreshCardStatusesAsync();
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
                HomeScreenChip.Recovery);
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
                if (_selectedBackendId != null && entry.Location.BackendId != _selectedBackendId)
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
                    TrackResult track = await backend.TrackAsync(entry.Location);
                    if (!track.Exists)
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

        private static string CategoryLabel(string backendId)
        {
            switch (backendId)
            {
                case "file": return "Local files";
                case "steamcloud": return "Steam Cloud";
                case "localstorage": return "Browser storage";
                case "pastebin": return "Pastebin";
                default: return null;
            }
        }

        private static string BadgeLabel(string backendId)
        {
            switch (backendId)
            {
                case "file": return "FILE";
                case "steamcloud": return "STEAM";
                case "localstorage": return "BROWSER";
                case "pastebin": return "PASTE";
                case "webfile": return "FILE";
                default: return backendId.ToUpperInvariant();
            }
        }
    }
}
