using System;
using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveMap : ICaveMap
    {
        private readonly int _width;
        private readonly int _height;
        private readonly Func<int, int, CaveCell> _getCell;
        private readonly CaveData _defaultTerrain;

        public CaveMap(int width, int height, Func<int, int, CaveCell> getCell, CaveData defaultTerrain)
        {
            _width = width;
            _height = height;
            _getCell = getCell ?? throw new ArgumentNullException(nameof(getCell));
            _defaultTerrain = defaultTerrain ?? throw new ArgumentNullException(nameof(defaultTerrain));
        }

        public bool IsOpen(int x, int y)
        {
            return !IsSolid(x, y);
        }

        public bool IsSolid(int x, int y)
        {
            return !IsVisibleCell(x, y) || GetRequiredCell(x, y).IsSolid;
        }

        public CaveData GetTerrain(int x, int y)
        {
            return IsVisibleCell(x, y) ? GetRequiredCell(x, y).Terrain : _defaultTerrain;
        }

        public int GetFloorHeight(int x, int y, CaveCorner corner)
        {
            return GetCornerCell(x, y, corner).FloorHeight;
        }

        public int GetClearance(int x, int y, CaveCorner corner)
        {
            return GetCornerCell(x, y, corner).Clearance;
        }

        public int GetCeilingHeight(int x, int y, CaveCorner corner)
        {
            CaveCell cell = GetCornerCell(x, y, corner);
            return cell.FloorHeight + cell.Clearance;
        }

        public bool IsBoundarySolid(int x, int y, CaveEdge edge)
        {
            ValidateVisibleCell(x, y);
            GetNeighborCoordinates(x, y, edge, out int neighborX, out int neighborY);
            return IsSolid(neighborX, neighborY);
        }

        public CaveData GetBoundaryTerrain(int x, int y, CaveEdge edge)
        {
            ValidateVisibleCell(x, y);
            GetNeighborCoordinates(x, y, edge, out int neighborX, out int neighborY);
            return IsSolid(neighborX, neighborY) ? GetTerrain(neighborX, neighborY) : null;
        }

        public bool IsEntrance(int x, int y, CaveEdge edge)
        {
            ValidateVisibleCell(x, y);
            if (IsSolid(x, y))
            {
                return false;
            }

            switch (edge)
            {
                case CaveEdge.South:
                    return GetClearance(x, y, CaveCorner.SouthWest) == 0 &&
                           GetClearance(x, y, CaveCorner.SouthEast) == 0;
                case CaveEdge.East:
                    return GetClearance(x, y, CaveCorner.SouthEast) == 0 &&
                           GetClearance(x, y, CaveCorner.NorthEast) == 0;
                case CaveEdge.North:
                    return GetClearance(x, y, CaveCorner.NorthWest) == 0 &&
                           GetClearance(x, y, CaveCorner.NorthEast) == 0;
                case CaveEdge.West:
                    return GetClearance(x, y, CaveCorner.SouthWest) == 0 &&
                           GetClearance(x, y, CaveCorner.NorthWest) == 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
            }
        }

        private CaveCell GetCornerCell(int x, int y, CaveCorner corner)
        {
            ValidateVisibleCell(x, y);
            switch (corner)
            {
                case CaveCorner.SouthWest:
                    return GetRequiredCell(x, y);
                case CaveCorner.SouthEast:
                    return GetRequiredCell(x + 1, y);
                case CaveCorner.NorthWest:
                    return GetRequiredCell(x, y + 1);
                case CaveCorner.NorthEast:
                    return GetRequiredCell(x + 1, y + 1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(corner), corner, null);
            }
        }

        private void GetNeighborCoordinates(int x, int y, CaveEdge edge, out int neighborX, out int neighborY)
        {
            neighborX = x;
            neighborY = y;
            switch (edge)
            {
                case CaveEdge.South:
                    neighborY--;
                    break;
                case CaveEdge.East:
                    neighborX++;
                    break;
                case CaveEdge.North:
                    neighborY++;
                    break;
                case CaveEdge.West:
                    neighborX--;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
            }
        }

        private CaveCell GetRequiredCell(int x, int y)
        {
            CaveCell cell = _getCell(x, y);
            if (cell == null)
            {
                throw new ArgumentOutOfRangeException(nameof(x), "Cave cell coordinates are outside the map.");
            }

            return cell;
        }

        private bool IsVisibleCell(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _width && y < _height;
        }

        private void ValidateVisibleCell(int x, int y)
        {
            if (!IsVisibleCell(x, y))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "Cave cell coordinates are outside the visible map.");
            }
        }
    }
}
