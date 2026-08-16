using Warlander.Deedplanner.Data.Caves;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public interface ICaveUpdaterView
    {
        void AddCaveEntry(CaveData data, string[] category);
    }
}
