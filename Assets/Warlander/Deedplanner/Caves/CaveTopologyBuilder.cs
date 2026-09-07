using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveTopologyBuilder
    {
        private const float TileSize = 4f;
        private const float HeightScale = 0.1f;
        private const float MinimumTriangleArea = 0.000001f;

        private readonly ICaveMap _map;
        private readonly ICaveTextureIndex _textureIndex;
        private readonly CaveData _defaultTerrain;
        private readonly int _mapWidth;
        private readonly int _mapHeight;

        public CaveTopologyBuilder(ICaveMap map, ICaveTextureIndex textureIndex, CaveData defaultTerrain,
            int mapWidth, int mapHeight)
        {
            _map = map;
            _textureIndex = textureIndex;
            _defaultTerrain = defaultTerrain;
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
        }

        public CaveTopology Build(int minimumX, int minimumY, int width, int height, int revision)
        {
            var topology = new CaveTopology();
            int maximumX = Mathf.Min(minimumX + width, _mapWidth);
            int maximumY = Mathf.Min(minimumY + height, _mapHeight);

            for (int x = minimumX; x < maximumX; x++)
            {
                for (int y = minimumY; y < maximumY; y++)
                {
                    if (!_map.IsOpen(x, y))
                    {
                        continue;
                    }

                    AddCell(topology, x, y, minimumX, minimumY, revision);
                }
            }

            return topology;
        }

        private void AddCell(CaveTopology topology, int x, int y, int originX, int originY, int revision)
        {
            Vector3 floorSouthWest = GetCorner(x, y, CaveCorner.SouthWest, originX, originY, false);
            Vector3 floorSouthEast = GetCorner(x, y, CaveCorner.SouthEast, originX, originY, false);
            Vector3 floorNorthWest = GetCorner(x, y, CaveCorner.NorthWest, originX, originY, false);
            Vector3 floorNorthEast = GetCorner(x, y, CaveCorner.NorthEast, originX, originY, false);
            Vector3 ceilingSouthWest = GetCorner(x, y, CaveCorner.SouthWest, originX, originY, true);
            Vector3 ceilingSouthEast = GetCorner(x, y, CaveCorner.SouthEast, originX, originY, true);
            Vector3 ceilingNorthWest = GetCorner(x, y, CaveCorner.NorthWest, originX, originY, true);
            Vector3 ceilingNorthEast = GetCorner(x, y, CaveCorner.NorthEast, originX, originY, true);

            int floorTexture = _textureIndex.GetIndex(_map.GetTerrain(x, y));
            int ceilingTexture = _textureIndex.GetIndex(_defaultTerrain);
            CaveFace floorFace = new CaveFace(CaveFaceKind.Floor, x, y, revision);
            CaveFace ceilingFace = new CaveFace(CaveFaceKind.Ceiling, x, y, revision);

            AddFan(topology, floorSouthWest, floorSouthEast, floorNorthWest, floorNorthEast,
                floorTexture, floorFace, false);
            AddFan(topology, ceilingSouthWest, ceilingSouthEast, ceilingNorthWest, ceilingNorthEast,
                ceilingTexture, ceilingFace, true);

            AddWall(topology, x, y, CaveEdge.South, floorSouthWest, floorSouthEast,
                ceilingSouthWest, ceilingSouthEast, revision);
            AddWall(topology, x, y, CaveEdge.East, floorSouthEast, floorNorthEast,
                ceilingSouthEast, ceilingNorthEast, revision);
            AddWall(topology, x, y, CaveEdge.North, floorNorthEast, floorNorthWest,
                ceilingNorthEast, ceilingNorthWest, revision);
            AddWall(topology, x, y, CaveEdge.West, floorNorthWest, floorSouthWest,
                ceilingNorthWest, ceilingSouthWest, revision);
        }

        private Vector3 GetCorner(int x, int y, CaveCorner corner, int originX, int originY, bool ceiling)
        {
            int cornerX = x;
            int cornerY = y;
            if (corner == CaveCorner.SouthEast || corner == CaveCorner.NorthEast)
            {
                cornerX++;
            }
            if (corner == CaveCorner.NorthWest || corner == CaveCorner.NorthEast)
            {
                cornerY++;
            }

            int height = ceiling ? _map.GetCeilingHeight(x, y, corner) : _map.GetFloorHeight(x, y, corner);
            return new Vector3((cornerX - originX) * TileSize, height * HeightScale,
                (cornerY - originY) * TileSize);
        }

        private static void AddFan(CaveTopology topology, Vector3 southWest, Vector3 southEast,
            Vector3 northWest, Vector3 northEast, int textureIndex, CaveFace face, bool reverse)
        {
            Vector3 center = (southWest + southEast + northWest + northEast) * 0.25f;
            if (reverse)
            {
                AddTriangle(topology, southWest, center, northWest, new Vector2(0f, 0f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), textureIndex, face);
                AddTriangle(topology, northWest, center, northEast, new Vector2(0f, 1f),
                    new Vector2(0.5f, 0.5f), new Vector2(1f, 1f), textureIndex, face);
                AddTriangle(topology, northEast, center, southEast, new Vector2(1f, 1f),
                    new Vector2(0.5f, 0.5f), new Vector2(1f, 0f), textureIndex, face);
                AddTriangle(topology, southEast, center, southWest, new Vector2(1f, 0f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), textureIndex, face);
                return;
            }

            AddTriangle(topology, southWest, northWest, center, new Vector2(0f, 0f),
                new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), textureIndex, face);
            AddTriangle(topology, northWest, northEast, center, new Vector2(0f, 1f),
                new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), textureIndex, face);
            AddTriangle(topology, northEast, southEast, center, new Vector2(1f, 1f),
                new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), textureIndex, face);
            AddTriangle(topology, southEast, southWest, center, new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), textureIndex, face);
        }

        private void AddWall(CaveTopology topology, int x, int y, CaveEdge edge, Vector3 floorA,
            Vector3 floorB, Vector3 ceilingA, Vector3 ceilingB, int revision)
        {
            if (!_map.IsBoundarySolid(x, y, edge))
            {
                return;
            }

            GetNeighbor(x, y, edge, out int ownerX, out int ownerY);
            bool mapEdge = ownerX < 0 || ownerY < 0 || ownerX >= _mapWidth || ownerY >= _mapHeight;
            CaveFaceKind kind = mapEdge ? CaveFaceKind.MapEdgeCap : CaveFaceKind.Wall;
            CaveFace face = new CaveFace(kind, x, y, ownerX, ownerY, edge, revision);
            int textureIndex = _textureIndex.GetIndex(_map.GetBoundaryTerrain(x, y, edge));

            AddTriangle(topology, floorA, ceilingB, ceilingA, new Vector2(0f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f), textureIndex, face);
            AddTriangle(topology, floorA, floorB, ceilingB, new Vector2(0f, 0f),
                new Vector2(1f, 0f), new Vector2(1f, 1f), textureIndex, face);
        }

        private static void AddTriangle(CaveTopology topology, Vector3 a, Vector3 b, Vector3 c,
            Vector2 uvA, Vector2 uvB, Vector2 uvC, int textureIndex, CaveFace face)
        {
            Vector3 cross = Vector3.Cross(b - a, c - a);
            if (cross.sqrMagnitude <= MinimumTriangleArea)
            {
                return;
            }

            int first = topology.Vertices.Count;
            Vector3 normal = cross.normalized;
            topology.Vertices.Add(a);
            topology.Vertices.Add(b);
            topology.Vertices.Add(c);
            topology.Normals.Add(normal);
            topology.Normals.Add(normal);
            topology.Normals.Add(normal);
            topology.TextureCoordinates.Add(uvA);
            topology.TextureCoordinates.Add(uvB);
            topology.TextureCoordinates.Add(uvC);
            Vector2 slice = new Vector2(textureIndex, 0f);
            topology.TextureIndices.Add(slice);
            topology.TextureIndices.Add(slice);
            topology.TextureIndices.Add(slice);
            topology.Triangles.Add(first);
            topology.Triangles.Add(first + 1);
            topology.Triangles.Add(first + 2);
            topology.Faces.Add(face);
        }

        private static void GetNeighbor(int x, int y, CaveEdge edge, out int neighborX, out int neighborY)
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
            }
        }
    }
}
