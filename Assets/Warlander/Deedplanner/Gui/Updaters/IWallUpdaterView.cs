using System;
using Warlander.Deedplanner.Data.Walls;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public interface IWallUpdaterView
    {
        event Action<WallData> WallSelected;
        event Action<bool> ReverseChanged;
        event Action<bool> AutomaticReverseChanged;

        void AddWallEntry(WallData data, string[] category);
        void SetReverseToggles(bool reverse, bool automaticReverse);
        void PushSelection();
    }
}
