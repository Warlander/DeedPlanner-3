using System;
using UnityEngine;

namespace Warlogic.Settings.Ugui
{
    public sealed class DropdownSettingWidgetFactory : ISettingWidgetFactory
    {
        private readonly DropdownSettingWidget _prefab;
        private readonly Type _enumType;

        public DropdownSettingWidgetFactory(DropdownSettingWidget prefab, Type enumType)
        {
            _prefab = prefab;
            _enumType = enumType;
        }

        public ISettingWidget Create(ISetting setting, Transform parent)
        {
            DropdownSettingWidget widget = UnityEngine.Object.Instantiate(_prefab, parent);
            widget.BindEnum(setting, _enumType);
            return widget;
        }
    }
}
