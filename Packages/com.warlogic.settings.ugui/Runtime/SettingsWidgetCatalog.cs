using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warlogic.Settings.Ugui
{
    public sealed class SettingsWidgetCatalog : MonoBehaviour
    {
        [SerializeField] private SliderSettingWidget sliderRowPrefab;
        [SerializeField] private ToggleSettingWidget toggleRowPrefab;
        [SerializeField] private DropdownSettingWidget dropdownRowPrefab;
        [SerializeField] private SettingsGroupHeader groupHeaderPrefab;
        [SerializeField] private SettingsTabButton tabButtonPrefab;

        private readonly Dictionary<Type, ISettingWidgetFactory> _factories = new Dictionary<Type, ISettingWidgetFactory>();

        public void RegisterFactory(Type settingType, ISettingWidgetFactory factory)
        {
            _factories[settingType] = factory;
        }

        public ISettingWidget CreateWidget(ISetting setting, Transform parent)
        {
            ISettingWidgetFactory factory = ResolveFactory(setting.GetType());
            return factory?.Create(setting, parent);
        }

        public SettingsGroupHeader CreateGroupHeader(string label, Transform parent)
        {
            SettingsGroupHeader header = Instantiate(groupHeaderPrefab, parent);
            header.SetLabel(label);
            return header;
        }

        public SettingsTabButton CreateTabButton(Transform parent)
        {
            return Instantiate(tabButtonPrefab, parent);
        }

        private ISettingWidgetFactory ResolveFactory(Type settingType)
        {
            if (_factories.TryGetValue(settingType, out ISettingWidgetFactory registered))
            {
                return registered;
            }
            if (settingType == typeof(FloatSetting) || settingType == typeof(IntSetting))
            {
                return new SliderSettingWidgetFactory(sliderRowPrefab);
            }
            if (settingType == typeof(BoolSetting))
            {
                return new ToggleSettingWidgetFactory(toggleRowPrefab);
            }
            if (settingType.IsGenericType && settingType.GetGenericTypeDefinition() == typeof(EnumSetting<>))
            {
                Type enumType = settingType.GetGenericArguments()[0];
                return new DropdownSettingWidgetFactory(dropdownRowPrefab, enumType);
            }
            return null;
        }
    }
}
