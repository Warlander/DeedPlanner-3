namespace Warlander.Deedplanner.Logic.Saving
{
    public readonly struct MapLocation
    {
        public readonly string BackendId;
        public readonly string Locator;
        public readonly string DisplayName;

        public MapLocation(string backendId, string locator, string displayName)
        {
            BackendId = backendId;
            Locator = locator;
            DisplayName = displayName;
        }

        public override string ToString() => $"{BackendId}://{Locator}";
    }
}
