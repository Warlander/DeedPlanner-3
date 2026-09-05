using UnityEngine;

namespace Warlogic.Settings.Ugui
{
    public interface ISettingWidgetFactory
    {
        ISettingWidget Create(ISetting setting, Transform parent);
    }
}
