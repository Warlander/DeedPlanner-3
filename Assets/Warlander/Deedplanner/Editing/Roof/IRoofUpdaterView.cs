using System;
using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Roofs;

namespace Warlander.Deedplanner.Editing
{
    public interface IRoofUpdaterView
    {
        event Action<RoofData> RoofSelected;
        void AddRoofEntry(RoofData data, Sprite sprite);
        void PushSelection();
    }
}
