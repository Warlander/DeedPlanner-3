using Warlander.Deedplanner.Domain;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Walls;

namespace Warlander.Deedplanner.Docks
{
    public enum DockHardBlock
    {
        None, TerrainAboveDeck, CaveCeilingBelowDeck, Bridge, FloorPresent, DockAtDifferentHeight, OutOfBounds
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

        public static DockHardBlock GetHardBlock(Map map, int x, int y, int height, DockRealm realm)
        {
            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
            {
                return DockHardBlock.OutOfBounds;
            }

            Tile tile = map[x, y];
            if (tile.GetBridgePart(realm == DockRealm.Cave) != null)
            {
                return DockHardBlock.Bridge;
            }

            Dock dock = tile.GetDock(realm);
            if (dock != null && dock.Height != height)
            {
                return DockHardBlock.DockAtDifferentHeight;
            }

            if (HasTouchingDockAtDifferentHeight(map, x, y, height, realm))
            {
                return DockHardBlock.DockAtDifferentHeight;
            }

            if (HasAnyFloor(tile, realm))
            {
                return DockHardBlock.FloorPresent;
            }

            if (MaxCornerHeight(map, x, y, realm) > height)
            {
                return DockHardBlock.TerrainAboveDeck;
            }

            if (realm == DockRealm.Cave && MinCeilingHeight(map, x, y) < height)
            {
                return DockHardBlock.CaveCeilingBelowDeck;
            }

            return DockHardBlock.None;
        }

        public static DockSupportData ResolveAutoSupport(Map map, int x, int y, int height,
            DockRealm realm, DockSupportData pillarPreference, IDataCatalog dataCatalog,
            out EntityOrientation braceDir)
        {
            if (IsFlatAtDeckLevel(map, x, y, height, realm))
            {
                braceDir = EntityOrientation.Up;
                return null;
            }

            if (IsPillarValid(map, x, y, height, pillarPreference, realm))
            {
                braceDir = EntityOrientation.Up;
                return pillarPreference;
            }

            if (TryPickBraceSide(map, x, y, height, realm, null, out braceDir))
            {
                return dataCatalog.GetDockSupport("dwb");
            }

            // Nothing fits: paint with the preferred pillar, chunk 6 marks it invalid.
            braceDir = EntityOrientation.Up;
            return pillarPreference;
        }

        public static bool IsFlatAtDeckLevel(Map map, int x, int y, int height, DockRealm realm)
        {
            return CornerHeight(map, x, y, realm) == height
                && CornerHeight(map, x + 1, y, realm) == height
                && CornerHeight(map, x, y + 1, realm) == height
                && CornerHeight(map, x + 1, y + 1, realm) == height;
        }

        public static bool IsPillarValid(Map map, int x, int y, int height, DockSupportData support,
            DockRealm realm)
        {
            int minCorner = MinCornerHeight(map, x, y, realm);
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
            return TryPickBraceSide(map, x, y, height, DockRealm.Surface, preferredNeighbor,
                out braceDir);
        }

        public static bool TryPickBraceSide(Map map, int x, int y, int height, DockRealm realm,
            Tile preferredNeighbor, out EntityOrientation braceDir)
        {
            if (preferredNeighbor != null)
            {
                int preferred = FindSideTowards(x, y, preferredNeighbor);
                if (preferred >= 0 && IsSideLoadBearing(map, x, y, height, realm, preferred))
                {
                    braceDir = SideOrientation[preferred];
                    return true;
                }
            }

            for (int side = 0; side < 4; side++)
            {
                if (IsSideLoadBearing(map, x, y, height, realm, side))
                {
                    braceDir = SideOrientation[side];
                    return true;
                }
            }

            braceDir = EntityOrientation.Up;
            return false;
        }

