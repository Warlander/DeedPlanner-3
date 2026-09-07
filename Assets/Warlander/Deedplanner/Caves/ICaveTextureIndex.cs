using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Caves
{
    public interface ICaveTextureIndex
    {
        int GetIndex(CaveData terrain);
    }
}
