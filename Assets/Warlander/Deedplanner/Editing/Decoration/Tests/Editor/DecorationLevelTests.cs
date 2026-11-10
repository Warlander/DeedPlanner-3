using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Decorations;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Persistence;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Deedplanner.Rendering.Outline;
using Warlander.Deedplanner.Settings;
using Warlogic.Settings;
using Object = UnityEngine.Object;
using UnityInputSettings = UnityEngine.InputSystem.InputSettings;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class DecorationLevelTests
    {
        private DecorationFixture _fixture;
        private UnityInputSettings _originalInputSettings;
        private UnityInputSettings _testInputSettings;

        [SetUp]
        public void SetUp()
        {
            _originalInputSettings = InputSystem.settings;
            _testInputSettings = Object.Instantiate(_originalInputSettings);
            _testInputSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            InputSystem.settings = _testInputSettings;
            _fixture = new DecorationFixture();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture?.Dispose();
            InputSystem.settings = _originalInputSettings;
            Object.DestroyImmediate(_testInputSettings);
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(1, 0)]
        [TestCase(-2, -1)]
        public void RightClick_DeletesOnlyTargetLevelAndPreservesOtherLevelsThroughHistory(int level, int otherLevel)
        {
            _fixture.TargetLevel(level);
            Decoration other = _fixture.AddDecoration(otherLevel, new Vector2(2, 2));
            Decoration first = _fixture.AddDecoration(level, new Vector2(1, 1));
            Decoration second = _fixture.AddDecoration(level, new Vector2(3, 3));
            _fixture.Map.CommandManager.FinishAction();

            _fixture.MouseButton(MouseButton.Right);

            Assert.That(_fixture.Tile.GetDecorations(), Is.EquivalentTo(new[] { other }));
            _fixture.Map.CommandManager.Undo();
            Assert.That(_fixture.Tile.GetDecorations(), Is.EquivalentTo(new[] { other, first, second }));
            _fixture.Map.CommandManager.Redo();
            Assert.That(_fixture.Tile.GetDecorations(), Is.EquivalentTo(new[] { other }));
        }

        [Test]
        public void RightClick_RealGroundHitDeletesSurfaceTreeAndPreservesCaveObject()
        {
            _fixture.TargetGround();
            Decoration surface = _fixture.AddDecoration(0, new Vector2(2, 2));
            Decoration cave = _fixture.AddDecoration(-1, new Vector2(2, 2));
            _fixture.Map.CommandManager.FinishAction();

            _fixture.MouseButton(MouseButton.Right);

            Assert.That(_fixture.Tile.GetDecorations(), Is.EquivalentTo(new[] { cave }));
            _fixture.Map.CommandManager.Undo();
            Assert.That(_fixture.Tile.GetDecorations(), Is.EquivalentTo(new[] { surface, cave }));
        }

        [Test]
        public void LeftClick_RealGroundHitPlacesSurfaceObject()
        {
            _fixture.TargetGround();

            _fixture.MouseButton(MouseButton.Left);
            _fixture.ReleaseMouse();

            Assert.That(_fixture.Tile.GetDecorations().Single().Level, Is.Zero);
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(1, 0)]
        [TestCase(-2, -1)]
        public void DeleteKey_SkipsCloserObjectOnAnotherLevel(int level, int otherLevel)
        {
            _fixture.TargetLevel(level);
            Decoration other = _fixture.AddDecoration(otherLevel, new Vector2(2, 2));
            _fixture.AddDecoration(level, new Vector2(2.1f, 2));
            _fixture.Map.CommandManager.FinishAction();

            _fixture.DeleteKey();

            Assert.That(_fixture.Tile.GetDecorations(), Is.EquivalentTo(new[] { other }));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(1, 0)]
        [TestCase(-2, -1)]
        public void LeftClick_AllowsPlacementAtSamePositionOnAnotherLevel(int level, int otherLevel)
        {
            _fixture.TargetLevel(level);
            Decoration other = _fixture.AddDecoration(otherLevel, new Vector2(2, 2));
            _fixture.Map.CommandManager.FinishAction();

            _fixture.MouseButton(MouseButton.Left);
            _fixture.ReleaseMouse();

            Decoration[] objects = _fixture.Tile.GetDecorations().ToArray();
            Assert.That(objects, Has.Length.EqualTo(2));
            Assert.That(objects, Does.Contain(other));
            Assert.That(objects.Single(d => d != other).Level, Is.EqualTo(level));
        }

        [TestCase(-1)]
        [TestCase(0)]
        public void LeftClick_StillRejectsNearbyPlacementOnSameLevel(int level)
        {
            _fixture.TargetLevel(level);
            Decoration existing = _fixture.AddDecoration(level, new Vector2(2.1f, 2));
            _fixture.Map.CommandManager.FinishAction();

            _fixture.MouseButton(MouseButton.Left);
            _fixture.ReleaseMouse();

            Assert.That(_fixture.Tile.GetDecorations(), Is.EquivalentTo(new[] { existing }));
        }

        [Test]
        public void Hover_LevelChangeRemovesPreviousOutlinesAndOutlinesOnlyCurrentLevel()
        {
            Decoration surface = _fixture.AddDecoration(0, new Vector2(2, 2));
            Decoration cave = _fixture.AddDecoration(-1, new Vector2(2, 2));
            _fixture.TargetLevel(-1);
            _fixture.Updater.Tick();
            Assert.That(_fixture.Outlines.GetOutlinedObjectsSnapshot().Single().Renderers,
                Does.Contain(cave.Model.GetComponent<Renderer>()));

            _fixture.TargetLevel(0);
            _fixture.Updater.Tick();
            Assert.That(_fixture.Outlines.GetOutlinedObjectsSnapshot().Single().Renderers,
                Does.Contain(surface.Model.GetComponent<Renderer>()));
        }

        [Test]
        public void RightClick_HitFloorUsesFloorLevelInsteadOfCameraLevel()
        {
            _fixture.TargetFloor(1);
            Decoration surface = _fixture.AddDecoration(0, new Vector2(2, 2));
            _fixture.AddDecoration(1, new Vector2(2, 2));
            _fixture.Map.CommandManager.FinishAction();

            _fixture.MouseButton(MouseButton.Right);

            Assert.That(_fixture.Tile.GetDecorations(), Is.EquivalentTo(new[] { surface }));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(1, 0)]
        [TestCase(-2, -1)]
        public void Facade_OverlapCheckIgnoresOtherLevelsIncludingMirroredTarget(int level, int otherLevel)
        {
            _fixture.Symmetry.PlaceVerticalAxis(2);
            Decoration original = _fixture.AddDecoration(otherLevel, new Vector2(2, 2));
            Decoration mirrored = _fixture.Map[1, 0].SetDecoration(_fixture.Data, new Vector2(2, 2), 0, otherLevel);
            _fixture.Map.CommandManager.FinishAction();

            Decoration placed = _fixture.Facade.SetDecoration(0, 0, _fixture.Data, new Vector2(2, 2), 0, level);
            _fixture.Facade.FinishAction();

            Assert.That(placed, Is.Not.Null);
            Assert.That(_fixture.Tile.GetDecorations(), Does.Contain(original));
            Assert.That(_fixture.Map[1, 0].GetDecorations(), Does.Contain(mirrored));
            Assert.That(_fixture.Map[1, 0].GetDecorations().Count(d => d.Level == level), Is.EqualTo(1));
            _fixture.Map.CommandManager.Undo();
            Assert.That(_fixture.Tile.GetDecorations(), Is.EquivalentTo(new[] { original }));
            Assert.That(_fixture.Map[1, 0].GetDecorations(), Is.EquivalentTo(new[] { mirrored }));
            _fixture.Map.CommandManager.Redo();
            Assert.That(_fixture.Tile.GetDecorations().Count(), Is.EqualTo(2));
            Assert.That(_fixture.Map[1, 0].GetDecorations().Count(), Is.EqualTo(2));
        }

        [TestCase(-1)]
        [TestCase(0)]
        public void Facade_StillRejectsNearbyPlacementOnSameLevel(int level)
        {
            Decoration existing = _fixture.AddDecoration(level, new Vector2(2.1f, 2));

            Decoration placed = _fixture.Facade.SetDecoration(0, 0, _fixture.Data, new Vector2(2, 2), 0, level);

            Assert.That(placed, Is.Null);
            Assert.That(_fixture.Tile.GetDecorations(), Is.EquivalentTo(new[] { existing }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Facade_NeighborOverlapBlocksOnlySameLevel(bool sameLevel)
        {
            _fixture.Map[1, 0].SetDecoration(_fixture.Data, new Vector2(0.1f, 2), 0, sameLevel ? -1 : 0);

            Decoration placed = _fixture.Facade.SetDecoration(0, 0, _fixture.Data, new Vector2(3.9f, 2), 0, -1);

            Assert.That(placed == null, Is.EqualTo(sameLevel));
        }

        [TestCase(0.1f, 0)]
        [TestCase(2f, 1)]
        [TestCase(3.9f, 1)]
        public void Facade_CornerOnlySnapsToGameCorner(float localX, int expectedX)
        {
            DecorationData data = _fixture.CornerData("catseye");

            Decoration placed = _fixture.Facade.SetDecoration(0, 0, data, new Vector2(localX, 0.1f), 0, 0);

            Assert.That(placed, Is.Not.Null);
            Assert.That(placed.Tile.X, Is.EqualTo(expectedX));
            Assert.That(placed.Position, Is.EqualTo(Vector2.zero));
        }

        [TestCase(2, 0)]
        [TestCase(0, 1)]
        [TestCase(2, 1)]
        public void Facade_CornerOnlySupportsOuterMapCorner(int x, int y)
        {
            DecorationData data = _fixture.CornerData("catseye");

            Decoration placed = _fixture.Facade.SetDecoration(x, y, data, Vector2.zero, 0, 0);
            _fixture.Facade.FinishAction();

            Assert.That(placed, Is.Not.Null);
            Assert.That(placed.Tile, Is.SameAs(_fixture.Map[x, y]));
            Assert.That(placed.Position, Is.EqualTo(Vector2.zero));
            _fixture.Map.CommandManager.Undo();
            Assert.That(placed.Tile.GetDecorations(), Is.Empty);
            _fixture.Map.CommandManager.Redo();
            Assert.That(placed.Tile.GetDecorations(), Does.Contain(placed));
        }

        [TestCase("catseyeGreen")]
        [TestCase("catseyeBlue")]
        [TestCase("waystone")]
        public void Facade_CornerVariantsShareSlotAcrossAdjacentTiles(string otherId)
        {
            Decoration existing = _fixture.Facade.SetDecoration(0, 0, _fixture.CornerData("catseye"),
                new Vector2(4, 0), 0, 0);

            Decoration duplicate = _fixture.Facade.SetDecoration(1, 0, _fixture.CornerData(otherId), Vector2.zero, 0, 0);

            Assert.That(existing, Is.Not.Null);
            Assert.That(duplicate, Is.Null);
            Assert.That(_fixture.Map[1, 0].GetDecorations().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Updater_CornerMidpointUsesGameTieBreakWithGridDisabled()
        {
            _fixture.SelectDecoration(_fixture.CornerData("catseye"));

            _fixture.MouseButton(MouseButton.Left);
            _fixture.ReleaseMouse();

            Decoration placed = _fixture.Map.Cast<Tile>().SelectMany(tile => tile.GetDecorations()).Single();
            Assert.That(placed.Tile, Is.SameAs(_fixture.Map[1, 1]));
            Assert.That(placed.Position, Is.EqualTo(Vector2.zero));
        }

        [TestCase(0f)]
        [TestCase(0.75f)]
        public void Facade_FixedRotationOverridesInputAndMirroring(float fixedRotation)
        {
            _fixture.Symmetry.PlaceVerticalAxis(2);
            DecorationData data = _fixture.CornerData("catseye", fixedRotation);

            Decoration placed = _fixture.Facade.SetDecoration(0, 0, data, Vector2.zero, 1.2f, 0);

            Assert.That(placed.Rotation, Is.EqualTo(fixedRotation));
            Assert.That(_fixture.Map[2, 0].GetDecorations().Single().Rotation, Is.EqualTo(fixedRotation));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void Facade_CornerSlotIsIndependentOnSeparateLevels(int otherLevel)
        {
            Decoration surface = _fixture.Facade.SetDecoration(1, 0, _fixture.CornerData("catseye"), Vector2.zero, 0, 0);

            Decoration other = _fixture.Facade.SetDecoration(1, 0, _fixture.CornerData("catseyeGreen"),
                Vector2.zero, 0, otherLevel);

            Assert.That(surface, Is.Not.Null);
            Assert.That(other, Is.Not.Null);
            Assert.That(_fixture.Map[1, 0].GetDecorations().Count(), Is.EqualTo(2));
        }

        [Test]
        public void Facade_OuterCornerRejectsAnotherVariantAndCanBeRemoved()
        {
            Decoration existing = _fixture.Facade.SetDecoration(2, 1, _fixture.CornerData("catseye"), Vector2.zero, 0, 0);

            Decoration duplicate = _fixture.Facade.SetDecoration(2, 1, _fixture.CornerData("catseyeBlue"), Vector2.zero, 0, 0);

            Assert.That(existing, Is.Not.Null);
            Assert.That(duplicate, Is.Null);
            Assert.That(_fixture.Map[2, 1].GetDecorations().Count(), Is.EqualTo(1));
            _fixture.Facade.SetDecoration(2, 1, null, Vector2.zero, 0, 0);
            Assert.That(_fixture.Map[2, 1].GetDecorations(), Is.Empty);
        }

        [Test]
        public void Facade_OrdinaryDecorationKeepsFreePositionAndRotation()
        {
            Vector2 position = new Vector2(1.3f, 2.1f);

            Decoration placed = _fixture.Facade.SetDecoration(0, 0, _fixture.Data, position, 0.8f, 0);

            Assert.That(placed.Position, Is.EqualTo(position));
            Assert.That(placed.Rotation, Is.EqualTo(0.8f));
        }

        [Test]
        public void Updater_FixedRotationIgnoresDragAndKeepsPreviewNorthFacing()
        {
            _fixture.SelectDecoration(_fixture.CornerData("catseye", 0));
            _fixture.MouseButton(MouseButton.Left);

            _fixture.MovePointer(new Vector2(100, 0));
            _fixture.MouseButton(MouseButton.Left);

            Assert.That(_fixture.PreviewRotation, Is.Zero);
            _fixture.ReleaseMouse();
            Decoration placed = _fixture.Map.Cast<Tile>().SelectMany(tile => tile.GetDecorations()).Single();
            Assert.That(placed.Rotation, Is.Zero);
        }
        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private sealed class DecorationFixture : IDisposable
        {
            private readonly GameObject _modelRoot;
            private readonly GameObject _cameraRoot;
            private readonly GameObject _ghost;
            private readonly SharedMaterials _materials;
            private readonly MultiCamera _camera;
            private readonly BoxCollider _overlay;
            private readonly DPInput _input;
            private readonly Mouse _mouse;
            private readonly Keyboard _keyboard;

            public Map Map { get; }
            public Tile Tile => Map[0, 0];
            public DecorationData Data { get; }
            public MapEditFacade Facade { get; }
            public SymmetrySession Symmetry { get; }
            public DecorationUpdater Updater { get; }
            public OutlineCoordinator Outlines { get; } = new OutlineCoordinator();

            public DecorationFixture()
            {
                Map = new GameObject("Decoration level test").AddComponent<Map>();
                SetField(Map, "_mapRenderSettingsRetriever", new MapRenderSettings());
                SetField(Map, "<Ground>k__BackingField", Map.gameObject.AddComponent<GroundMesh>());
                var grid = new MapTileGrid(2, 1);
                SetField(Map, "_tileGrid", grid);
                var solid = new CaveData(null, "Stone", "sw", Array.Empty<string[]>(), true, true, false);
                var open = new CaveData(null, "Open", "o", Array.Empty<string[]>(), false, true, false);
                var database = new Database();
                database.AddCave(solid);
                for (int x = 0; x <= 2; x++)
                {
                    for (int y = 0; y <= 1; y++)
                    {
                        var tile = new Tile(Map, x, y, null, database, null, new GroundResolver(), null);
                        tile.Cave.InitializeHeights(-30, 30);
                        tile.Cave.InitializeTerrain(open);
                        grid.SetTile(x, y, tile);
                    }
                }
                SetField(Map, "_roofCalculator", new MapRoofCalculator(Map));
                SetField(Map, "<Caves>k__BackingField", new CaveMap(2, 1, (x, y) => Map[x, y].Cave, solid));
                SetField(Map, "_dockCollection", new MapDockCollection(Map, null));
                var renderer = new MapLevelRenderer();
                renderer.Initialize(new[] { Root("Surface 0"), Root("Surface 1") },
                    new[] { Root("Cave -1"), Root("Cave -2") }, Root("Surface grid"), Root("Cave grid"), Root("Cave shell"), null, null);
                SetField(Map, "_levelRenderer", renderer);
                Map.CommandManager.ForgetAction();
                var logger = new LoggerSource();
                ModelHandle model = new WurmAssetFacade(logger).GetModel("Decoration level model", 0);
                _modelRoot = new GameObject("Decoration level model root");
                var originalModel = new GameObject("Decoration level model", typeof(MeshRenderer));
                originalModel.transform.SetParent(_modelRoot.transform);
                SetField(model, "_modelRoot", _modelRoot);
                SetField(model, "_originalModel", originalModel);
                Data = new DecorationData(model, "Object", "object", Array.Empty<string[]>(), "tree",
                    false, false, false, true, false, null);
                var handler = new MapHandler(null, null, logger);
                var registry = (MapRegistry)typeof(MapHandler).GetField("_registry", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(handler);
                registry.SetMap(Map);
                Symmetry = new SymmetrySession(handler);
                Facade = new MapEditFacade(handler, Symmetry, null, null, null);
                _cameraRoot = new GameObject("Decoration level camera");
                _cameraRoot.SetActive(false);
                _camera = _cameraRoot.AddComponent<MultiCamera>();
                SetField(_camera, "cameraMode", CameraMode.Top);
                var coordinator = _cameraRoot.AddComponent<CameraCoordinator>();
                SetField(coordinator, "_cameras", new[] { _camera });
                GameObject overlay = Root("Overlay").gameObject;
                overlay.AddComponent<OverlayMesh>();
                _overlay = overlay.AddComponent<BoxCollider>();
                _overlay.center = new Vector3(2, 0, 2);
                _overlay.size = new Vector3(4, 0.1f, 4);
                _input = new DPInput();
                _mouse = InputSystem.AddDevice<Mouse>();
                _keyboard = InputSystem.AddDevice<Keyboard>();
                _input.devices = new InputDevice[] { _mouse, _keyboard };
                _input.Enable();
                _materials = ScriptableObject.CreateInstance<SharedMaterials>();
                EditingSettings settings = DeedPlannerSettings.Create(logger.Create(DeedPlannerSettings.Category), new MemoryStore()).Editing;
                settings.DecorationSnapToGrid = false;
                Updater = new DecorationUpdater(null, settings, coordinator, _input, handler, Outlines,
                    _materials, null, logger, null, null, Facade);
                _ghost = new GameObject("Decoration level ghost");
                SetField(Updater, "_selectedDecoration", Data);
                SetField(Updater, "_lastGhostData", Data);
                SetField(Updater, "_ghostObject", _ghost);
                TargetLevel(0);
            }

            public Decoration AddDecoration(int level, Vector2 position) => Tile.SetDecoration(Data, position, 0, level);

            public float PreviewRotation => _ghost.transform.eulerAngles.y;

            public DecorationData CornerData(string shortName, float? fixedRotation = null)
            {
                return new DecorationData(Data.Model, shortName, shortName, Array.Empty<string[]>(), "corner",
                    false, true, false, false, false, null, fixedRotation);
            }

            public void SelectDecoration(DecorationData data)
            {
                SetField(Updater, "_selectedDecoration", data);
                SetField(Updater, "_lastGhostData", data);
            }

            public void MovePointer(Vector2 position)
            {
                typeof(MultiCamera).GetProperty(nameof(MultiCamera.MousePosition)).SetValue(_camera, position);
            }

            public void TargetLevel(int level)
            {
                SetField(_camera, "_level", level);
                SetRaycast(_overlay);
            }

            public void TargetFloor(int level)
            {
                var floorData = new FloorData(Data.Model, "Floor", "floor", Array.Empty<string[]>(), false, false, null);
                Floor floor = Tile.SetFloor(floorData, EntityOrientation.Up, level);
                var collider = floor.GetComponent<BoxCollider>();
                SetRaycast(collider);
            }

            public void TargetGround()
            {
                SetField(_camera, "_level", 0);
                BoxCollider collider = Map.Ground.gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(2, 0, 2);
                collider.size = new Vector3(4, 0.1f, 4);
                SetRaycast(collider);
            }

            private void SetRaycast(BoxCollider collider)
            {
                Physics.SyncTransforms();
                var origin = collider.transform.TransformPoint(collider.center) + Vector3.up * 10f;
                Assert.That(collider.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit, 20f), Is.True);
                typeof(MultiCamera).GetProperty(nameof(MultiCamera.CurrentRaycast)).SetValue(_camera, hit);
            }

            public void MouseButton(MouseButton button)
            {
                InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(button));
                InputSystem.Update();
                Updater.Tick();
            }

            public void ReleaseMouse()
            {
                InputSystem.QueueStateEvent(_mouse, new MouseState());
                InputSystem.Update();
                Updater.Tick();
            }

            public void DeleteKey()
            {
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Delete));
                InputSystem.Update();
                Updater.Tick();
            }

            private Transform Root(string name)
            {
                var root = new GameObject(name).transform;
                root.SetParent(Map.transform);
                return root;
            }

            public void Dispose()
            {
                _input.Disable();
                Object.DestroyImmediate(_input.asset);
                InputSystem.RemoveDevice(_mouse);
                InputSystem.RemoveDevice(_keyboard);
                Object.DestroyImmediate(_ghost);
                Object.DestroyImmediate(_cameraRoot);
                Object.DestroyImmediate(Map.gameObject);
                Object.DestroyImmediate(_modelRoot);
                Object.DestroyImmediate(_materials);
            }
        }

        private sealed class GroundResolver : IGroundDataResolver
        {
            private readonly GroundData _ground = new GroundData("Grass", "gr", Array.Empty<string[]>(), null, null, false);
            public GroundData Resolve(string shortName) => _ground;
        }

        private sealed class MemoryStore : ISettingsStore
        {
            public bool TryLoad(string key, out string value) { value = null; return false; }
            public void Save(string key, string value) { }
        }
    }
}
