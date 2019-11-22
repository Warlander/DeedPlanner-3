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

        public bool ContainsTile(TileSummary summary)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].Tiles.Contains(summary))
                {
                    return true;
                }
            }

            return false;
        }
    }
}