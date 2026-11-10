using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Domain.Entities.Walls;
using Warlander.Deedplanner.Settings;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Domain.Summary.Tests
{
    public class BuildingsSummaryTests
    {
        [Test]
        public void WallOnlyTilesDoNotBecomeBuildingInteriors()
        {
            using var fixture = new MapFixture(4, 4);
            fixture.Enclose(1, 1, 1, 1);
            var summary = new BuildingsSummary(fixture.Map, 0);

            Assert.That(summary.BuildingsCount, Is.EqualTo(1));
            Assert.That(summary.RoomsCount, Is.EqualTo(1));
            AssertMembership(summary, fixture.Map[1, 1], true, true, true);
            AssertMembership(summary, fixture.Map[1, 2], false, true, false);
            AssertMembership(summary, fixture.Map[2, 1], false, false, true);
            AssertMembership(summary, fixture.Map[2, 2], false, false, false);
            Assert.That(summary.GetBuildingAtTile(fixture.Map[1, 2]), Is.Null);
            Assert.That(summary.GetRoomAtTile(fixture.Map[2, 1]), Is.Null);
        }

        [Test]
        public void AdjacentRoomsShareBuildingAndRetainInteriorAndWallMembership()
        {
            using var fixture = new MapFixture(5, 4);
            fixture.Enclose(1, 1, 1, 1);
            fixture.Enclose(2, 1, 1, 1);
            var summary = new BuildingsSummary(fixture.Map, 0);

            Assert.That(summary.BuildingsCount, Is.EqualTo(1));
            Assert.That(summary.RoomsCount, Is.EqualTo(2));
            Assert.That(summary.GetBuildingAtCoords(1, 1), Is.SameAs(summary.GetBuildingAtCoords(2, 1)));
            Assert.That(summary.GetRoomAtTile(fixture.Map[1, 1]), Is.Not.SameAs(summary.GetRoomAtTile(fixture.Map[2, 1])));
            AssertMembership(summary, fixture.Map[2, 1], true, true, true);
            AssertMembership(summary, fixture.Map[3, 1], false, false, true);
        }

        [Test]
        public void DiagonalRoomsKeepDistinctBuildingIdentities()
        {
            using var fixture = new MapFixture(5, 5);
            fixture.Enclose(1, 1, 1, 1);
            fixture.Enclose(2, 2, 1, 1);
            var summary = new BuildingsSummary(fixture.Map, 0);

            Assert.That(summary.BuildingsCount, Is.EqualTo(2));
            Assert.That(summary.GetBuildingAtCoords(1, 1), Is.Not.Null);
            Assert.That(summary.GetBuildingAtCoords(2, 2), Is.Not.Null);
            Assert.That(summary.GetBuildingAtCoords(1, 1), Is.Not.SameAs(summary.GetBuildingAtCoords(2, 2)));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void BoundaryWallsAndOutsideCoordinatesRetainTheirMembership(int level)
        {
            using var fixture = new MapFixture(4, 3);
            fixture.Enclose(3, 2, 1, 1, level);
            var summary = new BuildingsSummary(fixture.Map, level);

            AssertMembership(summary, fixture.Map[3, 2], true, true, true);
            AssertMembership(summary, fixture.Map[3, 3], false, true, false);
            AssertMembership(summary, fixture.Map[4, 2], false, false, true);
            Assert.That(summary.GetBuildingAtCoords(-1, 2), Is.Null);
            Assert.That(summary.GetBuildingAtCoords(3, -1), Is.Null);
            Assert.That(summary.GetBuildingAtCoords(5, 2), Is.Null);
            Assert.That(summary.GetBuildingAtCoords(3, 4), Is.Null);
            Assert.That(new BuildingsSummary(fixture.Map, level == 0 ? -1 : 0).BuildingsCount, Is.Zero);
        }

        [Test]
        public void FreshSummaryReflectsWallRemovalAndRestoration()
        {
            using var fixture = new MapFixture(4, 4);
            fixture.Enclose(1, 1, 1, 1);
            Assert.That(new BuildingsSummary(fixture.Map, 0).ContainsFloor(fixture.Map[1, 1]), Is.True);

            fixture.RemoveWall(1, 1, EntityType.Vwall, 0);
            Assert.That(new BuildingsSummary(fixture.Map, 0).ContainsFloor(fixture.Map[1, 1]), Is.False);

            fixture.Enclose(1, 1, 1, 1);
            Assert.That(new BuildingsSummary(fixture.Map, 0).ContainsFloor(fixture.Map[1, 1]), Is.True);
        }

        [TestCase(10, 1)]
        [TestCase(11, 0)]
        public void RoomSizeLimitRemainsOneHundredTiles(int width, int expectedRooms)
        {
            using var fixture = new MapFixture(width + 2, 12);
            fixture.Enclose(1, 1, width, 10);
            var summary = new BuildingsSummary(fixture.Map, 0);

            Assert.That(summary.RoomsCount, Is.EqualTo(expectedRooms));
            Assert.That(summary.ContainsFloor(fixture.Map[1, 1]), Is.EqualTo(expectedRooms > 0));
        }

        private static void AssertMembership(BuildingsSummary summary, Tile tile, bool floor, bool horizontal, bool vertical)
        {
            Assert.That(summary.ContainsFloor(tile), Is.EqualTo(floor), $"Floor ({tile.X}, {tile.Y})");
            Assert.That(summary.ContainsHorizontalWall(tile), Is.EqualTo(horizontal), $"Horizontal ({tile.X}, {tile.Y})");
            Assert.That(summary.ContainsVerticalWall(tile), Is.EqualTo(vertical), $"Vertical ({tile.X}, {tile.Y})");
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private sealed class MapFixture : IDisposable
        {
            private readonly WallData _wall = new WallData(null, null, "Wall", "wall", Array.Empty<string[]>(),
                Color.white, 1, true, false, false, null, null);
            public Map Map { get; }

            public MapFixture(int width, int height)
            {
                var root = new GameObject("Building summary test");
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
                    }
                }
            }

            public void Enclose(int x, int y, int width, int height, int level = 0)
            {
                for (int column = x; column < x + width; column++)
                {
                    AddWall(column, y, EntityType.Hwall, level);
                    AddWall(column, y + height, EntityType.Hwall, level);
                }
                for (int row = y; row < y + height; row++)
                {
                    AddWall(x, row, EntityType.Vwall, level);
                    AddWall(x + width, row, EntityType.Vwall, level);
                }
            }

            private void AddWall(int x, int y, EntityType type, int level)
            {
                var entities = GetEntities(Map[x, y]);
                var key = new EntityData(level, type);
                if (entities.ContainsKey(key)) return;
                var root = new GameObject("Wall");
                root.transform.SetParent(Map.transform);
                var wall = root.AddComponent<Wall>();
                typeof(Wall).GetProperty(nameof(Wall.Data)).SetValue(wall, _wall);
                entities.Add(key, wall);
            }

            public void RemoveWall(int x, int y, EntityType type, int level)
            {
                var entities = GetEntities(Map[x, y]);
                var key = new EntityData(level, type);
                Wall wall = (Wall)entities[key];
                entities.Remove(key);
                Object.DestroyImmediate(wall.gameObject);
            }

            private static Dictionary<EntityData, LevelEntity> GetEntities(Tile tile)
            {
                return (Dictionary<EntityData, LevelEntity>)typeof(Tile)
                    .GetProperty("Entities", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tile);
            }

            public void Dispose() => Object.DestroyImmediate(Map.gameObject);
        }

        private sealed class TestGroundResolver : IGroundDataResolver
        {
            private readonly GroundData _ground = new GroundData("Grass", "gr", Array.Empty<string[]>(), null, null, false);
            public GroundData Resolve(string shortName) => _ground;
        }
    }
}
