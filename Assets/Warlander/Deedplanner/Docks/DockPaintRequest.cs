using Warlander.Deedplanner.Domain;
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
        public EntityOrientation BraceDirection { get; }
        public DockRealm Realm { get; }
        public int AnchorLevel { get; }
        public int? PreviousX { get; }
        public int? PreviousY { get; }

        public DockPaintRequest(int x, int y, int height, FloorData floor, bool automaticSupport,
            DockSupportData support, DockSupportData lastPillarSupport, EntityOrientation braceDirection,
            DockRealm realm, int anchorLevel, int? previousX, int? previousY)
        {
            X = x;
            Y = y;
            Height = height;
            Floor = floor;
            AutomaticSupport = automaticSupport;
            Support = support;
            LastPillarSupport = lastPillarSupport;
            BraceDirection = braceDirection;
            Realm = realm;
            AnchorLevel = anchorLevel;
            PreviousX = previousX;
            PreviousY = previousY;
        }
    }
}
