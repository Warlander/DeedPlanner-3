using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Warlander.Deedplanner.Data.Summary
{
    public class Building
    {
        private readonly List<Room> rooms;
        private IEnumerable<TileSummary> allTiles;

        public IEnumerable<TileSummary> AllTiles {
            get
            {
                if (allTiles == null)
                {
                    allTiles = GetAllTiles();
                }

                return allTiles;
            }
        }
        
        public Building(List<Room> newRooms)
        {
            rooms = newRooms;
        }

        /// <summary>
        /// Returns all tiles without the duplicates (for example, interior walls between rooms)
        /// </summary>
        private IEnumerable<TileSummary> GetAllTiles()
        {
            foreach (Room room in rooms)
            {
                foreach (TileSummary tileSummary in room.Tiles)
                {
                    if (tileSummary.TilePart == TilePart.Everything)
                    {
                        yield return tileSummary;
                    }
                    else
                    {
                        TileSummary fullTileSummary = new TileSummary(tileSummary.X, tileSummary.Y, TilePart.Everything);
                        bool otherContainsFullTile = false;
                        foreach (Room otherRoom in rooms)
                        {
                            if (otherRoom.ContainsTile(fullTileSummary))
                            {
                                otherContainsFullTile = true;
                                break;
                            }
                        }

                        if (!otherContainsFullTile)
                        {
                            yield return tileSummary;
                        }
                    }
                }
            }
        }

        public bool ContainsTile(TileSummary summary)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].ContainsTile(summary))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetCarpentryRequired()
        {
            int carpentryRequired = 0;
            
            foreach (TileSummary tileSummary in AllTiles)
            {
                if (tileSummary.TilePart != TilePart.Everything)
                {
                    continue;
                }
                
                carpentryRequired++;

                if (!AllTiles.Contains(new TileSummary(tileSummary.X - 1, tileSummary.Y, TilePart.Everything)))
                {
                    carpentryRequired++;
                }
                if (!AllTiles.Contains(new TileSummary(tileSummary.X + 1, tileSummary.Y, TilePart.Everything)))
                {
                    carpentryRequired++;
                }
                if (!AllTiles.Contains(new TileSummary(tileSummary.X, tileSummary.Y - 1, TilePart.Everything)))
                {
                    carpentryRequired++;
                }
                if (!AllTiles.Contains(new TileSummary(tileSummary.X, tileSummary.Y + 1, TilePart.Everything)))
                {
                    carpentryRequired++;
                }
            }
            
            return carpentryRequired;
        }

        public string CreateSummary()
        {
            StringBuilder build = new StringBuilder();
            build.AppendLine("Building summary");
            build.Append("Total rooms: ").Append(rooms.Count).AppendLine();
            foreach (Room room in rooms)
            {
                build.AppendLine(room.CreateSummary());
            }

            return build.ToString();
        }
    }
}