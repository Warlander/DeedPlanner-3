#if UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || UNITY_TVOS || UNITY_WEBGL || UNITY_WSA || UNITY_PS4 || UNITY_WII || UNITY_XBOXONE || UNITY_SWITCH
    #define DISABLESTEAMWORKS
#else
    #undef DISABLESTEAMWORKS
#endif

using UnityEditor;
using UnityEngine;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Updaters;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Steam;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Updaters
{
    public class MenuUpdater : IUpdater
    {
        private readonly IMenuUpdaterView _view;
        private readonly DPSettings _settings;
        private readonly WindowCoordinator _windowCoordinator;
        private readonly ISteamConnection _steamConnection;
        private readonly TabContext _tabContext;

        public Tab TargetTab => Tab.Menu;

        public MenuUpdater(IMenuUpdaterView view, DPSettings settings, WindowCoordinator windowCoordinator,
            ISteamConnection steamConnection, TabContext tabContext)
        {
            _view = view;
            _settings = settings;
            _windowCoordinator = windowCoordinator;
            _steamConnection = steamConnection;
            _tabContext = tabContext;
        }

        public void Initialize()
        {
            bool mobile = Application.isMobilePlatform;
            bool web = Application.platform == RuntimePlatform.WebGLPlayer;

            if (mobile || web)
            {
                _view.SetQuitButtonVisible(false);
            }

            if (mobile)
            {
                _view.SetFullscreenButtonVisible(false);
            }

            _view.SetVersionText(Constants.TitleString);

            _view.ButtonClicked += OnButtonClicked;
        }

        public void Enable()
        {
            _tabContext.TileSelectionMode = TileSelectionMode.Nothing;

            if (_steamConnection.Connected)
            {
                _view.SetSteamStatus(true, "Connected to Steam as " + _steamConnection.GetName());
            }
            else
            {
                _view.SetSteamStatus(false, null);
            }
        }

        public void Disable() { }

        public void Tick() { }

        private void OnButtonClicked(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.Resize:
                    _windowCoordinator.CreateWindowExclusive(WindowNames.ResizeMapWindow);
                    break;
                case MenuAction.Clear:
                    _windowCoordinator.CreateWindowExclusive(WindowNames.ClearMapWindow);
                    break;
                case MenuAction.Save:
                    _windowCoordinator.CreateWindowExclusive(WindowNames.SaveMapWindow);
                    break;
                case MenuAction.Load:
                    _windowCoordinator.CreateWindowExclusive(WindowNames.LoadMapWindow);
                    break;
                case MenuAction.GraphicsSettings:
                    _windowCoordinator.CreateWindowExclusive(WindowNames.GraphicsSettingsWindow);
                    break;
                case MenuAction.InputSettings:
                    _windowCoordinator.CreateWindowExclusive(WindowNames.InputSettingsWindow);
                    break;
                case MenuAction.Credits:
                    _windowCoordinator.CreateWindow(WindowNames.CreditsWindow);
                    break;
                case MenuAction.Fullscreen:
                    ToggleFullscreen();
                    break;
                case MenuAction.Quit:
                    Quit();
                    break;
                case MenuAction.Patreon:
                    Application.OpenURL("https://www.patreon.com/warlander");
                    break;
                case MenuAction.Paypal:
                    Application.OpenURL("https://www.paypal.me/MCyranowicz/10eur");
                    break;
            }
        }

        private void ToggleFullscreen()
        {
            if (Screen.fullScreen)
            {
                Screen.fullScreen = false;
            }
            else
            {
                // makes sure fullscreen mode always uses intended fullscreen window mode instead of native window or other fullscreen mode saved in user settings
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            }
        }

        private void Quit()
        {
            // TODO: add auto-saving before quit logic
            _settings.Save();

#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}
