using System.Collections.Generic;

namespace Warlander.Deedplanner.Data.Summary
{
    public class RoomBorderingComparer : IEqualityComparer<TileSummary>
    {
        public bool Equals(TileSummary t0, TileSummary t1)
        {
            // tile part check to ensure diagonally bordering rooms won't be included in some cases
            return t0.X == t1.X && t0.Y == t1.Y && (t0.TilePart == TilePart.Everything || t1.TilePart == TilePart.Everything);
        }

        public int GetHashCode(TileSummary obj)
        {
            return obj.X * 10000 + obj.Y;
        }
    }
}