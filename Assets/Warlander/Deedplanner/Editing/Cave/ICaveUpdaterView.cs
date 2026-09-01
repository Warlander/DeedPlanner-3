using Warlander.Deedplanner.Data.Caves;

namespace Warlander.Deedplanner.Editing
{
    public interface ICaveUpdaterView
    {
        void AddCaveEntry(CaveData data, string[] category);
    }
}
