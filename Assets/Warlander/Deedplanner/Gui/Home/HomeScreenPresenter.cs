using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Warlander.Deedplanner.Logic.Saving;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Platform.Steam;
using VContainer.Unity;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Gui.Home
{
    public class HomeScreenPresenter : IHomeScreenPresenter, IInitializable, IDisposable, ITickable
    {
        private readonly IHomeScreenView _view;
        private readonly ISaveCoordinator _saveCoordinator;
        private readonly HomeScreenCardCatalog _cardCatalog;
        private readonly WindowCoordinator _windowCoordinator;
        private readonly DPSettings _settings;
        private readonly ISteamConnection _steamConnection;
        private readonly ICategoryLogger _logger;

        public HomeScreenPresenter(IHomeScreenView view, ISaveCoordinator saveCoordinator,
            HomeScreenCardCatalog cardCatalog, WindowCoordinator windowCoordinator, DPSettings settings,
            ISteamConnection steamConnection, UiLog uiLog)
        {
            _view = view;
            _saveCoordinator = saveCoordinator;
            _cardCatalog = cardCatalog;
            _windowCoordinator = windowCoordinator;
            _settings = settings;
            _steamConnection = steamConnection;
            _logger = uiLog.Logger;
        }

        public void Initialize()
        {
            _view.BackClicked += OnBack;
            _view.NewDeedClicked += OnNewDeed;
            _view.LoadClicked += OnLoad;
            _view.WebLinkClicked += OnWebLink;
            _view.AboutClicked += OnAbout;
            _view.QuitClicked += OnQuit;
            _view.PatreonClicked += OnPatreon;
            _view.PaypalClicked += OnPaypal;
            _view.CategoryClicked += OnCategory;
            _view.CardClicked += OnCard;
            _view.CardDeleteClicked += OnDeleteCard;

            _view.SetLoadButtonVisible(true);
            // Steam policy: no external funding links while running under Steam
            _view.SetFundingLinksVisible(!_steamConnection.Connected);
        }

        public void Dispose() { }

        public void Tick()
        {
            if (_view.Visible && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnBack();
            }
        }

        public void ShowHomeScreen(bool animated)
        {
            _cardCatalog.ResetCategory();
            _view.Show(animated);
            _cardCatalog.Populate();
        }

        public void HideHomeScreen()
        {
            _view.Hide(false);
        }

        /// Hides the screen without touching the current map: at startup the blank default waits
        /// behind it, in-session this simply returns to the deed being edited.
        private void OnBack()
        {
            _view.Hide(true);
        }

        private void OnNewDeed()
        {
            _ = _saveCoordinator.NewMapAsync();
            _view.Hide(true);
        }

        private async void OnLoad()
        {
            if (_saveCoordinator.GetBackend(SaveBackendId.File) != null)
            {
                bool loaded = await _saveCoordinator.PickAndLoadAsync(SaveBackendId.File);
                if (loaded)
                {
                    _view.Hide(true);
                }
            }
            else
            {
                // no file backend (WebGL): the load window provides the browser file picker
                _windowCoordinator.CreateWindowExclusive(WindowNames.LoadMapWindow);
                _view.Hide(true);
            }
        }

        private void OnWebLink()
        {
            _windowCoordinator.CreateWindowExclusive(WindowNames.LoadMapWindow);
            _view.Hide(true);
        }

        private void OnAbout()
        {
            _windowCoordinator.CreateWindow(WindowNames.CreditsWindow);
        }

        private void OnPatreon()
        {
            Application.OpenURL("https://www.patreon.com/warlander");
        }

        private void OnPaypal()
        {
            Application.OpenURL("https://www.paypal.me/MCyranowicz/10eur");
        }

        private void OnQuit()
        {
            _ = QuitAsync();
        }

        private async Task QuitAsync()
        {
            await _saveCoordinator.PrepareForQuitAsync();
            _settings.Save();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        private void OnCategory(SaveBackendId? backendId)
        {
            _cardCatalog.SelectCategory(backendId);
        }

        private async void OnCard(MapLocation location)
        {
            if (location.BackendId == SaveBackendId.WebFile)
            {
                // browser downloads cannot be re-read; the load window provides the file picker
                _windowCoordinator.CreateWindowExclusive(WindowNames.LoadMapWindow);
                _view.Hide(true);
                return;
            }

            if (_cardCatalog.RecoveryMains.TryGetValue(location, out MapLocation? mainLocation))
            {
                bool recovered = await _saveCoordinator.LoadRecoveryAsync(location, mainLocation);
                if (recovered)
                {
                    _view.Hide(true);
                }
                else
                {
                    _cardCatalog.Populate();
                }

                return;
            }

            bool loaded = await _saveCoordinator.LoadAsync(location);
            if (loaded)
            {
                _view.Hide(true);
            }
            else
            {
                _cardCatalog.Populate();
            }
        }

        private async void OnDeleteCard(MapLocation location)
        {
            ISaveBackend backend = _saveCoordinator.GetBackend(location.BackendId);
            bool realDelete = backend != null && (backend.Capabilities & SaveCapabilities.Delete) != 0;

            // missing saves lose nothing when removed from the list, no confirmation needed
            if (realDelete && (backend.Capabilities & SaveCapabilities.Track) != 0)
            {
                try
                {
                    SaveLocationStatus status = await backend.TrackAsync(location);
                    if (!status.Exists)
                    {
                        await _saveCoordinator.DeleteSaveAsync(location);
                        _cardCatalog.Populate();
                        return;
                    }
                }
                catch (Exception e)
                {
                    _logger.Warning($"Failed to track {location} before delete: {e.Message}");
                }
            }

            string message;
            if (realDelete)
            {
                message = $"Delete '{location.DisplayName}' permanently?\n\n" +
                          "The save and its auto-saves will be deleted. This cannot be undone.";
            }
            else
            {
                string kept = location.BackendId == SaveBackendId.Pastebin
                    ? "The paste itself stays online."
                    : "The file itself will not be deleted.";
                message = $"Remove '{location.DisplayName}' from the list?\n\n{kept}";
            }

            Window window = _windowCoordinator.CreateWindowExclusive(WindowNames.DeleteSaveWindow);
            window.GetComponentInChildren<Windows.DeleteSaveWindowView>(true)
                .SetMessage(message, () => _ = DeleteConfirmedAsync(location));
        }

        private async Task DeleteConfirmedAsync(MapLocation location)
        {
            await _saveCoordinator.DeleteSaveAsync(location);
            _cardCatalog.Populate();
        }
    }
}
