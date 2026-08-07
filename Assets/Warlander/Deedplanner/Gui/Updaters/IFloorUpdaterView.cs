using System;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Floors;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public interface IFloorUpdaterView
    {
        event Action<FloorData> FloorSelected;
        event Action<EntityOrientation> OrientationChanged;
        void AddFloorEntry(FloorData data, string[] category);
        void PushSelection();
    }
}
