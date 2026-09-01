using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Editing
{
    public class MenuUpdaterView : MonoBehaviour, IMenuUpdaterView
    {
        [SerializeField] private Button _resizeButton;
        [SerializeField] private Button _clearButton;
        [SerializeField] private Button _newButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _saveAsButton;
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
        [SerializeField] private TMP_Text _saveStateText;

        private static readonly Color SavedColor = new Color(0.55f, 0.85f, 0.6f);
        private static readonly Color UnsavedColor = new Color(0.95f, 0.75f, 0.35f);

        public event Action<MenuAction> ButtonClicked;

        private void Awake()
        {
            _resizeButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Resize));
            _clearButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Clear));
            _newButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.New));
            _saveButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.Save));
            _saveAsButton.onClick.AddListener(() => ButtonClicked?.Invoke(MenuAction.SaveAs));
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

        public void SetFundingLinksVisible(bool visible)
        {
            _patreonButton.gameObject.SetActive(visible);
            _paypalButton.gameObject.SetActive(visible);
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

        public void SetSaveIndicator(string text, bool hasUnsavedChanges)
        {
            _saveStateText.text = text;
            _saveStateText.color = hasUnsavedChanges ? UnsavedColor : SavedColor;
        }
    }
}
