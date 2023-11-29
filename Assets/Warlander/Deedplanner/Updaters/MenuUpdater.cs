#if UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || UNITY_TVOS || UNITY_WEBGL || UNITY_WSA || UNITY_PS4 || UNITY_WII || UNITY_XBOXONE || UNITY_SWITCH
    #define DISABLESTEAMWORKS
#else
    #undef DISABLESTEAMWORKS
#endif

#if !DISABLESTEAMWORKS
using Steamworks;
#endif
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Steam;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Updaters
{
    public class MenuUpdater : AbstractUpdater
    {
        [Inject] private DPSettings _settings;
        [Inject] private WindowCoordinator _windowCoordinator;
        [Inject] private ISteamConnection _steamConnection;

        [SerializeField] private Button _resizeButton;
        [SerializeField] private Button _clearButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _graphicsSettingsButton;
        [SerializeField] private Button _inputSettingsButton;
        [SerializeField] private Button _creditsButton;
        [SerializeField] private Button _fullscreenButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _patreonButton;
        [SerializeField] private Button _paypalButton;
        
        [SerializeField] private TMP_Text _steamConnectionText;
        [SerializeField] private TMP_Text _versionText;
        
        private void Start()
        {
            bool mobile = Application.isMobilePlatform;
            bool web = Application.platform == RuntimePlatform.WebGLPlayer;

            if (mobile || web)
            {
                _quitButton.gameObject.SetActive(false);
            }

            if (mobile)
            {
                _fullscreenButton.gameObject.SetActive(false);
            }

            _versionText.text = Constants.TitleString;
            
            _resizeButton.onClick.AddListener(ResizeButtonOnClick);
            _clearButton.onClick.AddListener(ClearOnClick);
            _saveButton.onClick.AddListener(SaveOnClick);
            _loadButton.onClick.AddListener(LoadOnClick);
            _graphicsSettingsButton.onClick.AddListener(GraphicsSettingsOnClick);
            _inputSettingsButton.onClick.AddListener(InputSettingsOnClick);
            _creditsButton.onClick.AddListener(CreditsOnClick);
            _fullscreenButton.onClick.AddListener(FullscreenOnClick);
            _quitButton.onClick.AddListener(QuitOnClick);
            _patreonButton.onClick.AddListener(PatreonOnClick);
            _paypalButton.onClick.AddListener(PaypalOnClick);
        }
        
        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Nothing;
            
            _steamConnectionText.gameObject.SetActive(_steamConnection.Connected);
            if (_steamConnection.Connected)
            {
                _steamConnectionText.text = "Connected to Steam as " + _steamConnection.GetName();
            }
        }

        private void ResizeButtonOnClick()
        {
            _windowCoordinator.CreateWindowExclusive(WindowNames.ResizeMapWindow);
        }

        private void ClearOnClick()
        {
            _windowCoordinator.CreateWindowExclusive(WindowNames.ClearMapWindow);
        }

        private void SaveOnClick()
        {
            _windowCoordinator.CreateWindowExclusive(WindowNames.SaveMapWindow);
        }

        private void LoadOnClick()
        {
            _windowCoordinator.CreateWindowExclusive(WindowNames.LoadMapWindow);
        }

        private void GraphicsSettingsOnClick()
        {
            _windowCoordinator.CreateWindowExclusive(WindowNames.GraphicsSettingsWindow);
        }
        
        private void InputSettingsOnClick()
        {
            _windowCoordinator.CreateWindowExclusive(WindowNames.InputSettingsWindow);
        }

        private void CreditsOnClick()
        {
            _windowCoordinator.CreateWindow(WindowNames.CreditsWindow);
        }

        private void FullscreenOnClick()
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

        private void QuitOnClick()
        {
            // TODO: add auto-saving before quit logic
            _settings.Save();
            
#if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
        }

        private void PatreonOnClick()
        {
            Application.OpenURL("https://www.patreon.com/warlander");
        }

        private void PaypalOnClick()
        {
            Application.OpenURL("https://www.paypal.me/MCyranowicz/10eur");
        }
    }
}