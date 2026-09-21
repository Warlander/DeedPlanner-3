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
using Warlander.Deedplanner.Logging;
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

            public DockFixture()
            {
                Map = new GameObject("Dock test map").AddComponent<Map>();
                SetField(Map, "_mapRenderSettingsRetriever", new MapRenderSettings());
                SetField(Map, "<Ground>k__BackingField", Map.gameObject.AddComponent<GroundMesh>());
                Database = new Database();
                Database.AddCave(new CaveData(null, "Stone", "sw", Array.Empty<string[]>(), true, true, false));
                var grid = new MapTileGrid(2, 2);
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
                _materials = ScriptableObject.CreateInstance<SharedMaterials>();
                _factory = new DockFactory(_materials, Database, loggerSource);
                SetField(Map, "_dockCollection", new MapDockCollection(Map, _factory));
                SetField(Map, "_levelRenderer", new MapLevelRenderer());
            }

            public Dock CreateDock(DockRealm realm)
            {
                Dock dock = _factory.CreateDock(Map, 0, 0, realm == DockRealm.Cave ? -30 : 0,
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
