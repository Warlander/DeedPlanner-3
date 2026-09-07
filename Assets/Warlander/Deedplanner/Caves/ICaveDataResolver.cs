using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Caves
{
    public interface ICaveDataResolver
    {
        CaveData Resolve(string shortName);
    }
}
