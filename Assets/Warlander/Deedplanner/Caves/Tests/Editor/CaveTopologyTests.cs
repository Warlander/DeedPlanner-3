using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Caves;
using static Warlander.Deedplanner.Caves.Tests.CaveTestData;

namespace Warlander.Deedplanner.Caves.Tests
{
    public class CaveTopologyTests
    {
        private CaveData _stone;
        private CaveData _floor;

        [SetUp]
        public void SetUp()
        {
            _stone = CreateTerrain("sw", true);
            _floor = CreateTerrain("sfl", false);
        }

        [Test]
        public void IsolatedOpeningBuildsCompleteInwardShell()
        {
            TestMap map = CreateMap(3, 3);
            map.Open(1, 1, _floor);

            CaveTopology topology = Build(map, 0, 0, 3, 3, 7);

            Assert.That(topology.TriangleCount, Is.EqualTo(16));
            Assert.That(Count(topology, CaveFaceKind.Floor), Is.EqualTo(4));
            Assert.That(Count(topology, CaveFaceKind.Ceiling), Is.EqualTo(4));
            Assert.That(Count(topology, CaveFaceKind.Wall), Is.EqualTo(8));
            Assert.That(topology.GetTriangleNormal(0).y, Is.GreaterThan(0f));
            Assert.That(topology.GetTriangleNormal(4).y, Is.LessThan(0f));
            for (int i = 0; i < topology.TriangleCount; i++)
            {
                Assert.That(topology.GetFace(i).Revision, Is.EqualTo(7));
                Assert.That(topology.GetTriangleNormal(i).sqrMagnitude, Is.EqualTo(1f).Within(0.0001f));
            }
        }

        [Test]
        public void AdjacentOpeningsDoNotBuildInternalWall()
        {
            TestMap map = CreateMap(4, 3);
            map.Open(1, 1, _floor);
            map.Open(2, 1, _floor);

            CaveTopology topology = Build(map, 0, 0, 4, 3, 1);

            Assert.That(topology.TriangleCount, Is.EqualTo(28));
            Assert.That(Count(topology, CaveFaceKind.Wall), Is.EqualTo(12));
        }

        [Test]
        public void ColliderAddsEditableSurfacesForSolidCells()
        {
            TestMap map = CreateMap(2, 2);
            var builder = new CaveTopologyBuilder(map.Map, new TestTextureIndex(), _stone, map.Width, map.Height);

            CaveTopology topology = builder.BuildCollider(0, 0, 2, 2, 3);

            Assert.That(Count(topology, CaveFaceKind.SolidSurface), Is.EqualTo(8));
            CaveFace face = topology.GetFace(0);
            Assert.That(face.Kind, Is.EqualTo(CaveFaceKind.SolidSurface));
            Assert.That(face.OpenCellX, Is.EqualTo(0));
            Assert.That(face.OpenCellY, Is.EqualTo(0));
            Assert.That(topology.GetTriangleNormal(0).y, Is.GreaterThan(0f));
        }

        [Test]
        public void WallMetadataUsesSolidNeighborAndItsTexture()
        {
            TestMap map = CreateMap(3, 3);
            CaveData ore = CreateTerrain("ore", true);
            map.Open(1, 1, _floor);
            map.SetTerrain(2, 1, ore);
            var indices = new TestTextureIndex();
            indices.Set(_stone, 2);
            indices.Set(_floor, 3);
            indices.Set(ore, 9);

            CaveTopology topology = Build(map, 0, 0, 3, 3, 4, indices);
            int eastTriangle = Find(topology, CaveFaceKind.Wall, CaveEdge.East);
            CaveFace east = topology.GetFace(eastTriangle);

            Assert.That(east.SolidOwnerX, Is.EqualTo(2));
            Assert.That(east.SolidOwnerY, Is.EqualTo(1));
            Assert.That(topology.GetTriangleTextureIndex(eastTriangle), Is.EqualTo(9));
            Assert.That(topology.GetTriangleTextureIndex(0), Is.EqualTo(3));
            Assert.That(topology.GetTriangleTextureIndex(4), Is.EqualTo(2));
        }

