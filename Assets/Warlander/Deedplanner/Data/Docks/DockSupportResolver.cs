using UnityEngine;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Walls;

namespace Warlander.Deedplanner.Data.Docks
{
    public enum DockHardBlock
    {
        None, TerrainAboveDeck, Bridge, FloorPresent, DockAtDifferentHeight, OutOfBounds
    }

    public static class DockSupportResolver
    {
        public const int WoodPillarMaxDrop = 120;
        public const int StonePillarMaxDrop = 300;
        public const int MaxLevels = 16;

        // Sides: Up = +Y (N), Down = -Y (S), Left = +X (E), Right = -X (W), matching the panel compass.
        private static readonly int[] SideDx = { 0, 0, 1, -1 };
        private static readonly int[] SideDy = { 1, -1, 0, 0 };
        private static readonly EntityOrientation[] SideOrientation =
        {
            EntityOrientation.Up, EntityOrientation.Down, EntityOrientation.Left, EntityOrientation.Right
        };

        public static DockHardBlock GetHardBlock(Map map, int x, int y, int height)
        {
            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
            {
                return DockHardBlock.OutOfBounds;
            }

            Tile tile = map[x, y];
            if (tile.BridgePart != null)
            {
                return DockHardBlock.Bridge;
            }

            Dock dock = tile.Dock;
            if (dock != null && dock.Height != height)
            {
                return DockHardBlock.DockAtDifferentHeight;
            }

            if (HasAnyFloor(tile))
            {
                return DockHardBlock.FloorPresent;
            }

            if (MaxCornerHeight(map, x, y) > height)
            {
                return DockHardBlock.TerrainAboveDeck;
            }

            return DockHardBlock.None;
        }

        public static DockSupportData ResolveAutoSupport(Map map, int x, int y, int height,
            DockSupportData pillarPreference, out EntityOrientation braceDir)
        {
            if (IsFlatAtDeckLevel(map, x, y, height))
            {
                braceDir = EntityOrientation.Up;
                return null;
            }

            if (IsPillarValid(map, x, y, height, pillarPreference))
            {
                braceDir = EntityOrientation.Up;
                return pillarPreference;
            }

            if (TryPickBraceSide(map, x, y, height, null, out braceDir))
            {
                return Database.DockSupports["dwb"];
            }

            // Nothing fits: paint with the preferred pillar, chunk 6 marks it invalid.
            braceDir = EntityOrientation.Up;
            return pillarPreference;
        }

        public static bool IsFlatAtDeckLevel(Map map, int x, int y, int height)
        {
            return map[x, y].SurfaceHeight == height
                && map[x + 1, y].SurfaceHeight == height
                && map[x, y + 1].SurfaceHeight == height
                && map[x + 1, y + 1].SurfaceHeight == height;
        }

        public static bool IsPillarValid(Map map, int x, int y, int height, DockSupportData support)
        {
            int minCorner = MinCornerHeight(map, x, y);
            int drop = height - minCorner;
            if (drop <= 0)
            {
                return false;
            }

            int maxDrop = support.Type == DockSupportType.WoodPillar ? WoodPillarMaxDrop : StonePillarMaxDrop;
            return drop <= maxDrop;
        }

        public static bool TryPickBraceSide(Map map, int x, int y, int height, Tile preferredNeighbor,
            out EntityOrientation braceDir)
        {
            if (preferredNeighbor != null)
            {
                int preferred = FindSideTowards(x, y, preferredNeighbor);
                if (preferred >= 0 && IsSideLoadBearing(map, x, y, height, preferred))
                {
                    braceDir = SideOrientation[preferred];
                    return true;
                }
            }

            for (int side = 0; side < 4; side++)
            {
                if (IsSideLoadBearing(map, x, y, height, side))
                {
                    braceDir = SideOrientation[side];
                    return true;
                }
            }

            braceDir = EntityOrientation.Up;
            return false;
        }

        private static int FindSideTowards(int x, int y, Tile neighbor)
        {
            for (int side = 0; side < 4; side++)
            {
                if (x + SideDx[side] == neighbor.X && y + SideDy[side] == neighbor.Y)
                {
                    return side;
                }
            }

            return -1;
        }

        private static bool IsSideLoadBearing(Map map, int x, int y, int height, int side)
        {
            int nx = x + SideDx[side];
            int ny = y + SideDy[side];
            if (nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height)
            {
                return false;
            }

            Tile neighbor = map[nx, ny];
            Dock neighborDock = neighbor.Dock;
            if (neighborDock != null && neighborDock.Height == height)
            {
                return true;
            }

            for (int level = 0; level < MaxLevels; level++)
            {
                if (neighbor.GetTileContent(level) is Floor && neighbor.SurfaceHeight + level * 30 == height)
                {
                    return true;
                }
            }

            return HasBorderWallTopAt(map, x, y, height, side);
        }

        private static bool HasBorderWallTopAt(Map map, int x, int y, int height, int side)
        {
            Tile wallTile;
            bool vertical;
            switch (side)
            {
                case 0:
                    wallTile = map[x, y];
                    vertical = true;
                    break;
                case 1:
                    wallTile = map[x, y - 1];
                    vertical = true;
                    break;
                case 2:
                    wallTile = map[x, y];
                    vertical = false;
                    break;
                default:
                    wallTile = map[x - 1, y];
                    vertical = false;
                    break;
            }

            if (wallTile == null)
            {
                return false;
            }

            for (int level = 0; level < MaxLevels; level++)
            {
                Wall wall = vertical ? wallTile.GetVerticalHouseWall(level) : wallTile.GetHorizontalHouseWall(level);
                if (wall != null && wallTile.SurfaceHeight + (level + 1) * 30 == height)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAnyFloor(Tile tile)
        {
            for (int level = 0; level < MaxLevels; level++)
            {
                if (tile.GetTileContent(level) is Floor)
                {
                    return true;
            }
            }

            return false;
        }

        private static int MinCornerHeight(Map map, int x, int y)
        {
            return Mathf.Min(
                map[x, y].SurfaceHeight,
                map[x + 1, y].SurfaceHeight,
                map[x, y + 1].SurfaceHeight,
                map[x + 1, y + 1].SurfaceHeight);
        }

        private static int MaxCornerHeight(Map map, int x, int y)
        {
            return Mathf.Max(
                map[x, y].SurfaceHeight,
                map[x + 1, y].SurfaceHeight,
                map[x, y + 1].SurfaceHeight,
                map[x + 1, y + 1].SurfaceHeight);
        }
    }
}
