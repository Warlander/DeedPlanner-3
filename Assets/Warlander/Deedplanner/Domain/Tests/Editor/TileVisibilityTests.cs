using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Decorations;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Deedplanner.Settings;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Domain.Tests
{
    public class TileVisibilityTests
    {
        [Test]
        public void UndoingFloorDeletionUndergroundDoesNotHideItAfterReturningToSurface()
        {
            using var fixture = new VisibilityFixture();
            Floor floor = fixture.Tile.SetFloor(fixture.Floor, EntityOrientation.Up, 0);
            fixture.Map.CommandManager.FinishAction();
            fixture.Tile.SetFloor(null, EntityOrientation.Up, 0);
            fixture.Map.CommandManager.FinishAction();
            fixture.Map.SetActiveRenderView(new MapRenderView(-1, true, false));

            fixture.Map.CommandManager.Undo();

            Assert.That(fixture.Tile.GetTileContent(0), Is.SameAs(floor));
            Assert.That(floor.gameObject.activeSelf, Is.True);
            Renderer renderer = floor.Model.GetComponent<Renderer>();
            using (fixture.Map.PrepareForCamera(new MapRenderView(-1, true, false)))
            {
                Assert.That(renderer.forceRenderingOff, Is.True);
            }
            fixture.Map.SetActiveRenderView(new MapRenderView(0, true, false));
            using (fixture.Map.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(floor.gameObject.activeInHierarchy, Is.True);
                Assert.That(renderer.forceRenderingOff, Is.False);
            }
        }

        [Test]
        public void CreatingAndRedoingTreeRespectsHiddenTreesSetting()
        {
            using var fixture = new VisibilityFixture();
            fixture.Settings.RenderTrees = false;
            Decoration tree = fixture.Tile.SetDecoration(fixture.Tree, new Vector2(2, 2), 0, 0);
            fixture.Map.CommandManager.FinishAction();
            Assert.That(tree.gameObject.activeSelf, Is.False);

            fixture.Map.CommandManager.Undo();
            fixture.Map.CommandManager.Redo();

            Assert.That(tree.gameObject.activeSelf, Is.False);
            fixture.Settings.RenderTrees = true;
            fixture.Tile.Refresh();
            Assert.That(tree.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void CaveContentStaysHiddenInRockAndRestoresCategoryVisibilityWhenOpened()
        {
            using var fixture = new VisibilityFixture();
            fixture.Map.SetActiveRenderView(new MapRenderView(-1, true, false));
            fixture.Settings.RenderTrees = false;
            Floor floor = fixture.Tile.SetFloor(fixture.Floor, EntityOrientation.Up, -1);
            Decoration tree = fixture.Tile.SetDecoration(fixture.Tree, new Vector2(2, 2), 0, -1);
            Assert.That(floor.gameObject.activeSelf, Is.False);
            Assert.That(tree.gameObject.activeSelf, Is.False);

            fixture.Tile.Cave.InitializeTerrain(fixture.OpenCave);
            fixture.Tile.Refresh();
            Assert.That(floor.gameObject.activeSelf, Is.True);
            Assert.That(tree.gameObject.activeSelf, Is.False);
            fixture.Settings.RenderTrees = true;
            fixture.Tile.Refresh();
            Assert.That(tree.gameObject.activeSelf, Is.True);

            fixture.Tile.Cave.InitializeTerrain(fixture.SolidCave);
            fixture.Tile.Refresh();
            Assert.That(floor.gameObject.activeSelf, Is.False);
            Assert.That(tree.gameObject.activeSelf, Is.False);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private sealed class VisibilityFixture : IDisposable
        {
            private readonly GameObject _modelRoot;

            public Map Map { get; }
            public Tile Tile => Map[0, 0];
            public MapRenderSettings Settings { get; } = new MapRenderSettings();
            public FloorData Floor { get; }
            public DecorationData Tree { get; }
            public CaveData SolidCave { get; } = new CaveData(null, "Stone", "sw", Array.Empty<string[]>(), true, true, false);
            public CaveData OpenCave { get; } = new CaveData(null, "Open cave", "o", Array.Empty<string[]>(), false, true, false);

            public VisibilityFixture()
            {
                Map = new GameObject("Tile visibility test").AddComponent<Map>();
                SetField(Map, "_mapRenderSettingsRetriever", Settings);
                SetField(Map, "<Ground>k__BackingField", Map.gameObject.AddComponent<GroundMesh>());
                var grid = new MapTileGrid(1, 1);
                SetField(Map, "_tileGrid", grid);
                var database = new Database();
                database.AddCave(SolidCave);
                var groundResolver = new TestGroundResolver();
                for (int x = 0; x <= 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                    {
                        var tile = new Tile(Map, x, y, null, database, null, groundResolver, null);
                        tile.Cave.InitializeHeights(-30, 30);
                        grid.SetTile(x, y, tile);
                    }
                }
                SetField(Map, "_roofCalculator", new MapRoofCalculator(Map));
                SetField(Map, "<Caves>k__BackingField", new CaveMap(1, 1, (x, y) => Map[x, y].Cave, SolidCave));
                SetField(Map, "_dockCollection", new MapDockCollection(Map, null));
                var renderer = new MapLevelRenderer();
                renderer.Initialize(new[] { CreateRoot("Surface") }, new[] { CreateRoot("Cave") },
                    CreateRoot("Surface grid"), CreateRoot("Cave grid"), CreateRoot("Cave shell"), null, null);
                SetField(Map, "_levelRenderer", renderer);
                Map.CommandManager.ForgetAction();

                var assets = new WurmAssetFacade(new LoggerSource());
                ModelHandle model = assets.GetModel("Tile visibility model", 0);
                _modelRoot = new GameObject("Tile visibility model root");
                var originalModel = new GameObject("Tile visibility model", typeof(MeshRenderer));
                originalModel.transform.SetParent(_modelRoot.transform);
                SetField(model, "_modelRoot", _modelRoot);
                SetField(model, "_originalModel", originalModel);
                Floor = new FloorData(model, "Floor", "floor", Array.Empty<string[]>(), false, false, null);
                Tree = new DecorationData(model, "Tree", "tree", Array.Empty<string[]>(), "tree",
                    false, false, false, true, false, null);
            }

            private Transform CreateRoot(string name)
            {
                var root = new GameObject(name);
                root.transform.SetParent(Map.transform);
                return root.transform;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Map.gameObject);
                Object.DestroyImmediate(_modelRoot);
            }
        }

        private sealed class TestGroundResolver : IGroundDataResolver
        {
            private readonly GroundData _ground = new GroundData("Grass", "gr", Array.Empty<string[]>(), null, null, false);

            public GroundData Resolve(string shortName) => _ground;
        }
    }
}
