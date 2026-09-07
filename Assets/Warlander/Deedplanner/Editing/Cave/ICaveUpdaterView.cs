using System;
using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Editing
{
    public interface ICaveUpdaterView
    {
        event Action<CaveData> CaveSelected;
        event Action<CaveTool> ToolChanged;
        event Action<bool> PrimaryTargetChanged;
        event Action<bool> CullingChanged;

        void AddCaveEntry(CaveData data, string[] category);
        void SetPrimaryData(CaveData data);
        void SetSecondaryData(CaveData data);
        void SetCullingEnabled(bool enabled);
    }
}
