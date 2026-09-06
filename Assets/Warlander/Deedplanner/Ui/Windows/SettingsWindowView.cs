using System;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Ui.Widgets;
using Warlogic.Settings;
using Warlogic.Settings.Ugui;

namespace Warlander.Deedplanner.Ui.Windows
{
    public class SettingsWindowView : MonoBehaviour, ISettingsWindowView
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

        public event Action SaveClicked;
        public event Action DiscardClicked;
        public event Action ResetBindingsClicked;
        public event Action Destroyed;

        public RectTransform TabButtonRoot => tabButtonRoot;
        public RectTransform ContentRoot => contentRoot;
        public SettingsWidgetCatalog WidgetCatalog => widgetCatalog;
        public KeybindSettingWidget KeybindRowPrefab => keybindRowPrefab;

        private void Awake()
        {
            saveButton.onClick.AddListener(() => SaveClicked?.Invoke());
            discardButton.onClick.AddListener(() => DiscardClicked?.Invoke());
            resetBindingsButton.onClick.AddListener(() => ResetBindingsClicked?.Invoke());

            rebindFade.gameObject.SetActive(false);
            resetBindingsButton.gameObject.SetActive(false);
            // all settings apply immediately; buttons kept for future OnSave settings
            saveButton.gameObject.SetActive(false);
            discardButton.gameObject.SetActive(false);
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

        public void SetResetBindingsVisible(bool visible)
        {
            resetBindingsButton.gameObject.SetActive(visible);
        }

        private void OnDestroy()
        {
            rebindFade.DOKill();
            Destroyed?.Invoke();
        }
    }

    public sealed class SettingsWindowSession : IRebindOverlay, IDisposable
    {
        private readonly SettingsRegistry _registry;
        private readonly InputSettings _inputSettings;
        private readonly SettingsWindowView _view;
        private readonly SettingsWindowPresenter _presenter;
        private KeybindSetting _activeRebind;
        private bool _disposed;

        public SettingsWindowSession(SettingsRegistry registry, InputSettings inputSettings, SettingsWindowView view)
        {
            _registry = registry;
            _inputSettings = inputSettings;
            _view = view;

            view.WidgetCatalog.RegisterFactory(typeof(KeybindSetting),
                new KeybindSettingWidgetFactory(view.KeybindRowPrefab, this));
            _presenter = new SettingsWindowPresenter(registry, view.WidgetCatalog, view);
            _presenter.ActiveTabChanged += OnActiveTabChanged;
            _view.ResetBindingsClicked += OnResetBindingsClicked;
            _view.Destroyed += Dispose;

            if (registry.Tabs.Count > 0)
            {
                OnActiveTabChanged(registry.Tabs[0].Id);
            }
        }

        public void BeginRebind(KeybindSetting setting)
        {
            CancelActiveRebind();
            _activeRebind = setting;
            _view.ShowRebind(setting.Label);
            setting.PerformInteractiveRebind(
                () => FinishRebind(setting),
                () => FinishRebind(setting));
        }

        private void FinishRebind(KeybindSetting setting)
        {
            if (_activeRebind != setting)
            {
                return;
            }

            _activeRebind = null;
            if (!_disposed)
            {
                _view.HideRebind();
            }
        }

        private void CancelActiveRebind()
        {
            _activeRebind?.CancelInteractiveRebind();
        }

        private void OnActiveTabChanged(string tabId)
        {
            SettingsTab tab = _registry.Tabs.First(t => t.Id == tabId);
            _view.SetResetBindingsVisible(tab.Settings.Any(setting => setting is KeybindSetting));
        }

        private void OnResetBindingsClicked()
        {
            CancelActiveRebind();
            _inputSettings.Reset();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _presenter.ActiveTabChanged -= OnActiveTabChanged;
            _view.ResetBindingsClicked -= OnResetBindingsClicked;
            _view.Destroyed -= Dispose;

            KeybindSetting activeRebind = _activeRebind;
            _activeRebind = null;
            activeRebind?.CancelInteractiveRebind();
        }
    }
}
