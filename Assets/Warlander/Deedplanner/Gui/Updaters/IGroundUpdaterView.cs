using System;
using Warlander.Deedplanner.Data.Grounds;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public interface IGroundUpdaterView
    {
        event Action<GroundData> GroundSelected;
        event Action<GroundTool> ToolChanged;
        event Action<bool> LeftClickTargetChanged;
        event Action<bool> EditCornersChanged;

        void AddGroundEntry(GroundData data, string[] category);
        void SetLeftClickData(GroundData data);
        void SetRightClickData(GroundData data);
    }
}
