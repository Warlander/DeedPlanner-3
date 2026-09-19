using UnityEngine;
using Warlander.Deedplanner.Domain;

namespace Warlander.Deedplanner.Bridges
{
    public static class BridgeSpanValidator
    {
        public static bool Validate(Map map, TileCoords start, TileCoords end, out string error)
        {
            error = string.Empty;
            if (start == null || end == null)
            {
                error = "Select start and end tiles.";
                return false;
            }
            if (map == null)
            {
                error = "No map loaded.";
                return false;
            }
            if ((start.Level >= 0) != (end.Level >= 0))
            {
                error = "Bridge cannot go from surface to cave.";
                return false;
            }

            bool verticalSpan = Mathf.Abs(end.Y - start.Y) > Mathf.Abs(end.X - start.X);
            if (verticalSpan ? start.Y > end.Y : start.X > end.X)
            {
                TileCoords swap = start;
                start = end;
                end = swap;
            }

            int minX = Mathf.Min(start.X, end.X);
            int maxX = Mathf.Max(start.X, end.X);
            int minY = Mathf.Min(start.Y, end.Y);
            int maxY = Mathf.Max(start.Y, end.Y);
            int bridgeLength = Mathf.Max(maxX - minX, maxY - minY) - 1;
            if (bridgeLength < 1)
            {
                error = "Bridge must span at least one tile.";
                return false;
            }
            if (bridgeLength > BridgeDefaults.MaxLength)
            {
                error = $"Bridge cannot be longer than {BridgeDefaults.MaxLength} tiles.";
                return false;
            }
            if (minX < 0 || maxX >= map.Width - 1 || minY < 0 || maxY >= map.Height - 1)
            {
                error = "Too close to the map edge - each end of a bridge needs an anchor tile.";
                return false;
            }

            bool vertical = maxY - minY > maxX - minX;
            bool startOnTerrain = start.Level == 0 || start.Level == -1;
            bool endOnTerrain = end.Level == 0 || end.Level == -1;
            if ((startOnTerrain && !AnchorBorderEven(map, start.Level, minX, maxX, minY, maxY, vertical, true))
                || (endOnTerrain && !AnchorBorderEven(map, end.Level, minX, maxX, minY, maxY, vertical, false)))
            {
                error = "Bridge cannot start or end on uneven ground - all tiles at each end must have equal height.";
                return false;
            }

            int spanMinX = minX;
            int spanMaxX = maxX;
            int spanMinY = minY;
            int spanMaxY = maxY;
            if (vertical)
            {
                spanMinY++;
                spanMaxY--;
            }
            else
            {
                spanMinX++;
                spanMaxX--;
            }
            for (int x = spanMinX; x <= spanMaxX; x++)
            {
                for (int y = spanMinY; y <= spanMaxY; y++)
                {
                    if (map[x, y].GetBridgePart(start.Level < 0) != null)
                    {
                        error = "Bridge would intersect an existing bridge.";
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool AnchorBorderEven(Map map, int level, int minX, int maxX, int minY, int maxY,
            bool vertical, bool startEdge)
        {
            int from = vertical ? minX : minY;
            int to = vertical ? maxX + 1 : maxY + 1;
            int fixedCoord = vertical
                ? startEdge ? minY + 1 : maxY
                : startEdge ? minX + 1 : maxX;
            int? borderHeight = null;
            for (int i = from; i <= to; i++)
            {
                Tile tile = vertical ? map[i, fixedCoord] : map[fixedCoord, i];
                int height = level < 0 ? tile.CaveHeight : tile.SurfaceHeight;
                if (borderHeight.HasValue && height != borderHeight.Value)
                {
                    return false;
                }
                borderHeight = height;
            }
            return true;
        }
    }
}
