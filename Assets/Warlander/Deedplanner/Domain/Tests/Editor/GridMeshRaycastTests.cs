using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Editing;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Domain.Tests
{
    public class GridMeshRaycastTests
    {
        [Test]
        public void BroadPhaseDimensionsMatchHandleGeometry()
        {
            var handle = new HeightmapHandle(new Vector2Int(1, 2), 0);
            var fields = BindingFlags.Static | BindingFlags.NonPublic;
            double spacing = (double)typeof(GridMesh).GetField("HandleSpacing", fields).GetRawConstantValue();
            double halfSize = (double)typeof(GridMesh).GetField("HandleHalfSize", fields).GetRawConstantValue();
            Assert.That((double)handle.TransformMatrix.m03, Is.EqualTo(spacing));
            Assert.That((double)handle.TransformMatrix.m23, Is.EqualTo(spacing * 2));
            Assert.That((double)(handle.TransformMatrix.m00 * 0.5f), Is.EqualTo(halfSize));
        }

        [TestCase(1, 1)]
        [TestCase(2, 7)]
        [TestCase(7, 2)]
        [TestCase(9, 13)]
        public void VariedHeightAndDirectionRaysSelectTheSameHandleAsFullScan(int width, int height)
        {
            using var fixture = new GridFixture(width, height);
            var random = new System.Random(1073);
            foreach (HeightmapHandle handle in fixture.Handles)
                handle.Slope = random.Next(-10000, 10001);

            for (int i = 0; i < 300; i++)
            {
                HeightmapHandle target = fixture.Handles[random.Next(width), random.Next(height)];
                Vector3 destination = new Vector3(target.TileCoords.x * 4, target.Slope * 0.1f, target.TileCoords.y * 4);
                Vector3 origin = destination + new Vector3(NextOffset(random), NextOffset(random), NextOffset(random));
                if (i % 3 == 0) origin.x = destination.x;
                if (i % 5 == 0) origin.z = destination.z;
                fixture.AssertMatches(new Ray(origin, destination - origin));
                fixture.AssertMatches(new Ray(origin, origin - destination));
            }
        }

        [Test]
        public void GrazingAndAxisAlignedRaysMatchFullScan()
        {
            using var fixture = new GridFixture(5, 8);
            float[] offsets = { -0.30000004f, -0.3f, -0.29999998f, 0, 0.29999998f, 0.3f, 0.30000004f };
            foreach (float offset in offsets)
            {
                for (int x = 0; x < 5; x++)
                {
                    for (int z = 0; z < 8; z++)
                    {
                        Vector3 center = new Vector3(x * 4, 0, z * 4);
                        fixture.AssertMatches(new Ray(center + new Vector3(offset, 20, 0), Vector3.down));
                        fixture.AssertMatches(new Ray(center + new Vector3(0, 20, offset), Vector3.down));
                        fixture.AssertMatches(new Ray(center + new Vector3(-20, offset, 0), Vector3.right));
                        fixture.AssertMatches(new Ray(center + new Vector3(0, offset, 20), Vector3.back));
                    }
                }
            }
            fixture.AssertMatches(new Ray(new Vector3(-8, 20, -8), Vector3.down));
            fixture.AssertMatches(new Ray(Vector3.zero, Vector3.up));
            fixture.AssertMatches(new Ray(new Vector3(32, 20, 48), Vector3.down));
        }

        [Test]
        public void HeightChangesAreObservedByRepeatedRaycasts()
        {
            using var fixture = new GridFixture(3, 4);
            var ray = new Ray(new Vector3(4, 10, 8), Vector3.down);
            HeightmapHandle handle = fixture.Handles[1, 2];
            Assert.That(fixture.Grid.RaycastHandles(ray), Is.SameAs(handle));

            handle.Slope = 200;
            Assert.That(fixture.Grid.RaycastHandles(ray), Is.Null);
            fixture.AssertMatches(ray);

            handle.Slope = -200;
            Assert.That(fixture.Grid.RaycastHandles(ray), Is.SameAs(handle));
            handle.Slope = int.MaxValue;
            fixture.AssertMatches(ray);
            handle.Slope = int.MinValue;
            fixture.AssertMatches(ray);
            handle.Slope = 0;
            Assert.That(fixture.Grid.RaycastHandles(ray), Is.SameAs(handle));
        }

        [Test]
        public void LargeCoordinatesAndDegenerateRaysMatchFullScan()
        {
            using var fixture = new GridFixture(5, 400);
            fixture.AssertMatches(new Ray(new Vector3(100000016, 0, -100000000), new Vector3(-1, 0, 1.0000001f)));
            fixture.AssertMatches(new Ray(new Vector3(-100000016, 0, 100000000), new Vector3(1, 0, -1.0000001f)));
            fixture.AssertMatches(new Ray(new Vector3(float.MaxValue, 0, 0), Vector3.left));
            fixture.AssertMatches(new Ray(new Vector3(float.PositiveInfinity, 0, 0), Vector3.left));
            fixture.AssertMatches(new Ray(new Vector3(float.NaN, 0, 0), Vector3.left));
            fixture.AssertMatches(new Ray(Vector3.zero, new Vector3(float.NaN, 0, 1)));
            fixture.AssertMatches(new Ray(Vector3.zero, new Vector3(float.PositiveInfinity, 0, 1)));
            fixture.AssertMatches(new Ray(Vector3.zero, new Vector3(float.Epsilon, 0, 0)));
            fixture.AssertMatches(default);
        }

        private static float NextOffset(System.Random random) => (float)(random.NextDouble() * 4000 - 2000);

        private sealed class GridFixture : IDisposable
        {
            private readonly GameObject _gameObject;
            public GridMesh Grid { get; }
            public HeightmapHandle[,] Handles { get; }

            public GridFixture(int width, int height)
            {
                _gameObject = new GameObject("Grid raycast test");
                Grid = _gameObject.AddComponent<GridMesh>();
                Handles = new HeightmapHandle[width, height];
                for (int x = 0; x < width; x++)
                    for (int z = 0; z < height; z++)
                        Handles[x, z] = new HeightmapHandle(new Vector2Int(x, z), 0);
                typeof(GridMesh).GetField("heightmapHandles", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Grid, Handles);
            }

            public void AssertMatches(Ray ray)
            {
                float closestDistance = float.MaxValue;
                HeightmapHandle expected = null;
                foreach (HeightmapHandle handle in Handles)
                {
                    float distance = handle.Raycast(ray);
                    if (distance >= 0 && distance < closestDistance)
                    {
                        closestDistance = distance;
                        expected = handle;
                    }
                }
                Assert.That(Grid.RaycastHandles(ray), Is.SameAs(expected), "Ray: " + ray);
            }

            public void Dispose() => Object.DestroyImmediate(_gameObject);
        }
    }
}
