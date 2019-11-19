using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Warlander.Deedplanner.Data.Summary
{
    public class Room
    {
        private List<TileSummary> tiles;

        public IReadOnlyList<TileSummary> Tiles => tiles.AsReadOnly();
        
        public Room(List<TileSummary> newTiles)
        {
            tiles = newTiles;
        }

        public bool BordersRoom(Room room)
        {
            if (this == room)
            {
                // we assume room cannot border itself
                return false;
            }
            
            return tiles.Intersect(room.tiles, new TileSummaryComparer()).Any();
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