using System.Collections.Generic;

namespace Warlander.Deedplanner.Data.Summary
{
    public class TileSummaryComparer : IEqualityComparer<TileSummary>
    {
        public bool Equals(TileSummary t0, TileSummary t1)
        {
            return t0.X == t1.X && t0.Y == t1.Y;
        }

        public int GetHashCode(TileSummary obj)
        {
            return obj.X * 10000 + obj.Y;
        }
    }
}