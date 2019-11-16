using System.Collections.Generic;
using System.Text;

namespace Warlander.Deedplanner.Data.Summary
{
    public class Room
    {
        private List<TileSummary> tiles;

        public Room(List<TileSummary> newTiles)
        {
            tiles = newTiles;
        }

        public string CreateSummary()
        {
            StringBuilder build = new StringBuilder();
            build.AppendLine("Room summary");
            
            foreach (TileSummary tileSummary in tiles)
            {
                build.Append('(').Append(tileSummary.X).Append(' ').Append(tileSummary.Y).Append(')');
                build.Append(' ').Append(tileSummary.TilePart).AppendLine();
            }

            return build.ToString();
        }
    }
}