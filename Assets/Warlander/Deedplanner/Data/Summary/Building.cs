using System.Collections.Generic;

namespace Warlander.Deedplanner.Data.Summary
{
    public class Building
    {
        private List<Room> rooms;
        
        public IReadOnlyList<Room> Rooms => rooms.AsReadOnly();

        public Building(List<Room> newRooms)
        {
            rooms = newRooms;
        }

        /// <summary>
        /// Returns all tiles without the duplicates (for example, interior walls between rooms)
        /// </summary>
        public IEnumerable<TileSummary> GetAllTiles()
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
    }
}