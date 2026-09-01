using Warlander.Deedplanner.Platform.Web;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Persistence;
using Warlander.Deedplanner.Utils;
using VContainer;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class LoadWindow : MonoBehaviour
    {
        private Window _window;
        [Inject] private MapHandler _mapHandler;
        [Inject] private ISaveCoordinator _saveCoordinator;

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
            bool loaded = await _saveCoordinator.PickAndLoadAsync(SaveBackendId.File);
            if (loaded)
            {
                _window.Close();
            }
        }

        private async void LoadFromWebOnClick()
        {
            await _saveCoordinator.LoadFromWebAsync(_pastebinInput.text);

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