using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Warlander.Deedplanner.Data.Summary
{
    public class Room
    {
        public HashSet<TileSummary> Tiles { get; }
        
        public Room(HashSet<TileSummary> newTiles)
        {
            Tiles = newTiles;
        }

        public bool BordersRoom(Room room)
        {
            if (this == room)
            {
                // we assume room cannot border itself
                return false;
            }
            
            return Tiles.Intersect(room.Tiles, new TileSummaryComparer()).Any();
        }
        
        public string CreateSummary()
        {
            StringBuilder build = new StringBuilder();
            build.AppendLine("Room summary");
            
            foreach (TileSummary tileSummary in Tiles)
            {
                build.Append('(').Append(tileSummary.X).Append(' ').Append(tileSummary.Y).Append(')');
                build.Append(' ').Append(tileSummary.TilePart).AppendLine();
            }

            return build.ToString();
        }

    }
}