        [Test]
        public void OpenMapCornerBuildsStoneCapsWithOutsideOwners()
        {
            TestMap map = CreateMap(2, 2);
            map.Open(0, 0, _floor);

            CaveTopology topology = Build(map, 0, 0, 2, 2, 1);

            Assert.That(Count(topology, CaveFaceKind.MapEdgeCap), Is.EqualTo(4));
            CaveFace south = topology.GetFace(Find(topology, CaveFaceKind.MapEdgeCap, CaveEdge.South));
            CaveFace west = topology.GetFace(Find(topology, CaveFaceKind.MapEdgeCap, CaveEdge.West));
            Assert.That(south.SolidOwnerY, Is.EqualTo(-1));
            Assert.That(west.SolidOwnerX, Is.EqualTo(-1));
        }

        [Test]
        public void EachWallFacesIntoOpenCell()
        {
            TestMap map = CreateMap(3, 3);
            map.Open(1, 1, _floor);
            CaveTopology topology = Build(map, 0, 0, 3, 3, 1);

            AssertDirection(topology, CaveEdge.South, Vector3.forward);
            AssertDirection(topology, CaveEdge.East, Vector3.left);
            AssertDirection(topology, CaveEdge.North, Vector3.back);
            AssertDirection(topology, CaveEdge.West, Vector3.right);
        }

        [Test]
        public void CollapsedWallTrianglesAreRemovedIndependently()
        {
            TestMap map = CreateMap(3, 3);
            map.Open(1, 1, _floor);
            map.SetClearance(1, 1, 0);

            CaveTopology topology = Build(map, 0, 0, 3, 3, 1);

            Assert.That(Count(topology, CaveFaceKind.Wall), Is.EqualTo(6));
            Assert.That(Count(topology, CaveFaceKind.Floor), Is.EqualTo(4));
            Assert.That(Count(topology, CaveFaceKind.Ceiling), Is.EqualTo(4));
        }

        [Test]
        public void ZeroClearanceEdgeBuildsEntranceWithoutWallTriangles()
        {
            TestMap map = CreateMap(3, 3);
            map.Open(1, 1, _floor);
            map.SetClearance(1, 1, 0);
            map.SetClearance(2, 1, 0);

            CaveTopology topology = Build(map, 0, 0, 3, 3, 1);

            Assert.That(map.Map.IsEntrance(1, 1, CaveEdge.South), Is.True);
            Assert.That(Count(topology, CaveFaceKind.Wall, CaveEdge.South), Is.Zero);
            Assert.That(Count(topology, CaveFaceKind.Wall), Is.EqualTo(4));
        }

        [Test]
        public void AdjacentOpeningsAcrossChunkSeamRemainOpen()
        {
            TestMap map = CreateMap(32, 2);
            map.Open(15, 0, _floor);
            map.Open(16, 0, _floor);

            CaveTopology west = Build(map, 0, 0, 16, 2, 1);
            CaveTopology east = Build(map, 16, 0, 16, 2, 1);

            Assert.That(Count(west, CaveFaceKind.Wall, CaveEdge.East), Is.Zero);
            Assert.That(Count(east, CaveFaceKind.Wall, CaveEdge.West), Is.Zero);
            Assert.That(west.TriangleCount, Is.EqualTo(14));
            Assert.That(east.TriangleCount, Is.EqualTo(14));
        }

        [Test]
        public void SlopedSurfaceUsesSharedCornerHeights()
        {
            TestMap map = CreateMap(2, 1);
            map.Open(0, 0, _floor);
            map.Open(1, 0, _floor);
            map.SetHeight(1, 0, 25, 40);
            map.SetHeight(1, 1, 35, 50);

            CaveTopology west = Build(map, 0, 0, 1, 1, 1);
            CaveTopology east = Build(map, 1, 0, 1, 1, 1);

            Assert.That(HasVertexAtHeight(west, 2.5f), Is.True);
            Assert.That(HasVertexAtHeight(west, 3.5f), Is.True);
            Assert.That(HasVertexAtHeight(east, 2.5f), Is.True);
            Assert.That(HasVertexAtHeight(east, 3.5f), Is.True);
        }

