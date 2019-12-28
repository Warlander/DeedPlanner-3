using System.Collections.Generic;

namespace Warlander.Deedplanner.Data.Summary
{
    public class BuildingsSummary
    {
        private HashSet<Building> Buildings { get; } = new HashSet<Building>();
        private HashSet<Room> Rooms { get; } = new HashSet<Room>();

        public BuildingsSummary(Map map, int level)
        {
            bool[,] tilesChecked = new bool[map.Width + 1, map.Height + 1];

            LinkedList<Room> rooms = new LinkedList<Room>();
            
            for (int x = 0; x <= map.Width; x++)
            {
                for (int y = 0; y <= map.Height; y++)
                {
                    Room room = ScanTileForRoom(map, tilesChecked, x, y, level);
                    if (room != null)
                    {
                        rooms.AddLast(room);
                    }
                }
            }
            
            Rooms = new HashSet<Room>(rooms);

            while (rooms.Count > 0)
            {
                List<Room> roomsInBuilding = new List<Room>();

                Room firstRoom = rooms.First.Value;
                roomsInBuilding.Add(firstRoom);
                bool addedRoom = true;

                while (addedRoom)
                {
                    addedRoom = false;
                    
                    foreach (Room roomToCheck in rooms)
                    {
                        if (roomsInBuilding.Contains(roomToCheck))
                        {
                            continue;
                        }
                        
                        foreach (Room buildingRoom in roomsInBuilding)
                        {
                            if (buildingRoom.BordersRoom(roomToCheck))
                            {
                                roomsInBuilding.Add(roomToCheck);
                                addedRoom = true;
                                break;
                            }
                        }
                    }
                }

                foreach (Room room in roomsInBuilding)
                {
                    rooms.Remove(room);
                }
                
                Buildings.Add(new Building(roomsInBuilding));
            }
        }

        private Room ScanTileForRoom(Map map, bool[,] tilesChecked, int x, int y, int level)
        {
            if (tilesChecked[x, y])
            {
                return null;
            }
            
            HashSet<Tile> checkedTiles = new HashSet<Tile>();
            Stack<Tile> tilesToCheck = new Stack<Tile>();
            HashSet<TileSummary> tilesInRoom = new HashSet<TileSummary>();
            checkedTiles.Add(map[x, y]);
            tilesToCheck.Push(map[x, y]);
            bool noRoom = false;

            while (checkedTiles.Count <= 100 && tilesToCheck.Count > 0)
            {
                Tile checkedTile = tilesToCheck.Pop();
                tilesInRoom.Add(new TileSummary(checkedTile.X, checkedTile.Y, TilePart.Everything));
                
                if (!checkedTile.GetHorizontalHouseWall(level))
                {
                    Tile nearbyTile = map.GetRelativeTile(checkedTile, 0, -1);
                    if (!nearbyTile)
                    {
                        noRoom = true;
                        break;
                    }
                    if (!tilesChecked[nearbyTile.X, nearbyTile.Y] && !checkedTiles.Contains(nearbyTile))
                    {
                        checkedTiles.Add(nearbyTile);
                        tilesToCheck.Push(nearbyTile);
                        
                    }
                    else if (tilesChecked[nearbyTile.X, nearbyTile.Y])
                    {
                        noRoom = true;
                        break;
                    }
                }

                if (!checkedTile.GetVerticalHouseWall(level))
                {
                    Tile nearbyTile = map.GetRelativeTile(checkedTile, -1, 0);
                    if (!nearbyTile)
                    {
                        noRoom = true;
                        break;
                    }
                    if (!tilesChecked[nearbyTile.X, nearbyTile.Y] && !checkedTiles.Contains(nearbyTile))
                    {
                        checkedTiles.Add(nearbyTile);
                        tilesToCheck.Push(nearbyTile);
                    }
                    else if (tilesChecked[nearbyTile.X, nearbyTile.Y])
                    {
                        noRoom = true;
                        break;
                    }
                }

                Tile leftTile = map.GetRelativeTile(checkedTile, 0, 1);
                if (!leftTile)
                {
                    noRoom = true;
                    break;
                }
                if (!tilesChecked[leftTile.X, leftTile.Y] && !leftTile.GetHorizontalHouseWall(level) && !checkedTiles.Contains(leftTile))
                {
                    checkedTiles.Add(leftTile);
                    tilesToCheck.Push(leftTile);
                }
                else if (leftTile.GetHorizontalHouseWall(level))
                {
                    tilesInRoom.Add(new TileSummary(leftTile.X, leftTile.Y, TilePart.HorizontalWallOnly));
                }
                else if (tilesChecked[leftTile.X, leftTile.Y])
                {
                    noRoom = true;
                    break;
                }

                Tile rightTile = map.GetRelativeTile(checkedTile, 1, 0);
                if (!rightTile)
                {
                    noRoom = true;
                    break;
                }
                if (!tilesChecked[rightTile.X, rightTile.Y] && !rightTile.GetVerticalHouseWall(level) && !checkedTiles.Contains(rightTile))
                {
                    checkedTiles.Add(rightTile);
                    tilesToCheck.Push(rightTile);
                }
                else if (rightTile.GetVerticalHouseWall(level))
                {
                    tilesInRoom.Add(new TileSummary(rightTile.X, rightTile.Y, TilePart.VerticalWallOnly));
                }
                else if (tilesChecked[rightTile.X, rightTile.Y])
                {
                    noRoom = true;
                    break;
                }
            }

            if (checkedTiles.Count > 100 || tilesToCheck.Count > 0)
            {
                noRoom = true;
            }

            foreach (Tile tile in checkedTiles)
            {
                tilesChecked[tile.X, tile.Y] = true;
            }

            if (noRoom)
            {
                return null;
            }
            
            return new Room(tilesInRoom);
        }

        private bool ContainsTile(TileSummary summary)
        {
            foreach (Building building in Buildings)
            {
                if (building.ContainsTile(summary))
                {
                    return true;
                }
            }

            return false;
        }

        public Building GetBuildingAtTile(Tile tile)
        {
            return GetBuildingAtCoords(tile.X, tile.Y);
        }
        
        public Building GetBuildingAtCoords(int x, int y)
        {
            TileSummary summary = new TileSummary(x, y, TilePart.Everything);
            
            foreach (Building building in Buildings)
            {
                if (building.ContainsTile(summary))
                {
                    return building;
                }
            }
            
            return null;
        }

        public Room GetRoomAtTile(Tile tile)
        {
            TileSummary summary = new TileSummary(tile.X, tile.Y, TilePart.Everything);
            
            foreach (Room room in Rooms)
            {
                if (room.ContainsTile(summary))
                {
                    return room;
                }
            }

            return null;
        }

        public bool ContainsFloor(Tile tile)
        {
            return ContainsTile(new TileSummary(tile.X, tile.Y, TilePart.Everything));
        }

        public bool ContainsVerticalWall(Tile tile)
        {
            return ContainsTile(new TileSummary(tile.X, tile.Y, TilePart.Everything)) || ContainsTile(new TileSummary(tile.X, tile.Y, TilePart.VerticalWallOnly));
        }
        
        public bool ContainsHorizontalWall(Tile tile)
        {
            return ContainsTile(new TileSummary(tile.X, tile.Y, TilePart.Everything)) || ContainsTile(new TileSummary(tile.X, tile.Y, TilePart.HorizontalWallOnly));
        }
    }
}