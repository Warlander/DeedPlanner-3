using System;
using System.Collections.Generic;
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
        private readonly WindowCoordinator _windowCoordinator;
        private readonly DPSettings _settings;

        private string _selectedBackendId;

        public HomeScreenPresenter(IHomeScreenView view, SaveCoordinator saveCoordinator,
            WindowCoordinator windowCoordinator, DPSettings settings)
        {
            _view = view;
            _saveCoordinator = saveCoordinator;
            _windowCoordinator = windowCoordinator;
            _settings = settings;
        }

        public void Initialize()
        {
            _view.NewDeedClicked += OnNewDeed;
            _view.LoadClicked += OnLoad;
            _view.WebLinkClicked += OnWebLink;
            _view.AboutClicked += OnAbout;
            _view.QuitClicked += OnQuit;
            _view.CategoryClicked += OnCategory;
            _view.CardClicked += OnCard;

            _view.SetLoadButtonVisible(_saveCoordinator.GetBackend("file") != null);
        }

        public void Dispose() { }

        public void Tick()
        {
            if (_view.Visible && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnNewDeed();
            }
        }

        public void ShowHomeScreen()
        {
            _selectedBackendId = null;
            _view.Show();
            Populate();
        }

        private void OnNewDeed()
        {
            _saveCoordinator.NewMap();
            _view.Hide();
        }

        private async void OnLoad()
        {
            bool loaded = await _saveCoordinator.PickAndLoadAsync("file");
            if (loaded)
            {
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
            var categories = new List<HomeScreenCategory>();
            foreach (ISaveBackend backend in _saveCoordinator.Backends)
            {
                string label = CategoryLabel(backend.Id);
                if (label != null)
                {
                    categories.Add(new HomeScreenCategory(backend.Id, label));
                }
            }

            _view.SetCategories(categories, _selectedBackendId);

            var cards = new List<HomeScreenCardData>();
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
            if (jpeg == null)
            {
                return null;
            }

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
