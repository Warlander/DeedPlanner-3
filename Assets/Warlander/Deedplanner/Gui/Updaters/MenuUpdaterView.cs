using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public class MenuUpdaterView : MonoBehaviour, IMenuUpdaterView
    {
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

        public event Action<MenuAction> ButtonClicked;

        private void Awake()
        {
            _resizeButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Resize));
            _clearButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Clear));
            _saveButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Save));
            _loadButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Load));
            _graphicsSettingsButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.GraphicsSettings));
            _inputSettingsButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.InputSettings));
            _creditsButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Credits));
            _fullscreenButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Fullscreen));
            _quitButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Quit));
            _patreonButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Patreon));
            _paypalButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Paypal));
        }

        public void SetQuitButtonVisible(bool visible)
        {
            _quitButton.gameObject.SetActive(visible);
        }

        public void SetFullscreenButtonVisible(bool visible)
        {
            _fullscreenButton.gameObject.SetActive(visible);
        }

        public void SetVersionText(string text)
        {
            _versionText.text = text;
        }

        public void SetSteamStatus(bool visible, string text)
        {
            _steamConnectionText.gameObject.SetActive(visible);
            if (visible)
            {
                _steamConnectionText.text = text;
            }
        }
    }
}
