using Warlander.Deedplanner.Persistence;

namespace Warlander.Deedplanner.Gui.Home
{
    public readonly struct HomeScreenCategory
    {
        public readonly SaveBackendId BackendId;
        public readonly string Label;

        public HomeScreenCategory(SaveBackendId backendId, string label)
        {
            BackendId = backendId;
            Label = label;
        }
    }
}
