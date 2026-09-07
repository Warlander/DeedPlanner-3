using System;
using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Caves
{
    public interface ICaveEditor
    {
        event Action<CaveDirtyRegion> Changed;

        bool SetTerrain(int x, int y, CaveData terrain, CaveOccupiedCellPolicy occupiedCellPolicy);
        bool SetFloorHeightAtVertex(int x, int y, int height);
        bool SetClearanceAtVertex(int x, int y, int clearance);
        ICaveEditStroke BeginTerrainStroke(CaveData terrain, CaveOccupiedCellPolicy occupiedCellPolicy);
        ICaveEditStroke BeginFloorHeightStroke(int height);
        ICaveEditStroke BeginClearanceStroke(int clearance);
    }
}
