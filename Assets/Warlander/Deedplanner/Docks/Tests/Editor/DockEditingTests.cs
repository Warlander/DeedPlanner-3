using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Persistence;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Deedplanner.Settings;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Docks.Tests
{
    public class DockEditingTests
    {
        [Test]
        public void CreatingReplacementLeavesExistingDockAttachedUntilCommandExecutes()
        {
            using var fixture = new DockFixture();
            Dock original = fixture.CreateDock(DockRealm.Surface);
            new DockPlacementCommand(fixture.Map, original, null).Execute();

            Dock replacement = fixture.CreateDock(DockRealm.Surface);

            fixture.AssertAttached(original, true);
            Assert.That(replacement.gameObject.activeSelf, Is.False);
            Assert.That(fixture.Map.Docks, Has.Count.EqualTo(1));
        }

        [TestCase(DockRealm.Surface)]
        [TestCase(DockRealm.Cave)]
        public void PlacementReplacementAndRemovalKeepTileAndMapConsistentThroughUndoRedo(DockRealm realm)
        {
            using var fixture = new DockFixture();
            Dock original = fixture.CreateDock(realm);
            fixture.AssertAttached(original, false);
            var placement = new DockPlacementCommand(fixture.Map, original, null);
            placement.Execute();
            fixture.AssertAttached(original, true);
            placement.Undo();
            fixture.AssertAttached(original, false);
            placement.Execute();
            fixture.AssertAttached(original, true);

            Dock replacement = fixture.CreateDock(realm);
            var replace = new DockPlacementCommand(fixture.Map, replacement, original);
            replace.Execute();
            fixture.AssertAttached(replacement, true);
            fixture.AssertAttached(original, false);
            replace.Undo();
            fixture.AssertAttached(original, true);
            fixture.AssertAttached(replacement, false);
            replace.Execute();
            fixture.AssertAttached(replacement, true);
            fixture.AssertAttached(original, false);

            var removal = new DockRemovalCommand(fixture.Map, replacement);
            removal.Execute();
            fixture.AssertAttached(replacement, false);
            Assert.That(fixture.Map.Docks, Is.Empty);
            removal.Undo();
            fixture.AssertAttached(replacement, true);
            removal.Execute();
            fixture.AssertAttached(replacement, false);
            Assert.That(fixture.Map.Docks, Is.Empty);
        }

        [Test]
        public void CaveContentRemovalRestoresCaveDockWithoutChangingSurfaceDock()
        {
            using var fixture = new DockFixture();
            Dock surface = fixture.CreateDock(DockRealm.Surface);
            Dock cave = fixture.CreateDock(DockRealm.Cave);
            new DockPlacementCommand(fixture.Map, surface, null).Execute();
            new DockPlacementCommand(fixture.Map, cave, null).Execute();
            var removal = (ICaveContentRemoval)typeof(Tile)
                .GetMethod("CreateCaveContentRemoval", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(cave.Tile, null);

            removal.Remove();
            fixture.AssertAttached(cave, false);
            fixture.AssertAttached(surface, true);
            Assert.That(fixture.Map.Docks, Has.Count.EqualTo(1));
            removal.Restore();
            fixture.AssertAttached(cave, true);
            fixture.AssertAttached(surface, true);
            Assert.That(fixture.Map.Docks, Has.Count.EqualTo(2));
            removal.Remove();
            fixture.AssertAttached(cave, false);
            fixture.AssertAttached(surface, true);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void StrokePlacesSelectedPillarsAndUndoesAsOneAction(bool automaticSupport)
        {
            using var fixture = new DockFixture();
            using var stroke = fixture.Edits.BeginDockStroke(0, 0);
            Assert.That(stroke.Place(new DockPaintRequest(0, 0, 30, fixture.Floor, automaticSupport,
                fixture.Pillar, fixture.Pillar, DockRealm.Surface, 1)), Is.True);
            Assert.That(stroke.Place(new DockPaintRequest(1, 0, 30, fixture.Floor, automaticSupport,
                fixture.Pillar, fixture.Pillar, DockRealm.Surface, 1)), Is.True);
            stroke.Commit();

            Assert.That(fixture.Map.Docks, Has.Count.EqualTo(2));
            Assert.That(fixture.Map[0, 0].GetDock(DockRealm.Surface).Support, Is.SameAs(fixture.Pillar));
            Assert.That(fixture.Map[1, 0].GetDock(DockRealm.Surface).Support, Is.SameAs(fixture.Pillar));
            fixture.Map.CommandManager.Undo();
            Assert.That(fixture.Map.Docks, Is.Empty);
            fixture.Map.CommandManager.Redo();
            Assert.That(fixture.Map.Docks, Has.Count.EqualTo(2));
        }

        [Test]
        public void AutomaticSupportUsesNoSupportOnFlatGround()
        {
            using var fixture = new DockFixture();
            using var stroke = fixture.Edits.BeginDockStroke(0, 0);
            Assert.That(stroke.Place(new DockPaintRequest(0, 0, 0, fixture.Floor, true,
                fixture.Pillar, fixture.Pillar, DockRealm.Surface, 0)), Is.True);
            stroke.Commit();

            Assert.That(fixture.Map[0, 0].GetDock(DockRealm.Surface).Support, Is.Null);
        }

        [Test]
        public void ManualBracesPreferEachMirroredStrokesPreviousTile()
        {
            using var fixture = new DockFixture(7, 3);
            fixture.Symmetry.PlaceVerticalAxis(7);
            foreach (int x in new[] { 0, 6 })
            {
                Dock anchor = fixture.CreateDock(DockRealm.Surface, x, 1, 30);
                new DockPlacementCommand(fixture.Map, anchor, null).Execute();
            }
            foreach (int x in new[] { 1, 5 })
            {
                Dock alternative = fixture.CreateDock(DockRealm.Surface, x, 2, 30);
                new DockPlacementCommand(fixture.Map, alternative, null).Execute();
            }
            using var stroke = fixture.Edits.BeginDockStroke(0, 1);
            for (int x = 1; x <= 2; x++)
            {
                Assert.That(stroke.Place(new DockPaintRequest(x, 1, 30, fixture.Floor, false,
                    fixture.Brace, fixture.Pillar, DockRealm.Surface, 1)), Is.True);
                Assert.That(fixture.Map[x, 1].GetDock(DockRealm.Surface).BraceRotation,
                    Is.EqualTo(EntityOrientation.Right));
                Assert.That(fixture.Map[6 - x, 1].GetDock(DockRealm.Surface).BraceRotation,
                    Is.EqualTo(EntityOrientation.Left));
            }
            stroke.Commit();
            fixture.Map.CommandManager.Undo();
            Assert.That(fixture.Map.Docks, Has.Count.EqualTo(4));
        }

        [Test]
        public void StrokeSkipsCellsWithTerrainAboveDeck()
        {
            using var fixture = new DockFixture();
            SetField(fixture.Map[0, 0], "surfaceHeight", 1);
            using var stroke = fixture.Edits.BeginDockStroke(0, 0);
            Assert.That(stroke.Place(new DockPaintRequest(0, 0, 0, fixture.Floor, true,
                null, fixture.Pillar, DockRealm.Surface, 0)), Is.False);
            stroke.Commit();

            Assert.That(fixture.Map.Docks, Is.Empty);
            Assert.That(fixture.Map[0, 0].GetDock(DockRealm.Surface), Is.Null);
        }

        [Test]
        public void CrossingAxesAndRepeatedPaintingCreateOneDock()
        {
            using var fixture = new DockFixture(3, 3);
            fixture.Symmetry.PlaceVerticalAxis(3);
            fixture.Symmetry.PlaceHorizontalAxis(3);
            using var stroke = fixture.Edits.BeginDockStroke(1, 1);
            var request = new DockPaintRequest(1, 1, 0, fixture.Floor, true,
                null, fixture.Pillar, DockRealm.Surface, 0);
            Assert.That(stroke.Place(request), Is.True);
            Dock placed = fixture.Map[1, 1].GetDock(DockRealm.Surface);
            Assert.That(stroke.Place(request), Is.False);
            stroke.Commit();

            Assert.That(fixture.Map.Docks, Has.Count.EqualTo(1));
            Assert.That(fixture.Map[1, 1].GetDock(DockRealm.Surface), Is.SameAs(placed));
            fixture.Map.CommandManager.Undo();
            fixture.AssertAttached(placed, false);
            fixture.Map.CommandManager.Redo();
            fixture.AssertAttached(placed, true);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private sealed class DockFixture : IDisposable
        {
            private readonly List<Dock> _createdDocks = new List<Dock>();
            private readonly SharedMaterials _materials;
            private readonly GameObject _modelRoot;
            private readonly DockFactory _factory;

            public Map Map { get; }
            public Database Database { get; }
            public FloorData Floor { get; }
            public DockSupportData Pillar { get; }
            public DockSupportData Brace { get; }
            public SymmetrySession Symmetry { get; }
            public MapEditFacade Edits { get; }

            public DockFixture(int width = 2, int height = 2)
            {
                Map = new GameObject("Dock test map").AddComponent<Map>();
                SetField(Map, "_mapRenderSettingsRetriever", new MapRenderSettings());
                SetField(Map, "<Ground>k__BackingField", Map.gameObject.AddComponent<GroundMesh>());
                Database = new Database();
                Database.AddCave(new CaveData(null, "Stone", "sw", Array.Empty<string[]>(), true, true, false));
                var grid = new MapTileGrid(width, height);
                SetField(Map, "_tileGrid", grid);
                var groundResolver = new TestGroundResolver();
                for (int x = 0; x <= Map.Width; x++)
                {
                    for (int y = 0; y <= Map.Height; y++)
                    {
                        var tile = new Tile(Map, x, y, null, Database, null, groundResolver, null);
                        tile.Cave.InitializeHeights(-30, 30);
                        grid.SetTile(x, y, tile);
                    }
                }

                var loggerSource = new LoggerSource();
                var assets = new WurmAssetFacade(loggerSource);
                ModelHandle model = assets.GetModel("Dock test model", 0);
                _modelRoot = new GameObject("Dock test model root");
                var originalModel = new GameObject("Dock test deck");
                originalModel.transform.SetParent(_modelRoot.transform);
                SetField(model, "_modelRoot", _modelRoot);
                SetField(model, "_originalModel", originalModel);
                Floor = new FloorData(model, "Dock test floor", "test", Array.Empty<string[]>(), false, true, null);
                Database.AddFloor(Floor);
                Pillar = new DockSupportData("Pillar", "test-pillar", DockSupportType.WoodPillar, model, null, null);
                Brace = new DockSupportData("Brace", "dwb", DockSupportType.Brace, model, null, null);
                Database.AddDockSupport(Pillar);
                Database.AddDockSupport(Brace);
                _materials = ScriptableObject.CreateInstance<SharedMaterials>();
                _factory = new DockFactory(_materials, Database, loggerSource);
                SetField(Map, "_dockCollection", new MapDockCollection(Map, _factory));
                SetField(Map, "_levelRenderer", new MapLevelRenderer());
                Map.CommandManager.ForgetAction();
                var maps = new MapHandler(null, null, loggerSource);
                var registry = new MapRegistry();
                registry.SetMap(Map);
                SetField(maps, "_registry", registry);
                Symmetry = new SymmetrySession(maps);
                Edits = new MapEditFacade(maps, Symmetry, _factory, Database, null);
            }

            public Dock CreateDock(DockRealm realm, int x = 0, int y = 0, int? height = null)
            {
                Dock dock = _factory.CreateDock(Map, x, y, height ?? (realm == DockRealm.Cave ? -30 : 0),
                    Floor, null, EntityOrientation.Up, realm);
                _createdDocks.Add(dock);
                return dock;
            }

            public void AssertAttached(Dock dock, bool attached)
            {
                Assert.That(dock.Tile.GetDock(dock.Realm) == dock, Is.EqualTo(attached));
                Assert.That(Map.GetDock(dock.Tile, dock.Realm) == dock, Is.EqualTo(attached));
                int matches = 0;
                foreach (Dock registeredDock in Map.Docks)
                {
                    if (registeredDock == dock) matches++;
                }
                Assert.That(matches, Is.EqualTo(attached ? 1 : 0));
                Assert.That(dock.gameObject.activeSelf, Is.EqualTo(attached));
                if (attached) Assert.That(dock.transform.parent, Is.EqualTo(Map.transform));
            }

            public void Dispose()
            {
                foreach (Dock dock in _createdDocks)
                {
                    if (dock) Object.DestroyImmediate(dock.gameObject);
                }
                Object.DestroyImmediate(Map.gameObject);
                Object.DestroyImmediate(_modelRoot);
                Object.DestroyImmediate(_materials);
            }
        }

        private sealed class TestGroundResolver : IGroundDataResolver
        {
            private readonly GroundData _ground = new GroundData("Grass", "gr", Array.Empty<string[]>(), null, null, false);

            public GroundData Resolve(string shortName) => _ground;
        }
    }
}
