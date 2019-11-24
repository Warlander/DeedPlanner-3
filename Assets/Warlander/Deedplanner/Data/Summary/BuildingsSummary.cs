using System.Collections.Generic;

namespace Warlander.Deedplanner.Data.Summary
{
    public class BuildingsSummary
    {
        public HashSet<Building> Buildings { get; } = new HashSet<Building>();

        public BuildingsSummary(Map map)
        {
            bool[,] tilesChecked = new bool[map.Width + 1, map.Height + 1];

            LinkedList<Room> rooms = new LinkedList<Room>();
            
            for (int x = 0; x <= map.Width; x++)
            {
                for (int y = 0; y <= map.Height; y++)
                {
                    Room room = ScanTileForRoom(map, tilesChecked, x, y);
                    if (room != null)
                    {
                        rooms.AddLast(room);
                    }
                }
            }

            while (rooms.Count > 0)
            {
                List<Room> roomsInBuilding = new List<Room>();

                Room firstRoom = rooms.First.Value;
                roomsInBuilding.Add(firstRoom);
                foreach (Room roomToCheck in rooms)
                {
                    foreach (Room buildingRoom in roomsInBuilding)
                    {
                        if (buildingRoom.BordersRoom(roomToCheck))
                        {
                            roomsInBuilding.Add(roomToCheck);
                            break;
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

        private Room ScanTileForRoom(Map map, bool[,] tilesChecked, int x, int y)
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
                
                if (!checkedTile.GetHorizontalWall(0))
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

                if (!checkedTile.GetVerticalWall(0))
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
                if (!tilesChecked[leftTile.X, leftTile.Y] && !leftTile.GetHorizontalWall(0) && !checkedTiles.Contains(leftTile))
                {
                    checkedTiles.Add(leftTile);
                    tilesToCheck.Push(leftTile);
                }
                else if (leftTile.GetHorizontalWall(0))
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
                if (!tilesChecked[rightTile.X, rightTile.Y] && !rightTile.GetVerticalWall(0) && !checkedTiles.Contains(rightTile))
                {
                    checkedTiles.Add(rightTile);
                    tilesToCheck.Push(rightTile);
                }
                else if (rightTile.GetVerticalWall(0))
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