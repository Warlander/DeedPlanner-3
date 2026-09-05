using UnityEngine;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Ui.Windows;
using Warlogic.Settings;
using Warlogic.Settings.Ugui;

namespace Warlander.Deedplanner.Ui.Widgets
{
    public sealed class KeybindSettingWidgetFactory : ISettingWidgetFactory
    {
        private readonly KeybindSettingWidget _prefab;
        private readonly IRebindOverlay _overlay;

        public KeybindSettingWidgetFactory(KeybindSettingWidget prefab, IRebindOverlay overlay)
        {
            _prefab = prefab;
            _overlay = overlay;
        }

        public ISettingWidget Create(ISetting setting, Transform parent)
        {
            KeybindSettingWidget widget = Object.Instantiate(_prefab, parent);
            widget.Bind((KeybindSetting) setting, _overlay);
            return widget;
        }
    }
}
