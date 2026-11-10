using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Domain.Entities.Walls;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Persistence;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Ui.Tooltips;
using Warlogic.Settings;
using Object = UnityEngine.Object;
using UnityInputSettings = UnityEngine.InputSystem.InputSettings;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class CaveBuildingTests
    {
        private BuildingFixture _fixture;
        private UnityInputSettings _originalSettings;
        private UnityInputSettings _testSettings;

        [SetUp]
        public void SetUp()
        {
            _originalSettings = InputSystem.settings;
            _testSettings = Object.Instantiate(_originalSettings);
            _testSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            InputSystem.settings = _testSettings;
            _fixture = new BuildingFixture();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture?.Dispose();
            InputSystem.settings = _originalSettings;
            Object.DestroyImmediate(_testSettings);
        }

        [TestCase(-2, -1)]
        [TestCase(-7, -6)]
        [TestCase(0, 1)]
        [TestCase(1, 0)]
        public void FloorPaintingUsesActiveCaveStoreyAndRetainsSurfaceHitTargeting(int activeLevel, int hitLevel)
        {
            Floor existing = _fixture.TargetFloor(activeLevel, hitLevel);
            _fixture.Press(_fixture.Floors, MouseButton.Left);
            _fixture.Release(_fixture.Floors);

            int expected = activeLevel < 0 ? activeLevel : hitLevel;
            Assert.That(((Floor)_fixture.Tile.GetTileContent(expected)).Data, Is.SameAs(_fixture.NewFloor));
            if (activeLevel < 0)
            {
                Assert.That(_fixture.Tile.GetTileContent(hitLevel), Is.SameAs(existing));
                _fixture.Map.CommandManager.Undo();
                Assert.That(_fixture.Tile.GetTileContent(activeLevel), Is.Null);
                Assert.That(_fixture.Tile.GetTileContent(hitLevel), Is.SameAs(existing));
                _fixture.Map.CommandManager.Redo();
                Assert.That(((Floor)_fixture.Tile.GetTileContent(activeLevel)).Data, Is.SameAs(_fixture.NewFloor));
            }
        }

        [Test]
        public void FloorDeletionAtUpperCaveStoreyPreservesTheHitFloorBelow()
        {
            Floor existing = _fixture.TargetFloor(-2, -1);

            _fixture.Press(_fixture.Floors, MouseButton.Right);
            _fixture.Release(_fixture.Floors);

            Assert.That(_fixture.Tile.GetTileContent(-1), Is.SameAs(existing));
        }

        [TestCase(-2, -1)]
        [TestCase(-7, -6)]
        [TestCase(1, 0)]
        public void WallPaintingBuildsTheUpperStoreyWithoutReplacingTheWallBelow(int activeLevel, int hitLevel)
        {
            Wall existing = _fixture.TargetWall(activeLevel, hitLevel);

            _fixture.Press(_fixture.Walls, MouseButton.Left);
            _fixture.Release(_fixture.Walls);

            Assert.That(_fixture.Tile.GetVerticalWallOrFence(activeLevel), Is.Not.Null);
            Assert.That(_fixture.Tile.GetVerticalWallOrFence(hitLevel), Is.SameAs(existing));
        }

        [Test]
        public void WallPaintingAboveCaveFloorUsesActiveStorey()
        {
            _fixture.TargetFloor(-2, -1);
            _fixture.TargetFloorBorder();

            _fixture.Press(_fixture.Walls, MouseButton.Left);
            _fixture.Release(_fixture.Walls);

            Assert.That(_fixture.Tile.GetVerticalWallOrFence(-2), Is.Not.Null);
            Assert.That(_fixture.Tile.GetVerticalWallOrFence(-1), Is.Null);
        }

        [TestCase(CaveEdge.South, false)]
        [TestCase(CaveEdge.West, false)]
        [TestCase(CaveEdge.North, false)]
        [TestCase(CaveEdge.East, false)]
        [TestCase(CaveEdge.South, true)]
        [TestCase(CaveEdge.West, true)]
        [TestCase(CaveEdge.North, true)]
        [TestCase(CaveEdge.East, true)]
        public void FacadeBuildsVisibleBoundaryWallsAndFencesWithHistory(CaveEdge edge, bool fence)
        {
            _fixture.PlaceWall(edge, fence);
            _fixture.Facade.FinishAction();

            Wall wall = _fixture.GetWall(edge);
            Assert.That(wall, Is.Not.Null);
            Assert.That(wall.gameObject.activeSelf, Is.True);
            _fixture.Map.CommandManager.Undo();
            Assert.That(_fixture.GetWall(edge), Is.Null);
            _fixture.Map.CommandManager.Redo();
            Assert.That(_fixture.GetWall(edge), Is.SameAs(wall));
            Assert.That(wall.gameObject.activeSelf, Is.True);
        }

        [TestCase(CaveEdge.South)]
        [TestCase(CaveEdge.West)]
        [TestCase(CaveEdge.North)]
        [TestCase(CaveEdge.East)]
        public void WallPaintingUsesOpenCellEdgeWhenRayHitsSolidRock(CaveEdge edge)
        {
            _fixture.TargetRockFace(edge);

            _fixture.Press(_fixture.Walls, MouseButton.Left);
            _fixture.Release(_fixture.Walls);

            Assert.That(_fixture.GetWall(edge), Is.Not.Null);
            Assert.That(_fixture.GetWall(edge).gameObject.activeSelf, Is.True);
        }

        [TestCase(CaveEdge.North)]
        [TestCase(CaveEdge.East)]
        public void BoundaryWallVisibilityFollowsAdjacentCaveTerrainAndHistory(CaveEdge edge)
        {
            _fixture.PlaceWall(edge, false);
            _fixture.Facade.FinishAction();
            Wall wall = _fixture.GetWall(edge);
            Assert.That(wall, Is.Not.Null);

            _fixture.CaveEditor.SetTerrain(1, 1, _fixture.Stone, CaveOccupiedCellPolicy.PreserveAndHide);

            Assert.That(wall.gameObject.activeSelf, Is.False);
            _fixture.Map.CommandManager.Undo();
            Assert.That(wall.gameObject.activeSelf, Is.True);
            _fixture.Map.CommandManager.Redo();
            Assert.That(wall.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void MirroringAcrossRoomBuildsAllFourBoundaryWalls()
        {
            _fixture.Symmetry.PlaceHorizontalAxis(3);
            _fixture.Symmetry.PlaceVerticalAxis(3);

            _fixture.PlaceWall(CaveEdge.South, false);
            _fixture.PlaceWall(CaveEdge.West, false);

            foreach (CaveEdge edge in Enum.GetValues(typeof(CaveEdge)))
            {
                Assert.That(_fixture.GetWall(edge), Is.Not.Null, edge.ToString());
                Assert.That(_fixture.GetWall(edge).gameObject.activeSelf, Is.True, edge.ToString());
            }
        }

        [Test]
        public void CaveWallPlacementRejectsSolidEdgesAndAcceptsOpenMapBoundary()
        {
            _fixture.Facade.SetVerticalWall(0, 0, _fixture.Wall, false, -1);
            Assert.That(_fixture.Map[0, 0].GetVerticalWallOrFence(-1), Is.Null);
            _fixture.Map[2, 1].Cave.InitializeTerrain(_fixture.Open);

            _fixture.Facade.SetVerticalWall(3, 1, _fixture.Wall, false, -1);

            Wall wall = _fixture.Map[3, 1].GetVerticalWallOrFence(-1);
            Assert.That(wall, Is.Not.Null);
            Assert.That(wall.gameObject.activeSelf, Is.True);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private sealed class BuildingFixture : IDisposable
        {
            private readonly GameObject _modelRoot;
            private readonly GameObject _cameraRoot;
            private readonly GameObject _tooltipRoot;
            private readonly MultiCamera _camera;
            private readonly DPInput _input;
            private readonly Mouse _mouse;
            private readonly FloorData _oldFloor;
            private readonly WallData _fence;
            public Map Map { get; }
            public Tile Tile => Map[1, 1];
            public FloorData NewFloor { get; }
            public WallData Wall { get; }
            public CaveData Stone { get; } = new CaveData(null, "Stone", "sw", Array.Empty<string[]>(), true, true, false);
            public CaveData Open { get; } = new CaveData(null, "Open", "sfl", Array.Empty<string[]>(), false, true, false);
            public MapEditFacade Facade { get; }
            public SymmetrySession Symmetry { get; }
            public FloorUpdater Floors { get; }
            public WallUpdater Walls { get; }
            public CaveEditor CaveEditor { get; }

            public BuildingFixture()
            {
                Map = new GameObject("Cave building test").AddComponent<Map>();
                SetField(Map, "_mapRenderSettingsRetriever", new MapRenderSettings());
                SetField(Map, "<Ground>k__BackingField", Map.gameObject.AddComponent<GroundMesh>());
                var grid = new MapTileGrid(3, 3);
                SetField(Map, "_tileGrid", grid);
                var database = new Database();
                database.AddCave(Stone);
                database.AddCave(Open);
                for (int x = 0; x <= 3; x++)
                {
                    for (int y = 0; y <= 3; y++)
                    {
                        var tile = new Tile(Map, x, y, null, database, null, new GroundResolver(), null);
                        tile.Cave.InitializeHeights(-30, 210);
                        grid.SetTile(x, y, tile);
                    }
                }
                Tile.Cave.InitializeTerrain(Open);
                SetField(Map, "_roofCalculator", new MapRoofCalculator(Map));
                SetField(Map, "<Caves>k__BackingField", new CaveMap(3, 3, (x, y) => Map[x, y].Cave, Stone));
                SetField(Map, "_dockCollection", new MapDockCollection(Map, null));
                var surfaceRoots = new Transform[7];
                var caveRoots = new Transform[7];
                for (int i = 0; i < 7; i++) { surfaceRoots[i] = Root("Surface " + i); caveRoots[i] = Root("Cave " + i); }
                var renderer = new MapLevelRenderer();
                renderer.Initialize(surfaceRoots, caveRoots, Root("Surface grid"), Root("Cave grid"), Root("Cave shell"), null, null);
                SetField(Map, "_levelRenderer", renderer);
                Map.CommandManager.ForgetAction();
                var logger = new LoggerSource();
                ModelHandle model = new WurmAssetFacade(logger).GetModel("Cave building model", 0);
                _modelRoot = new GameObject("Cave building model root");
                var originalModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                originalModel.name = "Cave building model";
                Object.DestroyImmediate(originalModel.GetComponent<Collider>());
                originalModel.transform.SetParent(_modelRoot.transform);
                SetField(model, "_modelRoot", _modelRoot);
                SetField(model, "_originalModel", originalModel);
                _oldFloor = new FloorData(model, "Old", "old", Array.Empty<string[]>(), false, false, null);
                NewFloor = new FloorData(model, "New", "new", Array.Empty<string[]>(), false, false, null);
                Wall = new WallData(model, model, "Wall", "wall", Array.Empty<string[]>(), Color.white, 1, true, false, false, null, null);
                _fence = new WallData(model, model, "Fence", "fence", Array.Empty<string[]>(), Color.white, 1, false, false, true, null, null);
                var handler = new MapHandler(null, null, logger);
                var registry = (MapRegistry)typeof(MapHandler).GetField("_registry", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(handler);
                registry.SetMap(Map);
                Symmetry = new SymmetrySession(handler);
                Facade = new MapEditFacade(handler, Symmetry, null, null, null);
                var mutationType = typeof(Map).Assembly.GetType("Warlander.Deedplanner.Caves.MapCaveMutationTarget");
                CaveEditor = new CaveEditor((ICaveMutationTarget)Activator.CreateInstance(mutationType, new object[] { Map }), new CaveDataResolver(database));
                _cameraRoot = new GameObject("Cave building camera");
                _cameraRoot.SetActive(false);
                _camera = _cameraRoot.AddComponent<MultiCamera>();
                var coordinator = _cameraRoot.AddComponent<CameraCoordinator>();
                SetField(coordinator, "_cameras", new[] { _camera });
                _input = new DPInput();
                _mouse = InputSystem.AddDevice<Mouse>();
                _input.devices = new InputDevice[] { _mouse };
                _input.Enable();
                var settings = DeedPlannerSettings.Create(logger.Create(DeedPlannerSettings.Category), new MemoryStore()).Editing;
                _tooltipRoot = new GameObject("Tooltip", typeof(RectTransform), typeof(CanvasGroup));
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
                Floors = new FloorUpdater(null, tooltips, coordinator, _input, handler, null, null, null, null, Facade);
                SetField(Floors, "_selectedFloor", NewFloor);
                Walls = new WallUpdater(null, settings, coordinator, _input, handler, null, null, null, Facade, tooltips);
                SetField(Walls, "_selectedWall", Wall);
            }

            public Floor TargetFloor(int activeLevel, int hitLevel)
            {
                Floor floor = Tile.SetFloor(_oldFloor, EntityOrientation.Up, hitLevel);
                Map.CommandManager.FinishAction();
                SetField(_camera, "_level", activeLevel);
                Physics.SyncTransforms();
                Assert.That(floor.GetComponent<Collider>().Raycast(new Ray(new Vector3(6, 100, 6), Vector3.down), out RaycastHit hit, 200), Is.True);
                typeof(MultiCamera).GetProperty("CurrentRaycast").SetValue(_camera, hit);
                return floor;
            }

            public void TargetRockFace(CaveEdge edge)
            {
                var chunk = Root("Cave chunk").gameObject.AddComponent<CaveChunk>();
                chunk.Initialize(0, 0, 3, 3, null);
                chunk.RebuildCollider(new CaveTopologyBuilder(Map.Caves, new TextureIndex(), Stone, 3, 3));
                Vector3 direction = edge == CaveEdge.South ? Vector3.back : edge == CaveEdge.North ? Vector3.forward
                    : edge == CaveEdge.East ? Vector3.right : Vector3.left;
                Physics.SyncTransforms();
                Assert.That(chunk.GetComponent<Collider>().Raycast(new Ray(new Vector3(6, 0, 6), direction), out RaycastHit hit, 4), Is.True);
                Assert.That(new CaveHitResolver().TryResolve(chunk, hit.triangleIndex, out CaveHit caveHit), Is.True);
                Assert.That(caveHit.Edge, Is.EqualTo(edge));
                Assert.That(Map.Caves.IsOpen(caveHit.CellX, caveHit.CellY), Is.False);
                SetField(_camera, "_level", -1);
                typeof(MultiCamera).GetProperty("CurrentRaycast").SetValue(_camera, hit);
                typeof(MultiCamera).GetProperty("CurrentCaveHit").SetValue(_camera, caveHit);
                typeof(MultiCamera).GetProperty("HasCurrentCaveHit").SetValue(_camera, true);
            }

            public Wall TargetWall(int activeLevel, int hitLevel)
            {
                Wall wall = Tile.SetVerticalWall(Wall, false, hitLevel);
                Map.CommandManager.FinishAction();
                SetField(_camera, "_level", activeLevel);
                Physics.SyncTransforms();
                Collider collider = wall.GetComponent<Collider>();
                Assert.That(collider.Raycast(new Ray(collider.bounds.center + Vector3.up * 100, Vector3.down), out RaycastHit hit, 200), Is.True);
                typeof(MultiCamera).GetProperty("CurrentRaycast").SetValue(_camera, hit);
                return wall;
            }

            public void TargetFloorBorder()
            {
                var floor = (Floor)Tile.GetTileContent(-1);
                Assert.That(floor.GetComponent<Collider>().Raycast(new Ray(new Vector3(4.1f, 100, 6), Vector3.down), out RaycastHit hit, 200), Is.True);
                typeof(MultiCamera).GetProperty("CurrentRaycast").SetValue(_camera, hit);
            }

            public void PlaceWall(CaveEdge edge, bool fence)
            {
                WallData data = fence ? _fence : Wall;
                if (edge == CaveEdge.South || edge == CaveEdge.North)
                    Facade.SetHorizontalWall(1, edge == CaveEdge.North ? 2 : 1, data, false, -1);
                else
                    Facade.SetVerticalWall(edge == CaveEdge.East ? 2 : 1, 1, data, false, -1);
            }

            public Wall GetWall(CaveEdge edge) => edge == CaveEdge.South || edge == CaveEdge.North
                ? Map[1, edge == CaveEdge.North ? 2 : 1].GetHorizontalWallOrFence(-1)
                : Map[edge == CaveEdge.East ? 2 : 1, 1].GetVerticalWallOrFence(-1);

            public void Press(IUpdater updater, MouseButton button)
            {
                InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(button));
                InputSystem.Update();
                updater.Tick();
            }

            public void Release(IUpdater updater)
            {
                InputSystem.QueueStateEvent(_mouse, new MouseState());
                InputSystem.Update();
                updater.Tick();
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
                foreach (var wall in Map.GetComponentsInChildren<Wall>(true))
                {
                    var mesh = (Mesh)typeof(Wall).GetField("boundsMesh", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(wall);
                    SetField(wall, "boundsMesh", null);
                    Object.DestroyImmediate(mesh);
                }
                Object.DestroyImmediate(_cameraRoot);
                Object.DestroyImmediate(_tooltipRoot);
                Object.DestroyImmediate(Map.gameObject);
                Object.DestroyImmediate(_modelRoot);
            }
        }

        private sealed class TextureIndex : ICaveTextureIndex
        {
            public int GetIndex(CaveData terrain) => 0;
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
