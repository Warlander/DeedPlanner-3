using System;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Docks;
using Warlander.Deedplanner.Data.Floors;
using UnityEngine;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public interface IFloorUpdaterView
    {
        event Action<FloorData> FloorSelected;
        event Action<EntityOrientation> OrientationChanged;
        event Action<FloorPaintMode> PaintModeChanged;
        event Action<bool, DockSupportData> DockSupportChanged;
        void AddFloorEntry(FloorData data, string[] category, Sprite sprite);
        void AddDockFloorEntry(FloorData data);
        void SetDockSupportSectionVisible(bool visible);
        void PushSelection();
    }
}
