using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Domain.Entities.Roofs;
using Warlander.Deedplanner.Settings;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Domain.Tests
{
    public class MapCalculationsTests
    {
        [Test]
        public void HeightBoundsRemainIndependentWhenAnotherMapChangesOrIsDestroyed()
        {
            using var first = new MapFixture(1, 2, 10, -10);
            using var second = new MapFixture(2, 1, 100, -100);

            AssertBounds(first.Map, 10, 13, -13, -10);
            AssertBounds(second.Map, 100, 103, -103, -100);

            first.SetHeights(0, 0, -30, 30);
            first.Heights.RecalculateHeights();

            AssertBounds(first.Map, -30, 13, -13, 30);
            AssertBounds(second.Map, 100, 103, -103, -100);

            first.Dispose();
            second.SetHeights(2, 1, 90, -90);
            second.Heights.RecalculateHeights();

            AssertBounds(second.Map, 90, 102, -102, -90);
        }

        [Test]
        public void EachMapProcessesItsOwnPendingRoofsAfterTheOtherMapIsDestroyed()
        {
            PropertyInfo roofTypes = typeof(RoofType).GetProperty(nameof(RoofType.RoofTypes));
            var originalRoofTypes = (RoofType[])roofTypes.GetValue(null);
            roofTypes.SetValue(null, Array.Empty<RoofType>());
            try
            {
                using var first = new MapFixture(3, 3, 0, 5);
                using var second = new MapFixture(5, 5, 0, 5);
                first.FillRoofs();
                second.FillRoofs();
                Roof firstCenter = (Roof)first.Map[1, 1].GetTileContent(0);
                Roof secondCenter = (Roof)second.Map[2, 2].GetTileContent(0);

                first.Map.RecalculateRoofs();
                second.Map.RecalculateRoofs();
                Assert.That(firstCenter.RoofLevel, Is.Zero);
                Assert.That(secondCenter.RoofLevel, Is.Zero);

                first.Tick();
                Assert.That(firstCenter.RoofLevel, Is.EqualTo(1));
                Assert.That(secondCenter.RoofLevel, Is.Zero);

                first.Dispose();
                second.Tick();
                Assert.That(secondCenter.RoofLevel, Is.EqualTo(2));

                second.RemoveRoof(0, 0);
                second.Tick();
                Assert.That(secondCenter.RoofLevel, Is.EqualTo(2));

                second.Map.RecalculateRoofs();
                Assert.That(secondCenter.RoofLevel, Is.EqualTo(2));
                second.Tick();
                Assert.That(secondCenter.RoofLevel, Is.EqualTo(1));
            }
            finally
            {
                roofTypes.SetValue(null, originalRoofTypes);
            }
        }

        private static void AssertBounds(Map map, int surfaceMin, int surfaceMax, int caveMin, int caveMax)
        {
            Assert.That(map.LowestSurfaceHeight, Is.EqualTo(surfaceMin));
            Assert.That(map.HighestSurfaceHeight, Is.EqualTo(surfaceMax));
            Assert.That(map.LowestCaveHeight, Is.EqualTo(caveMin));
            Assert.That(map.HighestCaveHeight, Is.EqualTo(caveMax));
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private sealed class MapFixture : IDisposable
        {
            public Map Map { get; }
            public MapHeightTracker Heights { get; }

            public MapFixture(int width, int height, int surfaceHeight, int caveHeight)
            {
                var root = new GameObject("Map calculation test");
                Map = root.AddComponent<Map>();
                SetField(Map, "_mapRenderSettingsRetriever", new MapRenderSettings());
                SetField(Map, "<Ground>k__BackingField", root.AddComponent<GroundMesh>());
                var grid = new MapTileGrid(width, height);
                SetField(Map, "_tileGrid", grid);
                var database = new Database();
                database.AddCave(new CaveData(null, "Stone", "sw", Array.Empty<string[]>(), true, true, false));
                var groundResolver = new TestGroundResolver();
                for (int x = 0; x <= width; x++)
                {
                    for (int y = 0; y <= height; y++)
                    {
                        grid.SetTile(x, y, new Tile(Map, x, y, null, database, null, groundResolver, null));
                        SetHeights(x, y, surfaceHeight + x + y, caveHeight - x - y);
                    }
                }
                Heights = new MapHeightTracker(Map);
                SetField(Map, "_heightTracker", Heights);
                SetField(Map, "_roofCalculator", new MapRoofCalculator(Map));
            }

            public void SetHeights(int x, int y, int surfaceHeight, int caveHeight)
            {
                SetField(Map[x, y], "surfaceHeight", surfaceHeight);
                Map[x, y].Cave.InitializeHeights(caveHeight, 30);
            }

            public void FillRoofs()
            {
                var data = new RoofData(null, "Test roof", "test", null);
                for (int x = 0; x < Map.Width; x++)
                {
                    for (int y = 0; y < Map.Height; y++)
                    {
                        var root = new GameObject("Roof");
                        root.transform.SetParent(Map.transform);
                        Roof roof = root.AddComponent<Roof>();
                        roof.Initialize(Map[x, y], data);
                        GetEntities(Map[x, y]).Add(new EntityData(0, EntityType.Floorroof), roof);
                    }
                }
            }

            public void RemoveRoof(int x, int y)
            {
                LevelEntity roof = Map[x, y].GetTileContent(0);
                GetEntities(Map[x, y]).Remove(new EntityData(0, EntityType.Floorroof));
                Object.DestroyImmediate(roof.gameObject);
            }

            public void Tick()
            {
                typeof(Map).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(Map, null);
            }

            public void Dispose()
            {
                if (Map)
                {
                    Object.DestroyImmediate(Map.gameObject);
                }
            }

            private static Dictionary<EntityData, LevelEntity> GetEntities(Tile tile)
            {
                return (Dictionary<EntityData, LevelEntity>)typeof(Tile)
                    .GetProperty("Entities", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tile);
            }
        }

        private sealed class TestGroundResolver : IGroundDataResolver
        {
            private readonly GroundData _ground = new GroundData("Grass", "gr", Array.Empty<string[]>(), null, null, false);

            public GroundData Resolve(string shortName) => _ground;
        }
    }
}
