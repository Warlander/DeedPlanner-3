using System;
using Warlander.Deedplanner.Data.Grounds;
using UnityEngine;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public interface IGroundUpdaterView
    {
        event Action<GroundData> GroundSelected;
        event Action<GroundTool> ToolChanged;
        event Action<bool> LeftClickTargetChanged;
        event Action<bool> EditCornersChanged;

        void AddGroundEntry(GroundData data, string[] category, Sprite sprite);
        void SetLeftClickData(GroundData data, Sprite sprite);
        void SetRightClickData(GroundData data, Sprite sprite);
    }
}
