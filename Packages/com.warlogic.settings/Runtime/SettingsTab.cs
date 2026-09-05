using System.Collections.Generic;

namespace Warlogic.Settings
{
    public sealed class SettingsTab
    {
        private readonly SettingsRegistry _registry;
        private readonly List<ISetting> _settings = new List<ISetting>();

        public string Id { get; }
        public string Label { get; }
        public int SortOrder { get; }
        public IReadOnlyList<ISetting> Settings => _settings;

        internal SettingsTab(SettingsRegistry registry, string id, string label, int sortOrder)
        {
            _registry = registry;
            Id = id;
            Label = label;
            SortOrder = sortOrder;
        }

        public void Add(ISetting setting)
        {
            _registry.Register(this, setting);
        }

        internal void AddDirect(ISetting setting)
        {
            _settings.Add(setting);
        }
    }
}
