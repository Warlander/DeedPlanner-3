using System;
using Warlander.Deedplanner.Data.Decorations;
using UnityEngine;

namespace Warlander.Deedplanner.Editing
{
    public interface IDecorationUpdaterView
    {
        event Action<DecorationData> DecorationSelected;
        event Action<bool> SnapToGridChanged;
        event Action<bool> RotationSnappingChanged;
        event Action<string> RotationSensitivityChanged;

        void AddDecorationEntry(DecorationData data, string[] category, Sprite sprite);
        void SetSnapToGrid(bool value);
        void SetRotationSnapping(bool value);
        void SetRotationSensitivity(string text);
        void PushSelection();
    }
}
