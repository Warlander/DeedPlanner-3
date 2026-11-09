using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Caves;

namespace Warlander.Deedplanner.Editing
{
    public static class LevelTileRaycast
    {
        public static bool TryGetTile(Map map, Ray ray, int level, out Vector2Int tile)
        {
            tile = default;
            var bounds = new Bounds(new Vector3(map.Width * 2f, 0, map.Height * 2f),
                new Vector3(map.Width * 4f, 400000f, map.Height * 4f));
            float entry = 0;
            if (!bounds.Contains(ray.origin) && !bounds.IntersectRay(ray, out entry)) return false;

            float distance = Mathf.Max(0, entry) + 0.0001f;
            Vector3 point = ray.GetPoint(distance);
            int x = Mathf.Clamp(Mathf.FloorToInt(point.x / 4f), 0, map.Width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(point.z / 4f), 0, map.Height - 1);
            int stepX = ray.direction.x >= 0 ? 1 : -1;
            int stepY = ray.direction.z >= 0 ? 1 : -1;
            float deltaX = ray.direction.x == 0 ? float.PositiveInfinity : Mathf.Abs(4f / ray.direction.x);
            float deltaY = ray.direction.z == 0 ? float.PositiveInfinity : Mathf.Abs(4f / ray.direction.z);
            float nextX = ray.direction.x == 0 ? float.PositiveInfinity
                : ((x + (stepX > 0 ? 1 : 0)) * 4f - ray.origin.x) / ray.direction.x;
            float nextY = ray.direction.z == 0 ? float.PositiveInfinity
                : ((y + (stepY > 0 ? 1 : 0)) * 4f - ray.origin.z) / ray.direction.z;

            while (x >= 0 && y >= 0 && x < map.Width && y < map.Height && distance <= 20000f)
            {
                Vector3 sw = Corner(map, x, y, level, CaveCorner.SouthWest);
                Vector3 nw = Corner(map, x, y, level, CaveCorner.NorthWest);
                Vector3 ne = Corner(map, x, y, level, CaveCorner.NorthEast);
                Vector3 se = Corner(map, x, y, level, CaveCorner.SouthEast);
                Vector3 center = (sw + nw + ne + se) * 0.25f;
                bool hit = level < 0 && map.Caves.IsSolid(x, y)
                    ? Intersects(ray, sw, nw, ne) || Intersects(ray, sw, ne, se)
                    : Intersects(ray, sw, nw, center) || Intersects(ray, nw, ne, center)
                        || Intersects(ray, ne, se, center) || Intersects(ray, se, sw, center);
                if (hit)
                {
                    tile = new Vector2Int(x, y);
                    return true;
                }

                if (float.IsPositiveInfinity(nextX) && float.IsPositiveInfinity(nextY)) break;
                if (nextX < nextY)
                {
                    distance = nextX;
                    nextX += deltaX;
                    x += stepX;
                }
                else
                {
                    distance = nextY;
                    nextY += deltaY;
                    y += stepY;
                }
            }
            return false;
        }

        private static Vector3 Corner(Map map, int x, int y, int level, CaveCorner corner)
        {
            int height = level < 0 ? map.Caves.GetFloorHeight(x, y, corner) : 0;
            if (corner == CaveCorner.SouthEast || corner == CaveCorner.NorthEast) x++;
            if (corner == CaveCorner.NorthWest || corner == CaveCorner.NorthEast) y++;
            float worldHeight = level < 0 ? height * 0.1f + CaveLevel.GetWorldHeightOffset(level)
                : map[x, y].SurfaceHeight * 0.1f + level * 3f + 0.01f;
            return new Vector3(x * 4f, worldHeight, y * 4f);
        }

        private static bool Intersects(Ray ray, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 edge1 = b - a;
            Vector3 edge2 = c - a;
            Vector3 cross = Vector3.Cross(ray.direction, edge2);
            float determinant = Vector3.Dot(edge1, cross);
            if (Mathf.Abs(determinant) < 0.000001f) return false;
            Vector3 offset = ray.origin - a;
            float u = Vector3.Dot(offset, cross) / determinant;
            if (u < 0 || u > 1) return false;
            Vector3 q = Vector3.Cross(offset, edge1);
            float v = Vector3.Dot(ray.direction, q) / determinant;
            if (v < 0 || u + v > 1) return false;
            float distance = Vector3.Dot(edge2, q) / determinant;
            return distance >= 0 && distance <= 20000f;
        }
    }
}
