using UnityEngine;

namespace Warlogic.Settings.Ugui
{
    public sealed class ToggleSettingWidgetFactory : ISettingWidgetFactory
    {
        private readonly ToggleSettingWidget _prefab;

        public ToggleSettingWidgetFactory(ToggleSettingWidget prefab)
        {
            _prefab = prefab;
        }

        public ISettingWidget Create(ISetting setting, Transform parent)
        {
            if (!(setting is ISetting<bool> boolSetting))
            {
                return null;
            }
            ToggleSettingWidget widget = Object.Instantiate(_prefab, parent);
            widget.Bind(boolSetting);
            return widget;
        }
    }
}
