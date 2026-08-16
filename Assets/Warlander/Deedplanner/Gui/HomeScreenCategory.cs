namespace Warlander.Deedplanner.Gui
{
    public readonly struct HomeScreenCategory
    {
        public readonly string BackendId;
        public readonly string Label;

        public HomeScreenCategory(string backendId, string label)
        {
            BackendId = backendId;
            Label = label;
        }
    }
}
