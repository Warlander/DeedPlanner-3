#if UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || UNITY_TVOS || UNITY_WEBGL || UNITY_WSA || UNITY_PS4 || UNITY_WII || UNITY_XBOXONE || UNITY_SWITCH
    #define DISABLESTEAMWORKS
#endif

#if !DISABLESTEAMWORKS
using Steamworks;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public class MenuUpdater : MonoBehaviour
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
            GuiManager.Instance.ResizeMapWindow.gameObject.SetActive(true);
        }

        public void OnClearMap()
        {
            GuiManager.Instance.ClearMapWindow.gameObject.SetActive(true);
        }

        public void OnSaveMap()
        {
            GuiManager.Instance.SaveMapWindow.gameObject.SetActive(true);
        }

        public void OnLoadMap()
        {
            GuiManager.Instance.LoadMapWindow.gameObject.SetActive(true);
        }

        public void OnGraphicsSettings()
        {
            GuiManager.Instance.GraphicsSettingsWindow.gameObject.SetActive(true);
        }

        public void OnInputSettings()
        {
            GuiManager.Instance.InputSettingsWindow.gameObject.SetActive(true);
        }

        public void OnAbout()
        {
            GuiManager.Instance.AboutWindow.gameObject.SetActive(true);
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