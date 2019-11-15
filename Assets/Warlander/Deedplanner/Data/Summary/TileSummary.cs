namespace Warlander.Deedplanner.Data.Summary
{
    public struct TileSummary
    {
        public int X { get; }
        public int Y { get; }
        public TilePart TilePart { get; }

        public TileSummary(int x, int y, TilePart tilePart)
        {
            X = x;
            Y = y;
            TilePart = tilePart;
        }
    }
}