        private TestMap CreateMap(int width, int height)
        {
            return new TestMap(width, height, _stone);
        }

        private CaveTopology Build(TestMap map, int x, int y, int width, int height, int revision,
            ICaveTextureIndex textureIndex = null)
        {
            textureIndex ??= new TestTextureIndex();
            var builder = new CaveTopologyBuilder(map.Map, textureIndex, _stone, map.Width, map.Height);
            return builder.Build(x, y, width, height, revision);
        }

        private static int Count(CaveTopology topology, CaveFaceKind kind)
        {
            int count = 0;
            for (int i = 0; i < topology.TriangleCount; i++)
            {
                if (topology.GetFace(i).Kind == kind)
                {
                    count++;
                }
            }
            return count;
        }

        private static int Count(CaveTopology topology, CaveFaceKind kind, CaveEdge edge)
        {
            int count = 0;
            for (int i = 0; i < topology.TriangleCount; i++)
            {
                CaveFace face = topology.GetFace(i);
                if (face.Kind == kind && face.Edge == edge)
                {
                    count++;
                }
            }
            return count;
        }

        private static int Find(CaveTopology topology, CaveFaceKind kind, CaveEdge edge)
        {
            for (int i = 0; i < topology.TriangleCount; i++)
            {
                CaveFace face = topology.GetFace(i);
                if (face.Kind == kind && face.Edge == edge)
                {
                    return i;
                }
            }
            Assert.Fail($"No {kind} face for {edge}.");
            return -1;
        }

        private static void AssertDirection(CaveTopology topology, CaveEdge edge, Vector3 direction)
        {
            int triangle = Find(topology, CaveFaceKind.Wall, edge);
            Assert.That(Vector3.Dot(topology.GetTriangleNormal(triangle), direction), Is.GreaterThan(0.999f));
        }

        private static bool HasVertexAtHeight(CaveTopology topology, float height)
        {
            for (int triangle = 0; triangle < topology.TriangleCount; triangle++)
            {
                for (int vertex = 0; vertex < 3; vertex++)
                {
                    if (Mathf.Approximately(topology.GetTriangleVertex(triangle, vertex).y, height))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private sealed class TestTextureIndex : ICaveTextureIndex
        {
            private readonly Dictionary<CaveData, int> _indices = new Dictionary<CaveData, int>();

            public void Set(CaveData terrain, int index)
            {
                _indices[terrain] = index;
            }

            public int GetIndex(CaveData terrain)
            {
                return terrain != null && _indices.TryGetValue(terrain, out int index) ? index : 0;
            }
        }

        private sealed class TestMap
        {
            private readonly CaveCell[,] _cells;

            public int Width { get; }
            public int Height { get; }
            public CaveMap Map { get; }

            public TestMap(int width, int height, CaveData defaultTerrain)
            {
                Width = width;
                Height = height;
                _cells = new CaveCell[width + 1, height + 1];
                for (int x = 0; x <= width; x++)
                {
                    for (int y = 0; y <= height; y++)
                    {
                        _cells[x, y] = new CaveCell(defaultTerrain);
                    }
                }
                Map = new CaveMap(width, height, GetCell, defaultTerrain);
            }

            public void Open(int x, int y, CaveData terrain)
            {
                _cells[x, y].InitializeTerrain(terrain);
            }

            public void SetTerrain(int x, int y, CaveData terrain)
            {
                _cells[x, y].InitializeTerrain(terrain);
            }

            public void SetClearance(int x, int y, int clearance)
            {
                _cells[x, y].InitializeHeights(_cells[x, y].FloorHeight, clearance);
            }

            public void SetHeight(int x, int y, int floorHeight, int clearance)
            {
                _cells[x, y].InitializeHeights(floorHeight, clearance);
            }

            private CaveCell GetCell(int x, int y)
            {
                if (x < 0 || y < 0 || x > Width || y > Height)
                {
                    return null;
                }
                return _cells[x, y];
            }
        }
    }
}
