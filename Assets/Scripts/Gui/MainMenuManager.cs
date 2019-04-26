using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui
{
    public class MainMenuManager : MonoBehaviour
    {

        [SerializeField]
        private Button fullscreenButton = null;
        [SerializeField]
        private Button quitButton = null;

        private void Start()
        {
            bool mobile = Application.isMobilePlatform;
            bool web = Application.platform == RuntimePlatform.WebGLPlayer;

            if (mobile || web)
            {
                fullscreenButton.gameObject.SetActive(false);
                quitButton.gameObject.SetActive(false);
            }
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

        public void OnToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
        }

        public void OnQuit()
        {
            // TODO: add auto-saving before quit logic
            Application.Quit();
        }

    }
}
