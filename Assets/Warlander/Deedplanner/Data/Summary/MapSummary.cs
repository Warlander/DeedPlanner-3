using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Warlander.Deedplanner.Data.Summary
{
    public class MapSummary
    {
        private List<Building> buildings = new List<Building>();
        
        public MapSummary(Map map)
        {
            bool[,] tilesChecked = new bool[map.Width + 1, map.Height + 1];

            for (int x = 0; x <= map.Width; x++)
            {
                for (int y = 0; y <= map.Height; y++)
                {
                    ScanTileForBuildings(map, tilesChecked, x, y);
                }
            }
        }

        private void ScanTileForBuildings(Map map, bool[,] tilesChecked, int x, int y)
        {
            if (tilesChecked[x, y])
            {
                return;
            }
            
            HashSet<Tile> checkedTiles = new HashSet<Tile>();
            Stack<Tile> tilesToCheck = new Stack<Tile>();
            tilesToCheck.Push(map[x, y]);
            bool noBuilding = false;

            while (checkedTiles.Count <= 100 && tilesToCheck.Count >= 1)
            {
                Tile checkedTile = tilesToCheck.Pop();
                checkedTiles.Add(checkedTile);
                if (!checkedTile.GetHorizontalWall(0))
                {
                    Tile nearbyTile = map.GetRelativeTile(checkedTile, 0, -1);
                    if (!nearbyTile)
                    {
                        noBuilding = true;
                        break;
                    }
                    if (!tilesChecked[nearbyTile.X, nearbyTile.Y] && !checkedTiles.Contains(nearbyTile) && !tilesToCheck.Contains(nearbyTile))
                    {
                        tilesToCheck.Push(nearbyTile);
                    }
                    else if (tilesChecked[nearbyTile.X, nearbyTile.Y])
                    {
                        noBuilding = true;
                        break;
                    }
                }

                if (!checkedTile.GetVerticalWall(0))
                {
                    Tile nearbyTile = map.GetRelativeTile(checkedTile, -1, 0);
                    if (!nearbyTile)
                    {
                        noBuilding = true;
                        break;
                    }
                    if (!tilesChecked[nearbyTile.X, nearbyTile.Y] && !checkedTiles.Contains(nearbyTile) && !tilesToCheck.Contains(nearbyTile))
                    {
                        tilesToCheck.Push(nearbyTile);
                    }
                    else if (tilesChecked[nearbyTile.X, nearbyTile.Y])
                    {
                        noBuilding = true;
                        break;
                    }
                }

                Tile leftTile = map.GetRelativeTile(checkedTile, 0, 1);
                if (!leftTile)
                {
                    noBuilding = true;
                    break;
                }
                if (!tilesChecked[leftTile.X, leftTile.Y] && !leftTile.GetHorizontalWall(0) && !checkedTiles.Contains(leftTile) && !tilesToCheck.Contains(leftTile))
                {
                    tilesToCheck.Push(leftTile);
                }
                else if (tilesChecked[leftTile.X, leftTile.Y])
                {
                    noBuilding = true;
                    break;
                }
                
                Tile rightTile = map.GetRelativeTile(checkedTile, 1, 0);
                if (!rightTile)
                {
                    noBuilding = true;
                    break;
                }
                if (!tilesChecked[rightTile.X, rightTile.Y] && !rightTile.GetVerticalWall(0) && !checkedTiles.Contains(rightTile) && !tilesToCheck.Contains(rightTile))
                {
                    tilesToCheck.Push(rightTile);
                }
                else if (tilesChecked[rightTile.X, rightTile.Y])
                {
                    noBuilding = true;
                    break;
                }
                
            }

            if (checkedTiles.Count > 100 || noBuilding)
            {
                Debug.Log("Flood fill didn't found a building");
            }
            else
            {
                StringBuilder build = new StringBuilder();
                build.AppendLine("There is a building!");
                foreach (Tile tile in checkedTiles)
                {
                    build.Append('(').Append(tile.X).Append(' ').Append(tile.Y).Append(')').AppendLine();
                }
                Debug.Log(build.ToString());
            }

            foreach (Tile tile in checkedTiles)
            {
                tilesChecked[tile.X, tile.Y] = true;
            }
        }
    }
}