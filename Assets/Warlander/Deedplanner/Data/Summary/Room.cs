using System.Collections.Generic;

namespace Warlander.Deedplanner.Data.Summary
{
    public class Room
    {
        private List<TileSummary> tiles;

        public Room(List<TileSummary> newTiles)
        {
            tiles = newTiles;
        }
    }
}