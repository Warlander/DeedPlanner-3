using Warlander.Deedplanner.Domain.Entities.Floors;

namespace Warlander.Deedplanner.Docks
{
    public readonly struct DockPaintRequest
    {
        public int X { get; }
        public int Y { get; }
        public int Height { get; }
        public FloorData Floor { get; }
        public bool AutomaticSupport { get; }
        public DockSupportData Support { get; }
        public DockSupportData LastPillarSupport { get; }
        public DockRealm Realm { get; }
        public int AnchorLevel { get; }

        public DockPaintRequest(int x, int y, int height, FloorData floor, bool automaticSupport,
            DockSupportData support, DockSupportData lastPillarSupport, DockRealm realm, int anchorLevel)
        {
            X = x;
            Y = y;
            Height = height;
            Floor = floor;
            AutomaticSupport = automaticSupport;
            Support = support;
            LastPillarSupport = lastPillarSupport;
            Realm = realm;
            AnchorLevel = anchorLevel;
        }
    }
}
