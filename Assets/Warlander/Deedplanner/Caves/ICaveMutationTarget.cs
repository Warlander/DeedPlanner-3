using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Editing;

namespace Warlander.Deedplanner.Caves
{
    public interface ICaveMutationTarget
    {
        bool ContainsCell(int x, int y);
        bool ContainsVertex(int x, int y);
        CaveData GetTerrain(int x, int y);
        int GetFloorHeightAtVertex(int x, int y);
        int GetClearanceAtVertex(int x, int y);
        bool HasCaveContent(int x, int y);
        ICaveContentRemoval CreateCaveContentRemoval(int x, int y);
        void SetTerrain(int x, int y, CaveData terrain);
        void SetFloorHeightAtVertex(int x, int y, int height);
        void SetClearanceAtVertex(int x, int y, int clearance);
        void Record(IReversibleCommand command);
    }
}
