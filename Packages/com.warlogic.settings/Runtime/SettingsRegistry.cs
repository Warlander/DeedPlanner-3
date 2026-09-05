using System;
using System.Collections.Generic;

namespace Warlogic.Settings
{
    public sealed class SettingsRegistry
    {
        private readonly List<SettingsTab> _tabs = new List<SettingsTab>();
        private readonly Dictionary<string, ISetting> _settingsByKey = new Dictionary<string, ISetting>();
        private readonly ISettingsStore _store;

        public event Action<ISetting> SettingChanged;

        public SettingsRegistry(ISettingsStore store = null)
        {
            _store = store;
        }

        public IReadOnlyList<SettingsTab> Tabs
        {
            get
            {
                var sorted = new List<SettingsTab>(_tabs);
                sorted.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
                return sorted;
            }
        }

        public SettingsTab AddTab(string id, string label, int sortOrder = 0)
        {
            foreach (SettingsTab tab in _tabs)
            {
                if (tab.Id == id)
                {
                    throw new ArgumentException($"Duplicate tab id: {id}", nameof(id));
                }
            }
            var newTab = new SettingsTab(this, id, label, sortOrder);
            _tabs.Add(newTab);
            return newTab;
        }

        public ISetting GetSetting(string key)
        {
            return _settingsByKey.TryGetValue(key, out ISetting setting) ? setting : null;
        }

        public ISetting<T> GetSetting<T>(string key)
        {
            return _settingsByKey.TryGetValue(key, out ISetting setting) ? (ISetting<T>) setting : null;
        }

        internal void Register(SettingsTab tab, ISetting setting)
        {
            if (setting == null)
            {
                throw new ArgumentNullException(nameof(setting));
            }
            if (_settingsByKey.ContainsKey(setting.Key))
            {
                throw new ArgumentException($"Duplicate setting key: {setting.Key}", nameof(setting));
            }
            if (_store != null && setting is IStoreAttachable attachable)
            {
                attachable.AttachStore(_store);
            }
            _settingsByKey.Add(setting.Key, setting);
            tab.AddDirect(setting);
            setting.Changed += () => SettingChanged?.Invoke(setting);
        }
    }
}
