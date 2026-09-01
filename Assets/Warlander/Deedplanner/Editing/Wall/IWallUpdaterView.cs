using System;
using Warlander.Deedplanner.Domain.Entities.Walls;
using UnityEngine;

namespace Warlander.Deedplanner.Editing
{
    public interface IWallUpdaterView
    {
        event Action<WallData> WallSelected;
        event Action<bool> ReverseChanged;
        event Action<bool> AutomaticReverseChanged;

        void AddWallEntry(WallData data, string[] category, Sprite sprite);
        void SetReverseToggles(bool reverse, bool automaticReverse);
        void PushSelection();
    }
}