        private static int FindBraceSide(EntityOrientation braceRotation)
        {
            for (int side = 0; side < 4; side++)
            {
                if (SideOrientation[side] == braceRotation)
                {
                    return side;
                }
            }

            return -1;
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

        private static bool IsSideLoadBearing(Map map, int x, int y, int height, DockRealm realm, int side)
        {
            int nx = x + SideDx[side];
            int ny = y + SideDy[side];
            if (nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height)
            {
                return false;
            }

            Tile neighbor = map[nx, ny];
            Dock neighborDock = neighbor.GetDock(realm);
            if (neighborDock != null && neighborDock.Height == height)
            {
                return true;
            }

            int minimumLevel = realm == DockRealm.Cave ? Constants.NegativeLevelLimit : 0;
            int maximumLevel = realm == DockRealm.Cave ? 0 : MaxLevels;
            for (int level = minimumLevel; level < maximumLevel; level++)
            {
                if (neighbor.GetTileContent(level) is Floor &&
                    neighbor.GetAbsoluteHeightForLevelOnTile(level) == height)
                {
                    return true;
                }
            }

            return HasBorderWallTopAt(map, x, y, height, realm, side);
        }

        private static bool HasBorderWallTopAt(Map map, int x, int y, int height, DockRealm realm, int side)
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

            int minimumLevel = realm == DockRealm.Cave ? Constants.NegativeLevelLimit : 0;
            int maximumLevel = realm == DockRealm.Cave ? 0 : MaxLevels;
            for (int level = minimumLevel; level < maximumLevel; level++)
            {
                Wall wall = vertical ? wallTile.GetVerticalHouseWall(level) : wallTile.GetHorizontalHouseWall(level);
                if (wall != null && wallTile.GetAbsoluteHeightForLevel(level) + 30 == height)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAnyFloor(Tile tile, DockRealm realm)
        {
            int minimumLevel = realm == DockRealm.Cave ? Constants.NegativeLevelLimit : 0;
            int maximumLevel = realm == DockRealm.Cave ? 0 : MaxLevels;
            for (int level = minimumLevel; level < maximumLevel; level++)
            {
                if (tile.GetTileContent(level) is Floor)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasTouchingDockAtDifferentHeight(Map map, int x, int y, int height, DockRealm realm)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    Dock dock = map[x + dx, y + dy]?.GetDock(realm);
                    if (dock != null && dock.Height != height)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static List<string> ValidateDock(Map map, Dock dock)
        {
            var errors = new List<string>();
            Tile tile = dock.Tile;
            int height = dock.Height;

            if (MaxCornerHeight(map, tile.X, tile.Y, dock.Realm) > height)
            {
                errors.Add("terrain above deck");
            }

            if (dock.Realm == DockRealm.Cave && MinCeilingHeight(map, tile.X, tile.Y) < height)
            {
                errors.Add("cave ceiling below deck");
            }

            if (HasTouchingDockAtDifferentHeight(map, tile.X, tile.Y, height, dock.Realm))
            {
                errors.Add("touches dock at different height");
            }

            DockSupportData support = dock.Support;
            if (support == null)
            {
                if (!IsFlatAtDeckLevel(map, tile.X, tile.Y, height, dock.Realm))
                {
                    errors.Add("without-support dock requires flat ground at deck level");
                }
            }
            else if (support.Type == DockSupportType.Brace)
            {
                int braceSide = FindBraceSide(dock.BraceRotation);
                if (braceSide < 0 || !IsSideLoadBearing(map, tile.X, tile.Y, height, dock.Realm, braceSide))
                {
                    errors.Add("brace has no support");
                }
            }
            else
            {
                int drop = height - MinCornerHeight(map, tile.X, tile.Y, dock.Realm);
                if (drop <= 0)
                {
                    errors.Add("pillar has no corner below deck");
                }
                else
                {
                    int maxDrop = support.Type == DockSupportType.WoodPillar ? WoodPillarMaxDrop : StonePillarMaxDrop;
                    if (drop > maxDrop)
                    {
                        errors.Add("pillar drop " + drop + " > " + maxDrop);
                    }
                }
            }

            if (!dock.Floor.SupportsDock)
            {
                errors.Add("floor material not dock-capable in Wurm");
            }

            return errors;
        }

        private static int MinCornerHeight(Map map, int x, int y, DockRealm realm)
        {
            return Mathf.Min(
                CornerHeight(map, x, y, realm),
                CornerHeight(map, x + 1, y, realm),
                CornerHeight(map, x, y + 1, realm),
                CornerHeight(map, x + 1, y + 1, realm));
        }

        private static int MaxCornerHeight(Map map, int x, int y, DockRealm realm)
        {
            return Mathf.Max(
                CornerHeight(map, x, y, realm),
                CornerHeight(map, x + 1, y, realm),
                CornerHeight(map, x, y + 1, realm),
                CornerHeight(map, x + 1, y + 1, realm));
        }

        private static int CornerHeight(Map map, int x, int y, DockRealm realm)
        {
            return realm == DockRealm.Cave ? map[x, y].CaveHeight : map[x, y].SurfaceHeight;
        }

        private static int MinCeilingHeight(Map map, int x, int y)
        {
            return Mathf.Min(
                map[x, y].CaveHeight + map[x, y].CaveSize,
                map[x + 1, y].CaveHeight + map[x + 1, y].CaveSize,
                map[x, y + 1].CaveHeight + map[x, y + 1].CaveSize,
                map[x + 1, y + 1].CaveHeight + map[x + 1, y + 1].CaveSize);
        }
    }
}
