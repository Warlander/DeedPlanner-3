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

        }

        public void OnClearMap()
        {

        }

        public void OnSaveMap()
        {

        }

        public void OnLoadMap()
        {

        }

        public void OnGraphicsSettings()
        {

        }

        public void OnInputSettings()
        {

        }

        public void OnToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
        }

        public void OnQuit()
        {
            Application.Quit();
        }

    }
}
