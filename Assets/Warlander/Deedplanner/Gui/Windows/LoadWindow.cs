using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Saving;
using Warlander.Deedplanner.Utils;
using VContainer;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class LoadWindow : MonoBehaviour
    {
        private Window _window;
        [Inject] private MapHandler _mapHandler;
        [Inject] private SaveCoordinator _saveCoordinator;

        [SerializeField] private Button _loadFromFileButton;
        [SerializeField] private Button _loadFromWebButton;
        [SerializeField] private TMP_InputField _pastebinInput = null;
        [SerializeField] private GameObject _webSaveGroup;

        private void Awake()
        {
            _window = GetComponentInParent<Window>(true);
        }

        private void Start()
        {
            _loadFromFileButton.onClick.AddListener(LoadFromFileOnClick);
            _loadFromWebButton.onClick.AddListener(LoadFromWebOnClick);
            
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                _webSaveGroup.gameObject.SetActive(false);
            }
        }

        private void LoadFromFileOnClick()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                LoadFileBrowser();
            }
            else
            {
                LoadFileStandalone();
            }
        }

        private async void LoadFileStandalone()
        {
            bool loaded = await _saveCoordinator.PickAndLoadAsync("file");
            if (loaded)
            {
                _window.Close();
            }
        }

        private async void LoadFromWebOnClick()
        {
            string rawLink = _pastebinInput.text;
            string requestLink = WebLinkUtils.ParseToDirectDownloadLink(rawLink);

            try
            {
                await _mapHandler.LoadMapAsync(new Uri(requestLink));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Unable to load map from: " + rawLink);
                if (Debug.isDebugBuild)
                {
                    Debug.LogError(e);
                }
            }

            _window.Close();
        }

        private void LoadFileBrowser()
        {
#if UNITY_WEBGL
            JavaScriptUtils.UploadNative(gameObject.name, nameof(LoadFileBrowserCallback));
#endif
        }

        public void LoadFileBrowserCallback(string result)
        {
            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            _mapHandler.LoadMap(result);
            _window.Close();
        }
    }
}