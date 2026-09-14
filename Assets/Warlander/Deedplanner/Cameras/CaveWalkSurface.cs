using System;
using UnityEngine;
using Warlander.Deedplanner.Caves;

namespace Warlander.Deedplanner.Cameras
{
    public sealed class CaveWalkSurface
    {
        public const float EyeHeight = 1.4f;
        private const int MinimumClearance = 17;
        private const float MovementStep = 0.25f;
        private const float BoundaryMargin = 0.3f;
        private readonly ICaveMap _map;
        private readonly int _width;
        private readonly int _height;
        private readonly Func<int, int, int> _supportHeight;
        private bool _searchFailed;

        public CaveWalkSurface(ICaveMap map, int width, int height, Func<int, int, int> supportHeight = null)
        {
            _map = map;
            _width = width;
            _height = height;
            _supportHeight = supportHeight;
        }

        public void Invalidate()
        {
            _searchFailed = false;
        }

        public bool TryPlace(ref Vector3 position)
        {
            if (CanStand(position))
            {
                position.y = SampleFloor(position) + EyeHeight;
                return true;
            }
            if (_searchFailed)
            {
                return false;
            }

            float nearestDistance = float.PositiveInfinity;
            Vector3 nearest = position;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var candidate = new Vector3(x * 4f + 2f, 0, y * 4f + 2f);
                    float distance = (candidate.x - position.x) * (candidate.x - position.x)
                        + (candidate.z - position.z) * (candidate.z - position.z);
                    if (distance < nearestDistance && CanStand(candidate))
                    {
                        nearestDistance = distance;
                        nearest = candidate;
                    }
                }
            }
            if (float.IsPositiveInfinity(nearestDistance))
            {
                _searchFailed = true;
                return false;
            }

            nearest.y = SampleFloor(nearest) + EyeHeight;
            position = nearest;
            return true;
        }

        public Vector3 Move(Vector3 position, Vector3 destination)
        {
            Vector3 movement = destination - position;
            movement.y = 0;
            int steps = Mathf.Max(1, Mathf.CeilToInt(movement.magnitude / MovementStep));
            Vector3 step = movement / steps;
            for (int i = 0; i < steps; i++)
            {
                Vector3 candidate = position + step;
                // Check both sides of a diagonal step so it cannot cut through a blocked corner.
                if (!CanStand(candidate) || !CanStand(new Vector3(candidate.x, 0, position.z))
                    || !CanStand(new Vector3(position.x, 0, candidate.z)))
                {
                    break;
                }
                position = candidate;
            }
            position.y = SampleFloor(position) + EyeHeight;
            return position;
        }

        private bool CanStand(Vector3 position)
        {
            int minX = Mathf.FloorToInt((position.x - BoundaryMargin) / 4f);
            int maxX = Mathf.FloorToInt((position.x + BoundaryMargin) / 4f);
            int minY = Mathf.FloorToInt((position.z - BoundaryMargin) / 4f);
            int maxY = Mathf.FloorToInt((position.z + BoundaryMargin) / 4f);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (!CanStandInCell(x, y))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool CanStandInCell(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height || !_map.IsOpen(x, y))
            {
                return false;
            }
            return HasHeadroom(x, y, CaveCorner.SouthWest)
                && HasHeadroom(x, y, CaveCorner.SouthEast)
                && HasHeadroom(x, y, CaveCorner.NorthWest)
                && HasHeadroom(x, y, CaveCorner.NorthEast);
        }

        private bool HasHeadroom(int x, int y, CaveCorner corner)
        {
            return _map.GetCeilingHeight(x, y, corner) - GetFloor(x, y, corner) >= MinimumClearance;
        }

        private int GetFloor(int x, int y, CaveCorner corner)
        {
            int floor = _map.GetFloorHeight(x, y, corner);
            if (_supportHeight == null)
            {
                return floor;
            }
            int vertexX = x + (corner == CaveCorner.SouthEast || corner == CaveCorner.NorthEast ? 1 : 0);
            int vertexY = y + (corner == CaveCorner.NorthWest || corner == CaveCorner.NorthEast ? 1 : 0);
            return Mathf.Max(floor, _supportHeight(vertexX, vertexY));
        }

        private float SampleFloor(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / 4f);
            int y = Mathf.FloorToInt(position.z / 4f);
            float u = position.x / 4f - x;
            float v = position.z / 4f - y;
            float sw = GetFloor(x, y, CaveCorner.SouthWest);
            float se = GetFloor(x, y, CaveCorner.SouthEast);
            float nw = GetFloor(x, y, CaveCorner.NorthWest);
            float ne = GetFloor(x, y, CaveCorner.NorthEast);
            float center = (sw + se + nw + ne) * 0.25f;
            // Match the four-triangle fan used by the cave shell, not bilinear interpolation.
            if (v <= u && v <= 1 - u)
            {
                return (sw * (1 - u - v) + se * (u - v) + center * 2 * v) * 0.1f;
            }
            if (v >= u && v >= 1 - u)
            {
                return (nw * (v - u) + ne * (u + v - 1) + center * 2 * (1 - v)) * 0.1f;
            }
            if (u <= v && u <= 1 - v)
            {
                return (sw * (1 - u - v) + nw * (v - u) + center * 2 * u) * 0.1f;
            }
            return (se * (u - v) + ne * (u + v - 1) + center * 2 * (1 - u)) * 0.1f;
        }
    }
}
