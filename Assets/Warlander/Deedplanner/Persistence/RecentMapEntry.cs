using System;

namespace Warlander.Deedplanner.Persistence
{
    public class RecentMapEntry
    {
        public MapLocation Location { get; }
        public DateTime LastOpenedUtc { get; }
        public bool HasThumbnail { get; }

        public RecentMapEntry(MapLocation location, DateTime lastOpenedUtc, bool hasThumbnail)
        {
            Location = location;
            LastOpenedUtc = lastOpenedUtc;
            HasThumbnail = hasThumbnail;
        }
    }
}
