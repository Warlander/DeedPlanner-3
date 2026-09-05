using UnityEngine;

namespace Warlogic.Settings.Ugui
{
    public sealed class SliderSettingWidgetFactory : ISettingWidgetFactory
    {
        private readonly SliderSettingWidget _prefab;

        public SliderSettingWidgetFactory(SliderSettingWidget prefab)
        {
            _prefab = prefab;
        }

        public ISettingWidget Create(ISetting setting, Transform parent)
        {
            SliderSettingWidget widget = Object.Instantiate(_prefab, parent);
            if (setting is ISetting<float> floatSetting && setting is FloatSetting floatRange)
            {
                widget.Bind(floatSetting, floatRange.Min, floatRange.Max);
            }
            else if (setting is ISetting<int> intSetting && setting is IntSetting intRange)
            {
                widget.Bind(intSetting, intRange.Min, intRange.Max);
            }
            else
            {
                Object.Destroy(widget.gameObject);
                return null;
            }
            return widget;
        }
    }
}
