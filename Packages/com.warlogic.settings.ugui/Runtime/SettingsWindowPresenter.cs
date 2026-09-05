using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Warlogic.Settings.Ugui
{
    public sealed class SettingsWindowPresenter
    {
        private sealed class TabContent
        {
            public readonly List<ISettingWidget> Widgets = new List<ISettingWidget>();
            public readonly List<GameObject> Roots = new List<GameObject>();
        }

        private readonly SettingsRegistry _registry;
        private readonly SettingsWidgetCatalog _catalog;
        private readonly ISettingsWindowView _view;
        private readonly Dictionary<string, TabContent> _contentByTab = new Dictionary<string, TabContent>();
        private readonly Dictionary<string, SettingsTabButton> _tabButtons = new Dictionary<string, SettingsTabButton>();
        private string _activeTabId;

        public event Action<string> TabContentBuilt;
        public event Action<string> ActiveTabChanged;

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
            return _contentByTab.ContainsKey(tabId);
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
            ActiveTabChanged?.Invoke(tabId);
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
            var content = new TabContent();
            string currentGroup = null;
            foreach (ISetting setting in tab.Settings)
            {
                if (setting is IGroupedSetting grouped && grouped.Group != currentGroup)
                {
                    currentGroup = grouped.Group;
                    SettingsGroupHeader header = _catalog.CreateGroupHeader(currentGroup, _view.ContentRoot);
                    content.Roots.Add(header.gameObject);
                    header.gameObject.SetActive(false);
                }
                ISettingWidget widget = _catalog.CreateWidget(setting, _view.ContentRoot);
                if (widget == null)
                {
                    continue;
                }
                widget.ValueEdited += RefreshFooter;
                widget.Root.gameObject.SetActive(false);
                content.Widgets.Add(widget);
                content.Roots.Add(widget.Root.gameObject);
            }
            _contentByTab[tabId] = content;
            TabContentBuilt?.Invoke(tabId);
        }

        private void SetTabContentVisible(string tabId, bool visible)
        {
            foreach (GameObject root in _contentByTab[tabId].Roots)
            {
                root.SetActive(visible);
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
            foreach (TabContent content in _contentByTab.Values)
            {
                foreach (ISettingWidget widget in content.Widgets)
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
