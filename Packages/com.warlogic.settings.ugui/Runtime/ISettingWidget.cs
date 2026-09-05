using System;
using UnityEngine;

namespace Warlogic.Settings.Ugui
{
    public interface ISettingWidget
    {
        RectTransform Root { get; }
        event Action ValueEdited;
        void Refresh();
    }
}
