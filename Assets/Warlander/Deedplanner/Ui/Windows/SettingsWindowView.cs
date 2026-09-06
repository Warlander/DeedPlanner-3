using System;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Ui.Widgets;
using Warlogic.Settings;
using Warlogic.Settings.Ugui;

namespace Warlander.Deedplanner.Ui.Windows
{
    public class SettingsWindowView : MonoBehaviour, ISettingsWindowView, IRebindOverlay
    {
        [SerializeField] private RectTransform tabButtonRoot;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private ScrollRect contentScrollRect;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private Button resetBindingsButton;
        [SerializeField] private SettingsWidgetCatalog widgetCatalog;
        [SerializeField] private KeybindSettingWidget keybindRowPrefab;
        [SerializeField] private CanvasGroup rebindFade;
        [SerializeField] private TMP_Text rebindText;

        [Inject] private InputSettings _inputSettings;

        private SettingsWindowPresenter _presenter;
        private SettingsRegistry _registry;

        public event Action SaveClicked;
        public event Action DiscardClicked;

        public RectTransform TabButtonRoot => tabButtonRoot;
        public RectTransform ContentRoot => contentRoot;

        private void Awake()
        {
            saveButton.onClick.AddListener(() => SaveClicked?.Invoke());
            discardButton.onClick.AddListener(() => DiscardClicked?.Invoke());
            resetBindingsButton.onClick.AddListener(ResetBindingsOnClick);

            rebindFade.gameObject.SetActive(false);
            resetBindingsButton.gameObject.SetActive(false);
            // all settings apply immediately; buttons kept for future OnSave settings
            saveButton.gameObject.SetActive(false);
            discardButton.gameObject.SetActive(false);
        }

        public SettingsWindowPresenter Initialize(SettingsRegistry registry)
        {
            _registry = registry;
            widgetCatalog.RegisterFactory(typeof(KeybindSetting), new KeybindSettingWidgetFactory(keybindRowPrefab, this));
            _presenter = new SettingsWindowPresenter(registry, widgetCatalog, this);
            _presenter.ActiveTabChanged += OnActiveTabChanged;
            OnActiveTabChanged(registry.Tabs[0].Id);
            return _presenter;
        }

        public void SetSaveInteractable(bool interactable)
        {
            saveButton.interactable = interactable;
        }

        public void SetDiscardInteractable(bool interactable)
        {
            discardButton.interactable = interactable;
        }

        public void ShowRebind(string bindingLabel)
        {
            rebindText.text = $"Waiting for input to rebind\n\n{bindingLabel}";
            contentScrollRect.enabled = false;
            rebindFade.gameObject.SetActive(true);
            rebindFade.alpha = 0;
            rebindFade.DOFade(1, 0.15f);
        }

        public void HideRebind()
        {
            contentScrollRect.enabled = true;
            rebindFade.DOFade(0, 0.15f).OnComplete(() => rebindFade.gameObject.SetActive(false));
        }

        private void OnActiveTabChanged(string tabId)
        {
            SettingsTab tab = _registry.Tabs.First(t => t.Id == tabId);
            bool hasKeybinds = tab.Settings.Any(s => s is KeybindSetting);
            resetBindingsButton.gameObject.SetActive(hasKeybinds);
        }

        private void ResetBindingsOnClick()
        {
            _inputSettings.Reset();
        }

        private void OnDestroy()
        {
            rebindFade.DOKill();
        }
    }
}
