using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Domain.Entities.Walls;
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

        [TestCase(DockRealm.Surface, 1, 0)]
        [TestCase(DockRealm.Surface, 0, 1)]
        [TestCase(DockRealm.Surface, 1, 1)]
        [TestCase(DockRealm.Cave, 1, 0)]
        [TestCase(DockRealm.Cave, 0, 1)]
        [TestCase(DockRealm.Cave, 1, 1)]
        public void DifferentHeightNeighborsRemainValidThroughUndoRedo(DockRealm realm, int x, int y)
        {
            using var fixture = new DockFixture();
            int high = realm == DockRealm.Cave ? -10 : 60;
            int low = realm == DockRealm.Cave ? -20 : 30;
            int level = realm == DockRealm.Cave ? -1 : 0;
            using (var stroke = fixture.Edits.BeginDockStroke(0, 0))
            {
                Assert.That(stroke.Place(new DockPaintRequest(0, 0, high, fixture.Floor, false,
                    fixture.Pillar, fixture.Pillar, realm, level)), Is.True);
                stroke.Commit();
            }
            using (var stroke = fixture.Edits.BeginDockStroke(x, y))
            {
                Assert.That(stroke.Place(new DockPaintRequest(x, y, low, fixture.Floor, false,
                    fixture.Pillar, fixture.Pillar, realm, level)), Is.True);
                stroke.Commit();
            }

            Dock higher = fixture.Map[0, 0].GetDock(realm);
            Dock lower = fixture.Map[x, y].GetDock(realm);
            Assert.That(higher.ValidationErrors, Is.Empty);
            Assert.That(lower.ValidationErrors, Is.Empty);
            fixture.Map.CommandManager.Undo();
            fixture.AssertAttached(higher, true);
            fixture.AssertAttached(lower, false);
            fixture.Map.CommandManager.Redo();
            fixture.AssertAttached(lower, true);
            Assert.That(higher.ValidationErrors, Is.Empty);
            Assert.That(lower.ValidationErrors, Is.Empty);
        }

        [TestCase(DockRealm.Surface)]
        [TestCase(DockRealm.Cave)]
        public void NeighborSupportDoesNotMoveGroundWallsOrReverseTheirSlope(DockRealm realm)
        {
            using var fixture = new DockFixture(3, 3);
            fixture.OpenCaves();
            bool cave = realm == DockRealm.Cave;
            int level = cave ? -1 : 0;
            if (cave)
            {
                fixture.Map[2, 1].Cave.InitializeHeights(-20, 20);
            }
            else
            {
                SetField(fixture.Map[2, 1], "surfaceHeight", 30);
            }
            Tile tile = fixture.Map[1, 1];
            Wall horizontal = tile.SetHorizontalWall(fixture.Wall, false, level);
            Wall vertical = tile.SetVerticalWall(fixture.Wall, false, level);
            float baseHeight = cave ? -3f : 0f;
            int slope = cave ? -10 : -30;
            var placement = new DockPlacementCommand(fixture.Map,
                fixture.CreateDock(realm, 0, 1, cave ? -10 : 60, level), null);
            for (int i = 0; i < 2; i++)
            {
                placement.Execute();
                Assert.That(horizontal.transform.localPosition.y, Is.EqualTo(baseHeight));
                Assert.That(vertical.transform.localPosition.y, Is.EqualTo(baseHeight));
                Assert.That(horizontal.SlopeDifference, Is.EqualTo(slope));
                Assert.That(vertical.SlopeDifference, Is.Zero);
                placement.Undo();
                Assert.That(horizontal.transform.localPosition.y, Is.EqualTo(baseHeight));
                Assert.That(horizontal.SlopeDifference, Is.EqualTo(slope));
            }
        }

        [TestCase(true, WallSupportSide.Tile)]
        [TestCase(true, WallSupportSide.OppositeTile)]
        [TestCase(false, WallSupportSide.Tile)]
        [TestCase(false, WallSupportSide.OppositeTile)]
        public void DeckWallKeepsItsChosenSupportBesideAnotherHeight(bool horizontal, WallSupportSide side)
        {
            using var fixture = new DockFixture(3, 3);
            int otherX = horizontal ? 1 : 0;
            int otherY = horizontal ? 0 : 1;
            Dock own = fixture.CreateDock(DockRealm.Surface, 1, 1,
                side == WallSupportSide.Tile ? 60 : 30, 0);
            Dock opposite = fixture.CreateDock(DockRealm.Surface, otherX, otherY,
                side == WallSupportSide.Tile ? 30 : 60, 0);
            new DockPlacementCommand(fixture.Map, own, null).Execute();
            new DockPlacementCommand(fixture.Map, opposite, null).Execute();
            Tile tile = fixture.Map[1, 1];
            Wall wall = horizontal ? tile.SetHorizontalWall(fixture.Wall, false, 0, side)
                : tile.SetVerticalWall(fixture.Wall, false, 0, side);
            Assert.That(wall.transform.localPosition.y, Is.EqualTo(6f));
            Assert.That(wall.SlopeDifference, Is.Zero);
            var removal = new DockRemovalCommand(fixture.Map,
                side == WallSupportSide.Tile ? own : opposite);
            removal.Execute();
            Assert.That(wall.transform.localPosition.y, Is.Zero);
            removal.Undo();
            Assert.That(wall.transform.localPosition.y, Is.EqualTo(6f));
            Assert.That(wall.SlopeDifference, Is.Zero);
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void WallAttachmentLoadsBeforeSupportAndSurvivesRepainting(bool horizontal, bool attachmentSaved)
        {
            using var source = new DockFixture(3, 3);
            Tile sourceTile = source.Map[1, 1];
            Wall original = horizontal
                ? sourceTile.SetHorizontalWall(source.Wall, false, 0, WallSupportSide.OppositeTile)
                : sourceTile.SetVerticalWall(source.Wall, false, 0, WallSupportSide.OppositeTile);
            var document = new XmlDocument();
            XmlElement tileElement = document.CreateElement("tile");
            XmlElement levelElement = document.CreateElement("level");
            levelElement.SetAttribute("value", "0");
            tileElement.AppendChild(levelElement);
            XmlElement wallElement = document.CreateElement(horizontal ? "hWall" : "vWall");
            original.Serialize(document, wallElement);
            if (!attachmentSaved) wallElement.RemoveAttribute("supportSide");
            levelElement.AppendChild(wallElement);

            using var loaded = new DockFixture(3, 3);
            Tile tile = loaded.Map[1, 1];
            tile.DeserializeEntities(tileElement);
            Wall wall = horizontal ? tile.GetHorizontalWall(0) : tile.GetVerticalWall(0);
            WallSupportSide expectedSide = attachmentSaved ? WallSupportSide.OppositeTile : WallSupportSide.Ground;
            float expectedHeight = attachmentSaved ? 6f : 0f;
            Assert.That(wall.SupportSide, Is.EqualTo(expectedSide));
            Assert.That(wall.transform.localPosition.y, Is.Zero);
            new DockPlacementCommand(loaded.Map, loaded.CreateDock(DockRealm.Surface,
                horizontal ? 1 : 0, horizontal ? 0 : 1, 60, 0), null).Execute();
            Assert.That(wall.transform.localPosition.y, Is.EqualTo(expectedHeight));
            Wall repainted = horizontal ? tile.SetHorizontalWall(loaded.Wall, true, 0, wall.SupportSide)
                : tile.SetVerticalWall(loaded.Wall, true, 0, wall.SupportSide);
            Assert.That(repainted.SupportSide, Is.EqualTo(expectedSide));
            Assert.That(repainted.transform.localPosition.y, Is.EqualTo(expectedHeight));
            loaded.Map.CommandManager.FinishAction();
            loaded.Map.CommandManager.Undo();
            loaded.Map.CommandManager.Redo();
            Assert.That(repainted.transform.localPosition.y, Is.EqualTo(expectedHeight));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RepaintingFromDeckReanchorsAnExistingGroundWallWithUndo(bool horizontal)
        {
            using var fixture = new DockFixture(3, 3);
            Tile tile = fixture.Map[1, 1];
            Wall groundWall = horizontal ? tile.SetHorizontalWall(fixture.Wall, false, 0)
                : tile.SetVerticalWall(fixture.Wall, false, 0);
            fixture.Map.CommandManager.FinishAction();
            new DockPlacementCommand(fixture.Map,
                fixture.CreateDock(DockRealm.Surface, 1, 1, 60, 0), null).Execute();
            Assert.That(groundWall.transform.localPosition.y, Is.Zero);
            Wall deckWall = horizontal ? tile.SetHorizontalWall(fixture.Wall, false, 0, WallSupportSide.Tile)
                : tile.SetVerticalWall(fixture.Wall, false, 0, WallSupportSide.Tile);
            fixture.Map.CommandManager.FinishAction();
            Assert.That(deckWall.transform.localPosition.y, Is.EqualTo(6f));
            fixture.Map.CommandManager.Undo();
            Assert.That(groundWall.gameObject.activeSelf, Is.True);
            Assert.That(groundWall.transform.localPosition.y, Is.Zero);
            fixture.Map.CommandManager.Redo();
            Assert.That(deckWall.gameObject.activeSelf, Is.True);
            Assert.That(deckWall.transform.localPosition.y, Is.EqualTo(6f));
            Wall returned = horizontal ? tile.SetHorizontalWall(fixture.Wall, false, 0, WallSupportSide.Ground)
                : tile.SetVerticalWall(fixture.Wall, false, 0, WallSupportSide.Ground);
            Assert.That(returned.transform.localPosition.y, Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MirroredWallKeepsItsSupportOnTheReflectedSide(bool horizontal)
        {
            using var fixture = new DockFixture(horizontal ? 3 : 7, horizontal ? 7 : 3);
            if (horizontal) fixture.Symmetry.PlaceHorizontalAxis(7);
            else fixture.Symmetry.PlaceVerticalAxis(7);
            new DockPlacementCommand(fixture.Map,
                fixture.CreateDock(DockRealm.Surface, 1, 1, 60, 0), null).Execute();
            new DockPlacementCommand(fixture.Map,
                fixture.CreateDock(DockRealm.Surface, horizontal ? 1 : 5, horizontal ? 5 : 1, 60, 0), null).Execute();
            if (horizontal) fixture.Edits.SetHorizontalWall(1, 1, fixture.Wall, false, 0, WallSupportSide.Tile);
            else fixture.Edits.SetVerticalWall(1, 1, fixture.Wall, false, 0, WallSupportSide.Tile);
            Wall reflected = horizontal ? fixture.Map[1, 6].GetHorizontalWall(0)
                : fixture.Map[6, 1].GetVerticalWall(0);
            Assert.That(reflected.SupportSide, Is.EqualTo(WallSupportSide.OppositeTile));
            Assert.That(reflected.transform.localPosition.y, Is.EqualTo(6f));
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

        [TestCase(true, DockRealm.Surface)]
        [TestCase(false, DockRealm.Surface)]
        [TestCase(true, DockRealm.Cave)]
        [TestCase(false, DockRealm.Cave)]
        public void WallStrokeKeepsDeckHeightAndResolvesEachEdgesSupportingSide(bool horizontal, DockRealm realm)
        {
            using var fixture = new DockFixture(3, 3);
            int level = realm == DockRealm.Cave ? -1 : 0;
            int height = realm == DockRealm.Cave ? -10 : 60;
            new DockPlacementCommand(fixture.Map, fixture.CreateDock(realm, 1, 1, height, level), null).Execute();
            var updater = new WallUpdater(null, null, null, null, null, null, null, null, null, null);
            WallSupportSide side = WallSupportSide.Tile;
            Assert.That(ResolveWallStroke(updater, fixture.Map, 1, 1, horizontal, ref level, ref side), Is.True);

            level = realm == DockRealm.Cave ? -2 : 1;
            side = WallSupportSide.Ground;
            Assert.That(ResolveWallStroke(updater, fixture.Map, horizontal ? 1 : 2, horizontal ? 2 : 1,
                horizontal, ref level, ref side), Is.True);
            Assert.That(side, Is.EqualTo(WallSupportSide.OppositeTile));
            Assert.That(level, Is.EqualTo(realm == DockRealm.Cave ? -1 : 0));

            side = WallSupportSide.OppositeTile;
            Assert.That(ResolveWallStroke(updater, fixture.Map, 1, 1, horizontal, ref level, ref side), Is.True);
            Assert.That(side, Is.EqualTo(WallSupportSide.Tile));
        }

        [Test]
        public void DeckWallStrokeSkipsGapsDifferentHeightsAndDifferentAnchorLevels()
        {
            using var fixture = new DockFixture(4, 3);
            new DockPlacementCommand(fixture.Map, fixture.CreateDock(DockRealm.Surface, 0, 1, 60, 0), null).Execute();
            new DockPlacementCommand(fixture.Map, fixture.CreateDock(DockRealm.Surface, 1, 1, 30, 0), null).Execute();
            new DockPlacementCommand(fixture.Map, fixture.CreateDock(DockRealm.Surface, 2, 1, 60, 1), null).Execute();
            new DockPlacementCommand(fixture.Map, fixture.CreateDock(DockRealm.Surface, 3, 1, 60, 0), null).Execute();
            var updater = new WallUpdater(null, null, null, null, null, null, null, null, null, null);
            int level = 0;
            WallSupportSide side = WallSupportSide.Tile;
            Assert.That(ResolveWallStroke(updater, fixture.Map, 0, 1, true, ref level, ref side), Is.True);
            Assert.That(ResolveWallStroke(updater, fixture.Map, 1, 1, true, ref level, ref side), Is.False);
            Assert.That(ResolveWallStroke(updater, fixture.Map, 2, 1, true, ref level, ref side), Is.False);
            Assert.That(ResolveWallStroke(updater, fixture.Map, 4, 1, true, ref level, ref side), Is.False);
            Assert.That(ResolveWallStroke(updater, fixture.Map, 3, 1, true, ref level, ref side), Is.True);
            Assert.That(side, Is.EqualTo(WallSupportSide.Tile));
        }

        [Test]
        public void GroundWallStrokeIgnoresDeckHoverUntilStrokeEnds()
        {
            using var fixture = new DockFixture();
            new DockPlacementCommand(fixture.Map, fixture.CreateDock(DockRealm.Surface, 1, 1, 60, 0), null).Execute();
            var updater = new WallUpdater(null, null, null, null, null, null, null, null, null, null);
            int level = 0;
            WallSupportSide side = WallSupportSide.Ground;
            Assert.That(ResolveWallStroke(updater, fixture.Map, 0, 1, true, ref level, ref side), Is.True);

            level = 1;
            side = WallSupportSide.Tile;
            Assert.That(ResolveWallStroke(updater, fixture.Map, 1, 1, true, ref level, ref side), Is.True);
            Assert.That(side, Is.EqualTo(WallSupportSide.Ground));
            Assert.That(level, Is.Zero);

            updater.Disable();
            side = WallSupportSide.Tile;
            Assert.That(ResolveWallStroke(updater, fixture.Map, 1, 1, true, ref level, ref side), Is.True);
            Assert.That(side, Is.EqualTo(WallSupportSide.Tile));
        }

        private static bool ResolveWallStroke(WallUpdater updater, Map map, int x, int y, bool horizontal,
            ref int level, ref WallSupportSide side)
        {
            MethodInfo method = typeof(WallUpdater).GetMethod("TryResolvePaintBase",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            object[] arguments = { map, x, y, horizontal, level, side };
            bool accepted = (bool)method.Invoke(updater, arguments);
            level = (int)arguments[4];
            side = (WallSupportSide)arguments[5];
            return accepted;
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
            public WallData Wall { get; }
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
                var originalModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                originalModel.name = "Dock test deck";
                Object.DestroyImmediate(originalModel.GetComponent<Collider>());
                originalModel.transform.SetParent(_modelRoot.transform);
                SetField(model, "_modelRoot", _modelRoot);
                SetField(model, "_originalModel", originalModel);
                Floor = new FloorData(model, "Dock test floor", "test", Array.Empty<string[]>(), false, true, null);
                Database.AddFloor(Floor);
                Wall = new WallData(model, model, "Test wall", "test-wall", Array.Empty<string[]>(),
                    Color.white, 1, true, false, false, null, null);
                Database.AddWall(Wall);
                Pillar = new DockSupportData("Pillar", "test-pillar", DockSupportType.WoodPillar, model, null, null);
                Brace = new DockSupportData("Brace", "dwb", DockSupportType.Brace, model, null, null);
                Database.AddDockSupport(Pillar);
                Database.AddDockSupport(Brace);
                _materials = ScriptableObject.CreateInstance<SharedMaterials>();
                _factory = new DockFactory(_materials, Database, loggerSource);
                SetField(Map, "_dockCollection", new MapDockCollection(Map, _factory));
                var renderer = new MapLevelRenderer();
                var roots = new Transform[16];
                for (int i = 0; i < roots.Length; i++) roots[i] = Map.transform;
                SetField(renderer, "_surfaceLevelRoots", roots);
                SetField(renderer, "_caveLevelRoots", roots);
                SetField(Map, "_levelRenderer", renderer);
                Map.CommandManager.ForgetAction();
                var maps = new MapHandler(null, null, loggerSource);
                var registry = new MapRegistry();
                registry.SetMap(Map);
                SetField(maps, "_registry", registry);
                Symmetry = new SymmetrySession(maps);
                Edits = new MapEditFacade(maps, Symmetry, _factory, Database, null);
            }

            public Dock CreateDock(DockRealm realm, int x = 0, int y = 0, int? height = null,
                int? anchorLevel = null)
            {
                Dock dock = _factory.CreateDock(Map, x, y, height ?? (realm == DockRealm.Cave ? -30 : 0),
                    Floor, null, EntityOrientation.Up, realm, anchorLevel);
                _createdDocks.Add(dock);
                return dock;
            }

            public void OpenCaves()
            {
                var air = new CaveData(null, "Cave", "cave", Array.Empty<string[]>(), false, true, false);
                for (int x = 0; x <= Map.Width; x++)
                {
                    for (int y = 0; y <= Map.Height; y++) Map[x, y].Cave.Initialize(air, -30, 30);
                }
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
                foreach (Wall wall in Map.GetComponentsInChildren<Wall>(true))
                {
                    var mesh = (Mesh)typeof(Wall).GetField("boundsMesh", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(wall);
                    SetField(wall, "boundsMesh", null);
                    if (mesh) Object.DestroyImmediate(mesh);
                }
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
