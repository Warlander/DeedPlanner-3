using System;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic.Saving;
using VContainer;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class SaveWindow : MonoBehaviour
    {
        private Window _window;
        [Inject] private SaveCoordinator _saveCoordinator;

        [SerializeField] private Button _saveToFileButton;
        [SerializeField] private Button _pastebinButton;
        [SerializeField] private Button _webVersionButton;
        [SerializeField] private GameObject _webSaveGroup;

        private void Awake()
        {
            _window = GetComponentInParent<Window>(true);
        }

        private void Start()
        {
            _saveToFileButton.onClick.AddListener(SaveToFileOnClick);
            _pastebinButton.onClick.AddListener(PastebinOnClick);
            _webVersionButton.onClick.AddListener(WebVersionOnClick);

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                _webSaveGroup.gameObject.SetActive(false);
            }
        }

        private async void SaveToFileOnClick()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Utils.JavaScriptUtils.DownloadNative("Deed plan.MAP", _saveCoordinator.SerializeCurrentMap());
            _window.Close();
#else
            MapLocation? location = await _saveCoordinator.SaveAsync("file");
            if (location.HasValue)
            {
                _window.Close();
            }
#endif
        }

        private async void PastebinOnClick()
        {
            SetPastebinButtonsInteractable(false);
            try
            {
                MapLocation? location = await _saveCoordinator.SaveAsync("pastebin");
                if (location.HasValue)
                {
                    Application.OpenURL(location.Value.Locator);
                    _window.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                SetPastebinButtonsInteractable(true);
            }
        }

        private async void WebVersionOnClick()
        {
            SetPastebinButtonsInteractable(false);
            try
            {
                MapLocation? location = await _saveCoordinator.SaveAsync("pastebin");
                if (location.HasValue)
                {
                    Application.OpenURL(Constants.WebVersionLink + "?map=" + location.Value.Locator);
                    _window.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                SetPastebinButtonsInteractable(true);
            }
        }

        private void SetPastebinButtonsInteractable(bool interactable)
        {
            _pastebinButton.interactable = interactable;
            _webVersionButton.interactable = interactable;
        }
    }
}
