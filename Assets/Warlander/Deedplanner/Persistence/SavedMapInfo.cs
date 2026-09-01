using System;

namespace Warlander.Deedplanner.Persistence
{
    /// One save discovered by backend enumeration (SaveCapabilities.List).
    public readonly struct SavedMapInfo
    {
        public readonly MapLocation Location;
        public readonly DateTime WriteTimeUtc;

        public SavedMapInfo(MapLocation location, DateTime writeTimeUtc)
        {
            Location = location;
            WriteTimeUtc = writeTimeUtc;
        }
    }
}
