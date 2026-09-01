using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Editing
{
    public interface ICaveUpdaterView
    {
        void AddCaveEntry(CaveData data, string[] category);
    }
}
