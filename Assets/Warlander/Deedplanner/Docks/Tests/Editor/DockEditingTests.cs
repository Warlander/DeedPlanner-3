using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Ui.Tooltips;
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

        [TestCase(EntityOrientation.Up, 0f, -4f, 0f)]
        [TestCase(EntityOrientation.Right, 90f, 0f, 0f)]
        [TestCase(EntityOrientation.Down, 180f, 0f, -4f)]
        [TestCase(EntityOrientation.Left, 270f, -4f, -4f)]
        public void DeckRotationKeepsBraceDirectionAndTileFootprint(EntityOrientation orientation, float yaw, float x, float z)
        {
            using var fixture = new DockFixture(3, 3);
            var factory = (DockFactory)typeof(DockFixture).GetField("_factory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fixture);
            Dock dock = factory.CreateDock(fixture.Map, 1, 1, 30, fixture.Floor, fixture.Brace,
                EntityOrientation.Right, DockRealm.Surface, 1, orientation);
            try
            {

                var model = (GameObject)typeof(Dock).GetField("_deckModel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dock);
                Transform deck = model.transform;
                Assert.That(dock.DeckOrientation, Is.EqualTo(orientation));
                Assert.That(dock.BraceRotation, Is.EqualTo(EntityOrientation.Right));
                Assert.That(Quaternion.Angle(deck.localRotation, Quaternion.Euler(0, yaw, 0)), Is.LessThan(0.01f));
                Assert.That(deck.localPosition, Is.EqualTo(new Vector3(x, 0, z)));
                Transform support = dock.transform.Find("Support");
                Assert.That(support.localRotation, Is.EqualTo(Quaternion.Euler(0, 180, 0)));
            }
            finally
            {
                Object.DestroyImmediate(dock.gameObject);
            }
        }

        [TestCase(EntityOrientation.Up, DockRealm.Surface)]
        [TestCase(EntityOrientation.Left, DockRealm.Cave)]
        [TestCase(EntityOrientation.Down, DockRealm.Surface)]
        [TestCase(EntityOrientation.Right, DockRealm.Cave)]
        public void DockXmlPreservesIndependentDirectionsAndLegacyDefaults(EntityOrientation orientation, DockRealm realm)
        {
            using var fixture = new DockFixture(3, 3);
            var factory = (DockFactory)typeof(DockFixture).GetField("_factory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fixture);
            Dock original = factory.CreateDock(fixture.Map, 1, 1, 0, fixture.Floor, fixture.Brace,
                EntityOrientation.Right, realm, realm == DockRealm.Surface ? 0 : -1, orientation);
            var document = new XmlDocument();
            XmlElement element = document.CreateElement("dock");
            original.Serialize(document, element);
            Dock loaded = factory.CreateDock(fixture.Map, element);
            element.RemoveAttribute("deckDir");
            Dock legacy = factory.CreateDock(fixture.Map, element);
            try
            {
                Assert.That(loaded.DeckOrientation, Is.EqualTo(orientation));
                Assert.That(loaded.BraceRotation, Is.EqualTo(EntityOrientation.Right));
                Assert.That(loaded.Realm, Is.EqualTo(realm));
                Assert.That(loaded.Support, Is.SameAs(fixture.Brace));
                Assert.That(legacy.DeckOrientation, Is.EqualTo(EntityOrientation.Down));
            }
            finally
            {
                Object.DestroyImmediate(original.gameObject);
                Object.DestroyImmediate(loaded.gameObject);
                Object.DestroyImmediate(legacy.gameObject);
            }
        }

        [Test]
        public void MirroredDeckRotationAndResizePreserveIndependentBraceDirection()
        {
            using var fixture = new DockFixture(5, 3);
            fixture.Symmetry.PlaceVerticalAxis(5);
            using (var stroke = fixture.Edits.BeginDockStroke(0, 1))
            {
                Assert.That(stroke.Place(new DockPaintRequest(1, 1, 30, fixture.Floor, true,
                    fixture.Pillar, fixture.Pillar, DockRealm.Surface, 1, EntityOrientation.Left)), Is.True);
                stroke.Commit();
            }
            Dock own = fixture.Map[1, 1].GetDock(DockRealm.Surface);
            Dock reflected = fixture.Map[3, 1].GetDock(DockRealm.Surface);
            Assert.That(own.DeckOrientation, Is.EqualTo(EntityOrientation.Left));
            Assert.That(reflected.DeckOrientation, Is.EqualTo(EntityOrientation.Right));
            fixture.Map.CommandManager.Undo();
            Assert.That(fixture.Map.Docks, Is.Empty);
            fixture.Map.CommandManager.Redo();
            Assert.That(reflected.DeckOrientation, Is.EqualTo(EntityOrientation.Right));
            using var resized = new DockFixture(7, 5);
            var collection = (MapDockCollection)typeof(Map).GetField("_dockCollection", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(resized.Map);
            collection.InitializeDocksAfterResize(fixture.Map, 1, 1);
            Assert.That(resized.Map[2, 2].GetDock(DockRealm.Surface).DeckOrientation, Is.EqualTo(EntityOrientation.Left));
            Assert.That(resized.Map[4, 2].GetDock(DockRealm.Surface).DeckOrientation, Is.EqualTo(EntityOrientation.Right));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-2)]
        public void IsolatedClickOnSlopeCreatesSupportedDockAndQuickClickErasesFinalDock(int level)
        {
            using var fixture = new FloorInputFixture();
            fixture.TargetGround(1, 1, level);
            fixture.SetSlope(1, 1, level);
            fixture.Press(MouseButton.Left);
            fixture.Release();
            DockRealm realm = level < 0 ? DockRealm.Cave : DockRealm.Surface;
            Dock dock = fixture.Map[1, 1].GetDock(realm);
            Assert.That(dock, Is.Not.Null);
            Assert.That(fixture.Map[1, 1].GetTileContent(level), Is.Null);
            Assert.That(dock.Support, Is.SameAs(fixture.Data.Pillar));
            Assert.That(dock.Height, Is.EqualTo(level < 0 ? CaveLevel.GetAbsoluteHeight(-20, level) : 30));
            Assert.That(dock.AnchorLevel, Is.EqualTo(level));
            Assert.That(dock.ValidationErrors, Is.Empty);
            fixture.Map.CommandManager.Undo();
            Assert.That(fixture.Map.Docks, Is.Empty);
            fixture.Map.CommandManager.Redo();
            fixture.TargetDock(dock);
            fixture.Press(MouseButton.Right);
            Assert.That(fixture.Map.Docks, Is.Empty);
            fixture.Release();
            fixture.Map.CommandManager.Undo();
            Assert.That(fixture.Map[1, 1].GetDock(realm), Is.SameAs(dock));
            fixture.Map.CommandManager.Redo();
            Assert.That(fixture.Map.Docks, Is.Empty);
        }

        [Test]
        public void DragIncludesFirstDockAndReplacementKeepsSupportThroughHistory()
        {
            using var fixture = new FloorInputFixture();
            fixture.SetSlope(1, 1, 0);
            fixture.TargetGround(1, 1, 0);
            fixture.Press(MouseButton.Left);
            fixture.TargetGround(2, 1, 0);
            fixture.Press(MouseButton.Left);
            fixture.Release();
            Assert.That(fixture.Map.Docks, Has.Count.EqualTo(2));
            fixture.Map.CommandManager.Undo();
            Assert.That(fixture.Map.Docks, Is.Empty);
            fixture.Map.CommandManager.Redo();
            Dock original = fixture.Map[1, 1].GetDock(DockRealm.Surface);
            fixture.SetOrientation(EntityOrientation.Left);
            fixture.TargetDock(original);
            fixture.Press(MouseButton.Left);
            fixture.Release();
            Dock replacement = fixture.Map[1, 1].GetDock(DockRealm.Surface);
            Assert.That(replacement, Is.Not.SameAs(original));
            Assert.That(replacement.Height, Is.EqualTo(original.Height));
            Assert.That(replacement.Support, Is.SameAs(fixture.Data.Pillar));
            Assert.That(replacement.DeckOrientation, Is.EqualTo(EntityOrientation.Left));
            fixture.Map.CommandManager.Undo();
            Assert.That(fixture.Map[1, 1].GetDock(DockRealm.Surface), Is.SameAs(original));
            fixture.Map.CommandManager.Redo();
            Assert.That(fixture.Map[1, 1].GetDock(DockRealm.Surface), Is.SameAs(replacement));
            fixture.TargetDock(replacement);
            fixture.Press(MouseButton.Right);
            fixture.Release();
            fixture.TargetGround(1, 1, 0);
            fixture.Press(MouseButton.Left);
            fixture.Release();
            Assert.That(fixture.Map[1, 1].GetDock(DockRealm.Surface).Support, Is.SameAs(fixture.Data.Pillar));
        }

        [Test]
        public void ExistingUpperFloorAnchorsHeightAndBracesIgnoreDeckSelector()
        {
            using var fixture = new FloorInputFixture();
            fixture.Data.Edits.SetFloor(0, 1, fixture.Data.Floor, EntityOrientation.Up, 2);
            fixture.Data.Edits.SetFloor(2, 1, fixture.Data.Floor, EntityOrientation.Up, 2);
            fixture.Data.Edits.FinishAction();
            Floor anchor = (Floor)fixture.Map[0, 1].GetTileContent(2);
            fixture.UseBraces();
            fixture.SetOrientation(EntityOrientation.Up);
            fixture.TargetFloor(anchor);
            fixture.Press(MouseButton.Left);
            fixture.TargetGround(1, 1, 2);
            fixture.Press(MouseButton.Left);
            fixture.Release();
            Dock first = fixture.Map[1, 1].GetDock(DockRealm.Surface);
            Assert.That(first.Height, Is.EqualTo(60));
            Assert.That(first.AnchorLevel, Is.EqualTo(2));
            Assert.That(first.DeckOrientation, Is.EqualTo(EntityOrientation.Up));
            Assert.That(first.BraceRotation, Is.EqualTo(EntityOrientation.Right));
            Assert.That(fixture.Map[0, 1].GetTileContent(2), Is.SameAs(anchor));
            fixture.Map.CommandManager.Undo();
            Assert.That(fixture.Map.Docks, Is.Empty);
            Assert.That(fixture.Map[0, 1].GetTileContent(2), Is.SameAs(anchor));
            fixture.Map.CommandManager.Redo();
            fixture.TargetDock(first);
            fixture.Press(MouseButton.Right);
            fixture.Release();
            fixture.TargetFloor((Floor)fixture.Map[2, 1].GetTileContent(2));
            fixture.Press(MouseButton.Left);
            fixture.TargetGround(1, 1, 2);
            fixture.Press(MouseButton.Left);
            fixture.Release();
            Assert.That(fixture.Map[1, 1].GetDock(DockRealm.Surface).BraceRotation, Is.EqualTo(EntityOrientation.Left));
        }

        [Test]
        public void RepaintingDeckKeepsBraceSideWhenSeveralAttachmentsAreValid()
        {
            using var fixture = new FloorInputFixture();
            fixture.Data.Edits.SetFloor(0, 1, fixture.Data.Floor, EntityOrientation.Up, 1);
            fixture.Data.Edits.SetFloor(1, 2, fixture.Data.Floor, EntityOrientation.Up, 1);
            fixture.Data.Edits.FinishAction();
            fixture.UseBraces();
            fixture.TargetFloor((Floor)fixture.Map[0, 1].GetTileContent(1));
            fixture.Press(MouseButton.Left);
            fixture.TargetGround(1, 1, 1);
            fixture.Press(MouseButton.Left);
            fixture.Release();
            Dock original = fixture.Map[1, 1].GetDock(DockRealm.Surface);
            Assert.That(original.BraceRotation, Is.EqualTo(EntityOrientation.Right));
            fixture.SetOrientation(EntityOrientation.Left);
            fixture.TargetDock(original);
            fixture.Press(MouseButton.Left);
            fixture.Release();
            Dock replacement = fixture.Map[1, 1].GetDock(DockRealm.Surface);
            Assert.That(replacement.DeckOrientation, Is.EqualTo(EntityOrientation.Left));
            Assert.That(replacement.BraceRotation, Is.EqualTo(original.BraceRotation));
            Assert.That(replacement.ValidationErrors, Is.Empty);
            fixture.Map.CommandManager.Undo();
            Assert.That(fixture.Map[1, 1].GetDock(DockRealm.Surface), Is.SameAs(original));
            fixture.Map.CommandManager.Redo();
            Assert.That(fixture.Map[1, 1].GetDock(DockRealm.Surface), Is.SameAs(replacement));
        }

        private sealed class FloorInputFixture : IDisposable
        {
            private readonly GameObject _cameraRoot;
            private readonly GameObject _tooltipRoot;
            private readonly MultiCamera _camera;
            private readonly BoxCollider _terrain;
            private readonly DPInput _input;
            private readonly Mouse _mouse;
            private readonly UnityEngine.InputSystem.InputSettings _originalSettings;
            private readonly UnityEngine.InputSystem.InputSettings _testSettings;
            public DockFixture Data { get; }
            public Map Map => Data.Map;
            public FloorUpdater Updater { get; }

            public FloorInputFixture()
            {
                Data = new DockFixture(4, 3);
                Data.OpenCaves();
                for (int x = 0; x <= Map.Width; x++)
                {
                    for (int y = 0; y <= Map.Height; y++) Map[x, y].Cave.InitializeHeights(-30, 100);
                }
                SetField(Map, "_roofCalculator", new MapRoofCalculator(Map));
                _cameraRoot = new GameObject("Dock input camera");
                _cameraRoot.SetActive(false);
                _camera = _cameraRoot.AddComponent<MultiCamera>();
                var coordinator = _cameraRoot.AddComponent<CameraCoordinator>();
                SetField(coordinator, "_cameras", new[] { _camera });
                _terrain = Map.Ground.gameObject.AddComponent<BoxCollider>();
                _terrain.size = new Vector3(4, 0.1f, 4);
                _originalSettings = InputSystem.settings;
                _testSettings = Object.Instantiate(_originalSettings);
                _testSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
                InputSystem.settings = _testSettings;
                _input = new DPInput();
                _mouse = InputSystem.AddDevice<Mouse>();
                _input.devices = new InputDevice[] { _mouse };
                _input.Enable();
                _tooltipRoot = new GameObject("Dock tooltip", typeof(RectTransform), typeof(CanvasGroup));
                _tooltipRoot.SetActive(false);
                var tooltip = _tooltipRoot.AddComponent<Tooltip>();
                SetField(tooltip, "canvasGroup", _tooltipRoot.GetComponent<CanvasGroup>());
                SetField(tooltip, "_transformToMove", _tooltipRoot.GetComponent<RectTransform>());
                var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(TooltipTextBlock));
                text.transform.SetParent(_tooltipRoot.transform);
                var block = text.GetComponent<TooltipTextBlock>();
                SetField(block, "label", text.GetComponent<TextMeshProUGUI>());
                SetField(tooltip, "textTemplate", block);
                var tooltips = new TooltipHandler(null, null);
                SetField(tooltips, "_tooltip", tooltip);
                var maps = new MapHandler(null, null, new LoggerSource());
                var registry = new MapRegistry();
                registry.SetMap(Map);
                SetField(maps, "_registry", registry);
                Updater = new FloorUpdater(null, tooltips, coordinator, _input, maps, null, null, null, Data.Database, Data.Edits);
                SetField(Updater, "_paintMode", FloorPaintMode.Docks);
                SetField(Updater, "_selectedFloor", Data.Floor);
                SetField(Updater, "_lastPillarSupport", Data.Pillar);
            }

            public void SetSlope(int x, int y, int level)
            {
                if (level < 0)
                {
                    Map[x + 1, y + 1].Cave.InitializeHeights(-20, 100);
                }
                else
                {
                    SetField(Map[x + 1, y + 1], "surfaceHeight", 30);
                }
            }

            public void SetOrientation(EntityOrientation orientation) => SetField(Updater, "_orientation", orientation);
            public void UseBraces()
            {
                SetField(Updater, "_dockSupportAuto", false);
                SetField(Updater, "_selectedDockSupport", Data.Brace);
            }

            public void TargetGround(int x, int y, int level)
            {
                SetField(_camera, "_level", level);
                _terrain.center = new Vector3(x * 4 + 2, -10, y * 4 + 2);
                Target(x, y, _terrain);
            }

            public void TargetFloor(Floor floor)
            {
                SetField(_camera, "_level", floor.Level);
                Target(floor.Tile.X, floor.Tile.Y, floor.GetComponent<Collider>());
            }

            public void TargetDock(Dock dock) => Target(dock.Tile.X, dock.Tile.Y, dock.GetComponent<Collider>());

            private void Target(int x, int y, Collider collider)
            {
                Physics.SyncTransforms();
                Assert.That(collider.Raycast(new Ray(new Vector3(x * 4 + 2, 100, y * 4 + 2), Vector3.down), out RaycastHit hit, 200), Is.True);
                typeof(MultiCamera).GetProperty(nameof(MultiCamera.CurrentRaycast)).SetValue(_camera, hit);
            }

            public void Press(MouseButton button)
            {
                InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(button));
                InputSystem.Update();
                Updater.Tick();
            }

            public void Release()
            {
                InputSystem.QueueStateEvent(_mouse, new MouseState());
                InputSystem.Update();
                Updater.Tick();
            }

            public void Dispose()
            {
                Updater.Disable();
                _input.Disable();
                Object.DestroyImmediate(_input.asset);
                InputSystem.RemoveDevice(_mouse);
                InputSystem.settings = _originalSettings;
                Object.DestroyImmediate(_testSettings);
                Object.DestroyImmediate(_cameraRoot);
                Object.DestroyImmediate(_tooltipRoot);
                Data.Dispose();
            }
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
