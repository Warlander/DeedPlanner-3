using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Caves;
using static Warlander.Deedplanner.Caves.Tests.CaveTestData;

namespace Warlander.Deedplanner.Caves.Tests
{
    public class CaveHitResolverTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private CaveData _stone;
        private CaveData _floor;
        private CaveHitResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _stone = CreateTerrain("sw", true);
            _floor = CreateTerrain("sfl", false);
            _resolver = new CaveHitResolver();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _objects)
            {
                Object.DestroyImmediate(gameObject);
            }
            _objects.Clear();
        }

        [TestCase(CaveFaceKind.Floor)]
        [TestCase(CaveFaceKind.Ceiling)]
        public void FloorAndCeilingResolveToOpenCell(CaveFaceKind kind)
        {
            CaveChunk chunk = CreateChunk(3, 3, out CaveTopology topology);
            int triangle = Find(topology, kind);

            bool resolved = _resolver.TryResolve(chunk, triangle, out CaveHit hit);

            Assert.That(resolved, Is.True);
            Assert.That(hit.CellX, Is.EqualTo(1));
            Assert.That(hit.CellY, Is.EqualTo(1));
            Assert.That(hit.OpenCellX, Is.EqualTo(1));
            Assert.That(hit.OpenCellY, Is.EqualTo(1));
            Assert.That(hit.Kind, Is.EqualTo(kind));
            Assert.That(hit.HasEdge, Is.False);
            Assert.That(_resolver.IsCurrent(hit), Is.True);
        }

        [Test]
        public void WallResolvesToSolidTextureOwner()
        {
            CaveChunk chunk = CreateChunk(3, 3, out CaveTopology topology);
            int triangle = Find(topology, CaveFaceKind.Wall, CaveEdge.East);

            bool resolved = _resolver.TryResolve(chunk, triangle, out CaveHit hit);

            Assert.That(resolved, Is.True);
            Assert.That(hit.CellX, Is.EqualTo(2));
            Assert.That(hit.CellY, Is.EqualTo(1));
            Assert.That(hit.OpenCellX, Is.EqualTo(1));
            Assert.That(hit.OpenCellY, Is.EqualTo(1));
            Assert.That(hit.Edge, Is.EqualTo(CaveEdge.East));
            Assert.That(hit.HasEdge, Is.True);
        }

        [Test]
        public void SolidSurfaceResolvesToSolidCell()
        {
            CaveChunk chunk = CreateChunk(2, 2, -1, -1, out CaveTopology topology);
            int triangle = Find(topology, CaveFaceKind.SolidSurface);

            bool resolved = _resolver.TryResolve(chunk, triangle, out CaveHit hit);

            Assert.That(resolved, Is.True);
            Assert.That(hit.CellX, Is.EqualTo(0));
            Assert.That(hit.CellY, Is.EqualTo(0));
            Assert.That(hit.Kind, Is.EqualTo(CaveFaceKind.SolidSurface));
            Assert.That(hit.HasEdge, Is.False);
        }

        [Test]
        public void MapEdgeCapIsNotEditable()
        {
            CaveChunk chunk = CreateChunk(2, 2, 0, 0, out CaveTopology topology);
            int triangle = Find(topology, CaveFaceKind.MapEdgeCap, CaveEdge.South);

            bool resolved = _resolver.TryResolve(chunk, triangle, out CaveHit hit);

            Assert.That(resolved, Is.False);
            Assert.That(hit.Chunk, Is.Null);
        }

        [Test]
        public void HitBecomesStaleAfterColliderRecook()
        {
            CaveChunk chunk = CreateChunk(3, 3, out CaveTopology topology, out CaveTopologyBuilder builder);
            int triangle = Find(topology, CaveFaceKind.Floor);
            Assert.That(_resolver.TryResolve(chunk, triangle, out CaveHit hit), Is.True);

            chunk.RebuildCollider(builder);

            Assert.That(_resolver.IsCurrent(hit), Is.False);
        }

        [Test]
        public void LogicalFaceRangeIncludesOnlySelectedFaceTriangles()
        {
            CaveChunk chunk = CreateChunk(3, 3, out CaveTopology topology);
            int floorTriangle = Find(topology, CaveFaceKind.Floor);
            int wallTriangle = Find(topology, CaveFaceKind.Wall, CaveEdge.East);

            Assert.That(chunk.TryGetLogicalFaceTriangleRange(floorTriangle + 2, out int floorFirst,
                out int floorCount), Is.True);
            Assert.That(chunk.TryGetLogicalFaceTriangleRange(wallTriangle + 1, out int wallFirst,
                out int wallCount), Is.True);

            Assert.That(floorFirst, Is.EqualTo(floorTriangle));
            Assert.That(floorCount, Is.EqualTo(4));
            Assert.That(wallFirst, Is.EqualTo(wallTriangle));
            Assert.That(wallCount, Is.EqualTo(2));
        }

        [Test]
        public void InvalidTriangleDoesNotResolve()
        {
            CaveChunk chunk = CreateChunk(3, 3, out _);

            Assert.That(_resolver.TryResolve(chunk, -1, out _), Is.False);
            Assert.That(_resolver.TryResolve(chunk, chunk.ColliderTriangleCount, out _), Is.False);
        }

        private CaveChunk CreateChunk(int width, int height, out CaveTopology topology)
        {
            return CreateChunk(width, height, 1, 1, out topology, out _);
        }

        private CaveChunk CreateChunk(int width, int height, int openX, int openY, out CaveTopology topology)
        {
            return CreateChunk(width, height, openX, openY, out topology, out _);
        }

        private CaveChunk CreateChunk(int width, int height, out CaveTopology topology,
            out CaveTopologyBuilder builder)
        {
            return CreateChunk(width, height, 1, 1, out topology, out builder);
        }

        private CaveChunk CreateChunk(int width, int height, int openX, int openY, out CaveTopology topology,
            out CaveTopologyBuilder builder)
        {
            var map = new TestMap(width, height, _stone);
            if (openX >= 0 && openY >= 0)
            {
                map.Open(openX, openY, _floor);
            }
            builder = new CaveTopologyBuilder(map.Map, new TestTextureIndex(), _stone, width, height);
            topology = builder.BuildCollider(0, 0, width, height, 1);

            var chunkObject = new GameObject("Cave Chunk Test");
            _objects.Add(chunkObject);
            CaveChunk chunk = chunkObject.AddComponent<CaveChunk>();
            chunk.Initialize(0, 0, width, height, null);
            chunk.RebuildCollider(builder);
            return chunk;
        }

        private static int Find(CaveTopology topology, CaveFaceKind kind, CaveEdge? edge = null)
        {
            for (int i = 0; i < topology.TriangleCount; i++)
            {
                CaveFace face = topology.GetFace(i);
                if (face.Kind == kind && (!edge.HasValue || face.Edge == edge.Value))
                {
                    return i;
                }
            }
            Assert.Fail($"No {kind} face found.");
            return -1;
        }

        private sealed class TestTextureIndex : ICaveTextureIndex
        {
            public int GetIndex(CaveData terrain)
            {
                return 0;
            }
        }

        private sealed class TestMap
        {
            private readonly CaveCell[,] _cells;

            public CaveMap Map { get; }

            public TestMap(int width, int height, CaveData defaultTerrain)
            {
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

            private CaveCell GetCell(int x, int y)
            {
                return _cells[x, y];
            }
        }
    }
}
