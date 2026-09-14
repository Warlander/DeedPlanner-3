using System;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Cameras.Tests
{
    public class CaveWalkSurfaceTests
    {
        [Test]
        public void DeepCaveEyeStaysBelowGround()
        {
            var map = new TestMap();
            map.Open[0, 0] = true;
            var walk = new CaveWalkSurface(map, 4, 4);
            var position = new Vector3(2, 10, 2);
            Assert.That(walk.TryPlace(ref position), Is.True);
            Assert.That(position.y, Is.EqualTo(-3.6f).Within(0.001f));
        }

        [Test]
        public void NearestSearchSkipsLowCave()
        {
            var map = new TestMap();
            map.Open[0, 0] = true;
            map.Clearance[0, 0] = 10;
            map.Open[2, 0] = true;
            var walk = new CaveWalkSurface(map, 4, 4);
            var position = new Vector3(2, 8, 2);
            Assert.That(walk.TryPlace(ref position), Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(10, -3.6f, 2)));
        }

        [Test]
        public void FailedSearchKeepsPositionAndDoesNotScanAgainUntilInvalidated()
        {
            var map = new TestMap();
            var walk = new CaveWalkSurface(map, 4, 4);
            var original = new Vector3(2, 8, 2);
            var position = original;
            Assert.That(walk.TryPlace(ref position), Is.False);
            int firstChecks = map.OpenChecks;
            for (int i = 0; i < 100; i++) Assert.That(walk.TryPlace(ref position), Is.False);
            Assert.That(map.OpenChecks - firstChecks, Is.EqualTo(100));
            Assert.That(position, Is.EqualTo(original));
            map.Open[2, 0] = true;
            Assert.That(walk.TryPlace(ref position), Is.False);
            walk.Invalidate();
            Assert.That(walk.TryPlace(ref position), Is.True);
            Assert.That(position.x, Is.EqualTo(10));
        }

        [TestCase(16, false)]
        [TestCase(17, true)]
        public void RequiresEyeHeightAndCeilingMarginAtEveryCorner(int clearance, bool expected)
        {
            var map = new TestMap();
            map.Open[0, 0] = true;
            map.Clearance[1, 1] = clearance;
            var walk = new CaveWalkSurface(map, 4, 4);
            var position = new Vector3(2, 8, 2);
            Assert.That(walk.TryPlace(ref position), Is.EqualTo(expected));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LargeMovementCannotCrossLowOrSolidCells(bool lowCeiling)
        {
            var map = new TestMap();
            map.Open[0, 0] = true;
            map.Open[1, 0] = lowCeiling;
            map.Open[2, 0] = true;
            if (lowCeiling) map.Clearance[2, 0] = 10;
            var walk = new CaveWalkSurface(map, 4, 4);
            Vector3 position = walk.Move(new Vector3(2, -3.6f, 2), new Vector3(14, 20, 2));
            Assert.That(position.x, Is.LessThanOrEqualTo(3.7f));
            Assert.That(position.y, Is.EqualTo(-3.6f).Within(0.001f));
        }

        [Test]
        public void MovementCannotCutAcrossSolidDiagonalCorner()
        {
            var map = new TestMap();
            map.Open[0, 0] = true;
            map.Open[1, 1] = true;
            var walk = new CaveWalkSurface(map, 4, 4);
            Vector3 position = walk.Move(new Vector3(2, 0, 2), new Vector3(6, 0, 6));
            Assert.That(position.x, Is.LessThan(4));
            Assert.That(position.z, Is.LessThan(4));
        }

        [TestCase(2, 1, 1.9f)]
        [TestCase(1, 2, 1.9f)]
        [TestCase(2, 3, 2.9f)]
        [TestCase(3, 2, 2.9f)]
        public void EyeTracksRenderedTriangleFan(float x, float z, float expectedHeight)
        {
            var map = new TestMap();
            map.Open[0, 0] = true;
            map.Floor[0, 0] = 0;
            map.Floor[1, 0] = 0;
            map.Floor[0, 1] = 0;
            map.Floor[1, 1] = 40;
            var walk = new CaveWalkSurface(map, 4, 4);
            var position = new Vector3(x, 100, z);
            Assert.That(walk.TryPlace(ref position), Is.True);
            Assert.That(position.y, Is.EqualTo(expectedHeight).Within(0.001f));
        }

        [TestCase(-40, true)]
        [TestCase(-30, false)]
        public void RaisedSupportUsesRemainingCeilingClearance(int supportHeight, bool expected)
        {
            var map = new TestMap();
            map.Open[0, 0] = true;
            var walk = new CaveWalkSurface(map, 4, 4, (x, y) => supportHeight);
            var position = new Vector3(2, 8, 2);
            Assert.That(walk.TryPlace(ref position), Is.EqualTo(expected));
            Assert.That(position.y, Is.EqualTo(expected ? supportHeight * 0.1f + 1.4f : 8).Within(0.001f));
        }

        private sealed class TestMap : ICaveMap
        {
            public readonly bool[,] Open = new bool[4, 4];
            public readonly int[,] Floor = new int[5, 5];
            public readonly int[,] Clearance = new int[5, 5];
            public int OpenChecks;

            public TestMap()
            {
                for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                {
                    Floor[x, y] = -50;
                    Clearance[x, y] = 30;
                }
            }

            public bool IsOpen(int x, int y)
            {
                OpenChecks++;
                return Open[x, y];
            }

            public bool IsSolid(int x, int y) => !IsOpen(x, y);
            public int GetFloorHeight(int x, int y, CaveCorner corner) => GetCorner(Floor, x, y, corner);
            public int GetClearance(int x, int y, CaveCorner corner) => GetCorner(Clearance, x, y, corner);
            public int GetCeilingHeight(int x, int y, CaveCorner corner) => GetFloorHeight(x, y, corner) + GetClearance(x, y, corner);
            public CaveData GetTerrain(int x, int y) => throw new NotSupportedException();
            public bool IsBoundarySolid(int x, int y, CaveEdge edge) => throw new NotSupportedException();
            public CaveData GetBoundaryTerrain(int x, int y, CaveEdge edge) => throw new NotSupportedException();
            public bool IsEntrance(int x, int y, CaveEdge edge) => throw new NotSupportedException();

            private static int GetCorner(int[,] values, int x, int y, CaveCorner corner)
            {
                if (corner == CaveCorner.SouthEast || corner == CaveCorner.NorthEast) x++;
                if (corner == CaveCorner.NorthWest || corner == CaveCorner.NorthEast) y++;
                return values[x, y];
            }
        }
    }
}
