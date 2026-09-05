using System;
using UnityEngine;
using UnityEngine.UI;
using Warlogic.Settings;
using Warlogic.Settings.Ugui;

namespace Warlander.Deedplanner.Ui.Windows
{
    public class SettingsWindowView : MonoBehaviour, ISettingsWindowView
    {
        [SerializeField] private RectTransform tabButtonRoot;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private SettingsWidgetCatalog widgetCatalog;

        public event Action SaveClicked;
        public event Action DiscardClicked;

        public RectTransform TabButtonRoot => tabButtonRoot;
        public RectTransform ContentRoot => contentRoot;

        private void Awake()
        {
            saveButton.onClick.AddListener(() => SaveClicked?.Invoke());
            discardButton.onClick.AddListener(() => DiscardClicked?.Invoke());
        }

        public SettingsWindowPresenter Initialize(SettingsRegistry registry)
        {
            return new SettingsWindowPresenter(registry, widgetCatalog, this);
        }

        public void SetSaveInteractable(bool interactable)
        {
            saveButton.interactable = interactable;
        }

        public void SetDiscardInteractable(bool interactable)
        {
            discardButton.interactable = interactable;
        }
    }
}
