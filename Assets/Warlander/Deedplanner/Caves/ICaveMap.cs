using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Caves
{
    public interface ICaveMap
    {
        bool IsOpen(int x, int y);
        bool IsSolid(int x, int y);
        CaveData GetTerrain(int x, int y);
        int GetFloorHeight(int x, int y, CaveCorner corner);
        int GetClearance(int x, int y, CaveCorner corner);
        int GetCeilingHeight(int x, int y, CaveCorner corner);
        bool IsBoundarySolid(int x, int y, CaveEdge edge);
        CaveData GetBoundaryTerrain(int x, int y, CaveEdge edge);
        bool IsEntrance(int x, int y, CaveEdge edge);
    }
}
