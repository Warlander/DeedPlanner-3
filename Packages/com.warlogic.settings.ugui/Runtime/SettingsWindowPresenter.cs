using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlogic.Settings.Ugui
{
    public sealed class SettingsWindowPresenter
    {
        private readonly SettingsRegistry _registry;
        private readonly SettingsWidgetCatalog _catalog;
        private readonly ISettingsWindowView _view;
        private readonly Dictionary<string, List<ISettingWidget>> _widgetsByTab = new Dictionary<string, List<ISettingWidget>>();
        private readonly Dictionary<string, SettingsTabButton> _tabButtons = new Dictionary<string, SettingsTabButton>();
        private string _activeTabId;

        public event Action<string> TabContentBuilt;

        public SettingsWindowPresenter(SettingsRegistry registry, SettingsWidgetCatalog catalog, ISettingsWindowView view)
        {
            _registry = registry;
            _catalog = catalog;
            _view = view;
            _view.SaveClicked += SaveAll;
            _view.DiscardClicked += DiscardAll;
            BuildTabButtons();
            RefreshFooter();
            if (_registry.Tabs.Count > 0)
            {
                SelectTab(_registry.Tabs[0].Id);
            }
        }

        public bool IsTabBuilt(string tabId)
        {
            return _widgetsByTab.ContainsKey(tabId);
        }

        public void SelectTab(string tabId)
        {
            if (tabId == _activeTabId)
            {
                return;
            }
            if (_activeTabId != null)
            {
                SetTabContentVisible(_activeTabId, false);
                _tabButtons[_activeTabId].SetSelected(false);
            }
            _activeTabId = tabId;
            if (!IsTabBuilt(tabId))
            {
                BuildTabContent(tabId);
            }
            SetTabContentVisible(tabId, true);
            _tabButtons[tabId].SetSelected(true);
        }

        private void BuildTabButtons()
        {
            foreach (SettingsTab tab in _registry.Tabs)
            {
                SettingsTabButton button = _catalog.CreateTabButton(_view.TabButtonRoot);
                button.SetLabel(tab.Label);
                string tabId = tab.Id;
                button.Clicked += () => SelectTab(tabId);
                _tabButtons[tabId] = button;
            }
        }

        private void BuildTabContent(string tabId)
        {
            SettingsTab tab = _registry.Tabs.First(t => t.Id == tabId);
            var widgets = new List<ISettingWidget>();
            foreach (ISetting setting in tab.Settings)
            {
                ISettingWidget widget = _catalog.CreateWidget(setting, _view.ContentRoot);
                if (widget == null)
                {
                    continue;
                }
                widget.ValueEdited += RefreshFooter;
                widget.Root.gameObject.SetActive(false);
                widgets.Add(widget);
            }
            _widgetsByTab[tabId] = widgets;
            TabContentBuilt?.Invoke(tabId);
        }

        private void SetTabContentVisible(string tabId, bool visible)
        {
            foreach (ISettingWidget widget in _widgetsByTab[tabId])
            {
                widget.Root.gameObject.SetActive(visible);
            }
        }

        private void SaveAll()
        {
            foreach (SettingsTab tab in _registry.Tabs)
            {
                foreach (ISetting setting in tab.Settings)
                {
                    if (setting.ApplyMode == ApplyMode.OnSave && setting.IsDirty)
                    {
                        setting.Commit();
                    }
                }
            }
            RefreshFooter();
        }

        private void DiscardAll()
        {
            foreach (SettingsTab tab in _registry.Tabs)
            {
                foreach (ISetting setting in tab.Settings)
                {
                    setting.Revert();
                }
            }
            foreach (List<ISettingWidget> widgets in _widgetsByTab.Values)
            {
                foreach (ISettingWidget widget in widgets)
                {
                    widget.Refresh();
                }
            }
            RefreshFooter();
        }

        private void RefreshFooter()
        {
            bool anyDirty = _registry.Tabs.SelectMany(t => t.Settings).Any(s => s.IsDirty);
            _view.SetSaveInteractable(anyDirty);
            _view.SetDiscardInteractable(anyDirty);
        }
    }
}
