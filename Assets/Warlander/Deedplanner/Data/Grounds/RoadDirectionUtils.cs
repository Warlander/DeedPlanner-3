namespace Warlander.Deedplanner.Data.Grounds
{
    public static class RoadDirectionUtils
    {
        public static bool IsNorth(this RoadDirection value)
        {
            return value == RoadDirection.NE || value == RoadDirection.NW;
        }

        public static bool IsSouth(this RoadDirection value)
        {
            return value == RoadDirection.SE || value == RoadDirection.SW;
        }

        public static bool IsWest(this RoadDirection value)
        {
            return value == RoadDirection.SW || value == RoadDirection.NW;
        }

        public static bool IsEast(this RoadDirection value)
        {
            return value == RoadDirection.SE || value == RoadDirection.NE;
        }

        public static bool IsCenter(this RoadDirection value)
        {
            return value == RoadDirection.Center;
        }
    }
}