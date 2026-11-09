using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Settings;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class AreaMaterialsTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void AreaIncludesBothCornersAndExcludesOutsideFloors(bool reversed)
        {
            using var fixture = new AreaFixture();
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    fixture.AddEntity(x, y, 0, EntityType.Floorroof, "Planks", 1);
                }
            }

            Vector2Int first = new Vector2Int(1, 2);
            Vector2Int last = new Vector2Int(3, 4);
            Materials result = fixture.Map.CalculateAreaMaterials(reversed ? last : first, reversed ? first : last, 0);

            Assert.That(Entries(result), Is.EquivalentTo(new[] { "Planks = 9" }));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(-2)]
        [TestCase(-7)]
        public void CurrentFloorExcludesOtherSurfaceAndCaveFloors(int level)
        {
            using var fixture = new AreaFixture();
            foreach (int entityLevel in new[] { 0, 1, -1, -2, -7 })
            {
                fixture.AddEntity(2, 2, entityLevel, EntityType.Floorroof, "Floor " + entityLevel, 1);
            }
            Vector2Int selected = new Vector2Int(2, 2);

            Assert.That(Entries(fixture.Map.CalculateAreaMaterials(selected, selected, level)),
                Is.EquivalentTo(new[] { "Floor " + level + " = 1" }));
            Assert.That(Entries(fixture.Map.CalculateAreaMaterials(selected, selected, null)), Has.Length.EqualTo(5));
        }

        [TestCase(null)]
        [TestCase(0)]
        [TestCase(-2)]
        public void AreaCountsAllFourBoundaryWallsOnceAndExcludesNeighbourContents(int? level)
        {
            using var fixture = new AreaFixture();
            int entityLevel = level ?? 0;
            fixture.AddEntity(2, 2, entityLevel, EntityType.Hwall, "Planks", 1);
            fixture.AddEntity(2, 2, entityLevel, EntityType.Vwall, "Planks", 2);
            fixture.AddEntity(2, 3, entityLevel, EntityType.Hwall, "Planks", 4);
            fixture.AddEntity(3, 2, entityLevel, EntityType.Vwall, "Planks", 8);
            fixture.AddEntity(2, 2, entityLevel, EntityType.Floorroof, "Planks", 16);
            fixture.AddEntity(2, 3, entityLevel, EntityType.Floorroof, "Outside", 100);
            fixture.AddEntity(3, 2, entityLevel, EntityType.Floorroof, "Outside", 100);
            fixture.AddEntity(2, 3, entityLevel, EntityType.Vwall, "Outside", 100);
            fixture.AddEntity(3, 2, entityLevel, EntityType.Hwall, "Outside", 100);
            fixture.AddEntity(3, 3, entityLevel, EntityType.Hwall, "Outside", 100);
            fixture.AddDock(2, 3, entityLevel, "Outside", 100);
            fixture.AddDock(3, 2, entityLevel, "Outside", 100);

            Materials result = fixture.Map.CalculateAreaMaterials(new Vector2Int(2, 2), new Vector2Int(2, 2), level);

            Assert.That(Entries(result), Is.EquivalentTo(new[] { "Planks = 31" }));
        }

        [Test]
        public void AreaAtMapEdgeIncludesOuterBoundaryWalls()
        {
            using var fixture = new AreaFixture();
            fixture.AddEntity(5, 6, 0, EntityType.Hwall, "Planks", 3);
            fixture.AddEntity(6, 5, 0, EntityType.Vwall, "Planks", 4);

            Materials result = fixture.Map.CalculateAreaMaterials(new Vector2Int(5, 5), new Vector2Int(5, 5), 0);

            Assert.That(Entries(result), Is.EquivalentTo(new[] { "Planks = 7" }));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(-1)]
        [TestCase(-3)]
        public void CurrentFloorIncludesOnlyDockAnchoredOnThatFloor(int level)
        {
            using var fixture = new AreaFixture();
            fixture.AddDock(2, 2, level, "Dock", 7);
            Vector2Int selected = new Vector2Int(2, 2);

            Assert.That(Entries(fixture.Map.CalculateAreaMaterials(selected, selected, level)),
                Is.EquivalentTo(new[] { "Dock = 7" }));
            Assert.That(Entries(fixture.Map.CalculateAreaMaterials(selected, selected, level == 0 ? 1 : 0)), Is.Empty);
            Assert.That(Entries(fixture.Map.CalculateAreaMaterials(selected, selected, null)),
                Is.EquivalentTo(new[] { "Dock = 7" }));
        }

        [Test]
        public void CaveTerrainCostBelongsToCaveGroundFloorAndAllFloors()
        {
            using var fixture = new AreaFixture();
            var materials = new Materials();
            materials.Add("Mortar", 12);
            fixture.Map[2, 2].Cave.InitializeTerrain(new CaveData(null, "Reinforced", "reinforced",
                Array.Empty<string[]>(), false, true, false, materials));
            Vector2Int selected = new Vector2Int(2, 2);

            Assert.That(Entries(fixture.Map.CalculateAreaMaterials(selected, selected, -1)),
                Is.EquivalentTo(new[] { "Mortar = 12" }));
            Assert.That(Entries(fixture.Map.CalculateAreaMaterials(selected, selected, null)),
                Is.EquivalentTo(new[] { "Mortar = 12" }));
            Assert.That(Entries(fixture.Map.CalculateAreaMaterials(selected, selected, -2)), Is.Empty);
            Assert.That(Entries(fixture.Map.CalculateAreaMaterials(selected, selected, 0)), Is.Empty);
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(-1)]
        [TestCase(-2)]
        [TestCase(-7)]
        public void RayPicksCurrentFloorInsteadOfBaseHeight(int level)
        {
            using var fixture = new AreaFixture();
            float height = level < 0 ? -4f + (-level - 1) * 3f : level * 3f;
            Vector3 target = new Vector3(10, height, 10);
            var ray = new Ray(target + new Vector3(-6, 6, 0), new Vector3(1, -1, 0));

            Assert.That(LevelTileRaycast.TryGetTile(fixture.Map, ray, level, out Vector2Int tile), Is.True);
            Assert.That(tile, Is.EqualTo(new Vector2Int(2, 2)));
        }

        [TestCase(0, 0, 0, 0)]
        [TestCase(0.001f, 0.001f, 0, 0)]
        [TestCase(10, 10, 2, 2)]
        [TestCase(23.999f, 23.999f, 5, 5)]
        public void VerticalRayOriginInsideBoundsPicksAtMapEdges(float x, float z, int expectedX, int expectedY)
        {
            using var fixture = new AreaFixture();

            Assert.That(LevelTileRaycast.TryGetTile(fixture.Map,
                new Ray(new Vector3(x, 9999.7f, z), Vector3.down), 0, out Vector2Int tile), Is.True);
            Assert.That(tile, Is.EqualTo(new Vector2Int(expectedX, expectedY)));
        }

        [TestCase(-1)]
        [TestCase(-7)]
        public void SolidCavePickingUsesRockSurfaceDiagonal(int level)
        {
            using var fixture = new AreaFixture();
            fixture.Map[1, 1].Cave.InitializeHeights(0, 30);
            float offset = CaveLevel.GetWorldHeightOffset(level);
            var ray = new Ray(new Vector3(0.95f, -3.65f + offset, 0.05f), new Vector3(1, -0.1f, 0));

            Assert.That(LevelTileRaycast.TryGetTile(fixture.Map, ray, level, out Vector2Int tile), Is.True);
            Assert.That(tile, Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void RayFollowsSlopedMeshFromEveryHorizontalDirection(
            [Values(0, 2, -1, -3)] int level,
            [Values(-1, 1)] int directionX,
            [Values(-1, 1)] int directionY)
        {
            using var fixture = new AreaFixture();
            fixture.SetSlope();
            Vector3 target = new Vector3(10, fixture.Map.GetInterpolatedHeightForLevel(10, 10, level), 10);
            Vector3 direction = new Vector3(directionX, -2, directionY);
            var ray = new Ray(target - direction * 6, direction);

            Assert.That(LevelTileRaycast.TryGetTile(fixture.Map, ray, level, out Vector2Int tile), Is.True);
            Assert.That(tile, Is.EqualTo(new Vector2Int(2, 2)));
        }

        [TestCase(-1, 10, 10, 0, -1, 0)]
        [TestCase(25, 10, 10, 0, -1, 0)]
        [TestCase(10, 10, -1, 0, -1, 0)]
        [TestCase(10, 10, 25, 0, -1, 0)]
        [TestCase(10, 10, 10, 0, 1, 0)]
        [TestCase(10, 10, 10, 1, 0, 0)]
        public void RayMissesOutsideMapOrAwayFromLevel(float x, float y, float z, float dx, float dy, float dz)
        {
            using var fixture = new AreaFixture();

            Assert.That(LevelTileRaycast.TryGetTile(fixture.Map,
                new Ray(new Vector3(x, y, z), new Vector3(dx, dy, dz)), 0, out _), Is.False);
        }

        private static string[] Entries(Materials materials) =>
            materials.ToString().Replace("\r", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1).Where(line => line != "None").ToArray();

        private sealed class AreaFixture : IDisposable
        {
            private readonly GameObject _root;
            public Map Map { get; }

            public AreaFixture()
            {
                _root = new GameObject("Area materials map");
                Map = _root.AddComponent<Map>();
                SetField(Map, "_mapRenderSettingsRetriever", new MapRenderSettings());
                typeof(Map).GetProperty(nameof(Map.Ground)).SetValue(Map, _root.AddComponent<GroundMesh>());
                var grid = new MapTileGrid(6, 6);
                SetField(Map, "_tileGrid", grid);
                var data = new Database();
                var cave = new CaveData(null, "Rock", "sw", Array.Empty<string[]>(), true, true, false);
                data.AddCave(cave);
                for (int x = 0; x <= 6; x++)
                {
                    for (int y = 0; y <= 6; y++)
                    {
                        var tile = new Tile(Map, x, y, null, data, null, new GroundResolver(), null);
                        tile.Cave.InitializeHeights(-40, 30);
                        grid.SetTile(x, y, tile);
                    }
                }
                typeof(Map).GetProperty(nameof(Map.Caves)).SetValue(Map,
                    new CaveMap(6, 6, (x, y) => Map[x, y]?.Cave, cave));
            }

            public void AddEntity(int x, int y, int level, EntityType type, string name, int count)
            {
                var root = new GameObject("Area material entity");
                root.transform.SetParent(_root.transform);
                var entity = root.AddComponent<Floor>();
                typeof(Floor).GetProperty(nameof(Floor.Data)).SetValue(entity, CreateFloorData(name, count));
                entity.Tile = Map[x, y];
                var entities = (Dictionary<EntityData, LevelEntity>)typeof(Tile)
                    .GetProperty("Entities", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(entity.Tile);
                entities.Add(new EntityData(level, type), entity);
            }

            public void AddDock(int x, int y, int level, string name, int count)
            {
                var root = new GameObject("Area material dock");
                root.transform.SetParent(_root.transform);
                var dock = root.AddComponent<Dock>();
                typeof(Dock).GetProperty(nameof(Dock.AnchorLevel)).SetValue(dock, level);
                typeof(Dock).GetProperty(nameof(Dock.Floor)).SetValue(dock, CreateFloorData(name, count));
                dock.Tile = Map[x, y];
                SetField(Map[x, y], level < 0 ? "caveDock" : "surfaceDock", dock);
            }

            public void SetSlope()
            {
                for (int x = 0; x <= 6; x++)
                {
                    for (int y = 0; y <= 6; y++)
                    {
                        SetField(Map[x, y], "surfaceHeight", x * 10 + y * 5);
                        Map[x, y].Cave.InitializeHeights(-40 + x * 10 + y * 5, 30);
                    }
                }
            }

            public void Dispose() => Object.DestroyImmediate(_root);

            private static FloorData CreateFloorData(string name, int count)
            {
                var materials = new Materials();
                materials.Add(name, count);
                return new FloorData(null, name, name, Array.Empty<string[]>(), false, false, materials);
            }

            private static void SetField(object target, string name, object value) =>
                target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private sealed class GroundResolver : IGroundDataResolver
        {
            private readonly GroundData _ground = new GroundData("Grass", "gr", Array.Empty<string[]>(), null, null, false);
            public GroundData Resolve(string shortName) => _ground;
        }
    }
}
