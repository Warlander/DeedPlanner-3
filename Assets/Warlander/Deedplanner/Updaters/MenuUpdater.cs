#if UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || UNITY_TVOS || UNITY_WEBGL || UNITY_WSA || UNITY_PS4 || UNITY_WII || UNITY_XBOXONE || UNITY_SWITCH
    #define DISABLESTEAMWORKS
#else
    #undef DISABLESTEAMWORKS
#endif

#if !DISABLESTEAMWORKS
using Steamworks;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public class MenuUpdater : AbstractUpdater
    {
        [SerializeField] private Button fullscreenButton = null;
        [SerializeField] private Button quitButton = null;
        
        [SerializeField] private TMP_Text steamConnectionText = null;
        [SerializeField] private TMP_Text versionText = null;
        
        private void Start()
        {
            bool mobile = Application.isMobilePlatform;
            bool web = Application.platform == RuntimePlatform.WebGLPlayer;

            if (mobile || web)
            {
                quitButton.gameObject.SetActive(false);
            }

            if (mobile)
            {
                fullscreenButton.gameObject.SetActive(false);
            }

            versionText.text = Constants.TitleString;
        }
        
        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Nothing;

            bool connectedToSteam = SteamManager.ConnectedToSteam;
            steamConnectionText.gameObject.SetActive(connectedToSteam);
#if !DISABLESTEAMWORKS
            if (connectedToSteam)
            {
                steamConnectionText.text = "Connected to Steam as " + SteamFriends.GetPersonaName();
            }
#endif
        }

        public void OnResizeMap()
        {
            GuiManager.Instance.ShowWindow(WindowId.ResizeMap);
        }

        public void OnClearMap()
        {
            GuiManager.Instance.ShowWindow(WindowId.ClearMap);
        }

        public void OnSaveMap()
        {
            GuiManager.Instance.ShowWindow(WindowId.SaveMap);
        }

        public void OnLoadMap()
        {
            GuiManager.Instance.ShowWindow(WindowId.LoadMap);
        }

        public void OnGraphicsSettings()
        {
            GuiManager.Instance.ShowWindow(WindowId.GraphicsSettings);
        }

        public void OnInputSettings()
        {
            GuiManager.Instance.ShowWindow(WindowId.InputSettings);
        }

        public void OnAbout()
        {
            GuiManager.Instance.ShowWindow(WindowId.About);
        }

        public void OnToggleFullscreen()
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

        public void OnQuit()
        {
            // TODO: add auto-saving before quit logic
            Properties.Instance.SaveProperties();
            Application.Quit();
        }

        public void OnPatreon()
        {
            Application.OpenURL("https://www.patreon.com/warlander");
        }

        public void OnPaypal()
        {
            Application.OpenURL("https://www.paypal.me/MCyranowicz/10eur");
        }
    }
}