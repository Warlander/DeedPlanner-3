namespace Warlander.Deedplanner.Domain
{
    public class TileCoords
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Level { get; set; }

        public TileCoords(int x, int y, int level)
        {
            X = x;
            Y = y;
            Level = level;
        }
    }
}
