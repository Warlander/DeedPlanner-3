using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Persistence;
using Warlander.Deedplanner.Settings;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class HeightSelectionTests
    {
        [TestCase(0, 0)]
        [TestCase(-4, 0)]
        [TestCase(0, -4)]
        [TestCase(400, 0)]
        [TestCase(0, 400)]
        public void MissedRayDoesNotSelectHeightHandles(float x, float z)
        {
            var updater = new HeightUpdater(null, null, null, null, null, null, null, null, null);
            var raycast = new RaycastHit { point = new Vector3(x, 0, z) };
            MethodInfo select = typeof(HeightUpdater).GetMethod("UpdateHoveredHandlesSimpleSelection",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var handles = (IList)select.Invoke(updater, new object[] { raycast });

            Assert.That(handles, Is.Empty);
        }

        [Test]
        public void AreaSelectionKeepsWorldOrigin(
            [Values(0, 1, 2)] int realm,
            [Values(CameraMode.Top, CameraMode.Isometric, CameraMode.Perspective)] CameraMode cameraMode,
            [Values(false, true)] bool zoom,
            [Values(false, true)] bool missedRay)
        {
            using var fixture = new SelectionFixture(realm, cameraMode);
            Vector3 origin = fixture.BeginSelection(missedRay);
            Vector2 pixelOrigin = fixture.Project(origin);
            fixture.MovePointer(new Vector3(22, origin.y, 22));
            fixture.Select();

            if (zoom)
            {
                fixture.Camera.AttachedCamera.orthographicSize *= 2;
                fixture.Camera.AttachedCamera.fieldOfView = 80;
            }
            else
            {
                fixture.Camera.transform.position += new Vector3(80, 0, 80);
            }

            List<HeightmapHandle> handles = fixture.Select();
            Vector2 projectedOrigin = fixture.Project(origin);
            Vector2 rectangleOrigin = fixture.Camera.SelectionBoxPosition;
            Vector2 rectangleEnd = rectangleOrigin + fixture.Camera.SelectionBoxSize;
            Vector2 expectedMin = Vector2.Min(projectedOrigin, fixture.Camera.MousePosition);
            Vector2 expectedMax = Vector2.Max(projectedOrigin, fixture.Camera.MousePosition);
            Assert.That(Vector2.Distance(rectangleOrigin, expectedMin), Is.LessThan(0.02f));
            Assert.That(Vector2.Distance(rectangleEnd, expectedMax), Is.LessThan(0.02f));
            Assert.That(handles.Exists(handle => handle.TileCoords == new Vector2Int(2, 2)), Is.True,
                "Camera movement must not discard map vertices near the original corner.");
            Assert.That(Vector2.Distance((Vector2)GetField(fixture.Updater, "dragStartPos"), pixelOrigin), Is.LessThan(0.02f),
                "Height manipulation must retain its original pixel position.");
            if (!zoom)
            {
                Assert.That(projectedOrigin.x, Is.LessThan(0), "The origin must remain valid offscreen.");
            }
        }

        private static object GetField(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class SelectionFixture : IDisposable
        {
            private readonly GameObject _mapRoot;
            private readonly GameObject _cameraRoot;
            private readonly Map _map;
            private readonly DPInput _input;
            private readonly Mouse _mouse;
            private readonly UnityEngine.InputSystem.InputSettings _originalSettings;
            private readonly UnityEngine.InputSystem.InputSettings _testSettings;
            private readonly BoxCollider _collider;
            private readonly float _height;
            public MultiCamera Camera { get; }
            public HeightUpdater Updater { get; }

            public SelectionFixture(int realm, CameraMode cameraMode)
            {
                _originalSettings = InputSystem.settings;
                _testSettings = Object.Instantiate(_originalSettings);
                _testSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
                InputSystem.settings = _testSettings;
                _input = new DPInput();
                _mouse = InputSystem.AddDevice<Mouse>();
                _input.devices = new InputDevice[] { _mouse };
                _input.Enable();
                _height = realm == 0 ? 0 : realm == 1 ? -4 : -1;

                _mapRoot = new GameObject("Height selection map");
                _map = _mapRoot.AddComponent<Map>();
                SetField(_map, "_mapRenderSettingsRetriever", new MapRenderSettings());
                SetField(_map, "<Ground>k__BackingField", _mapRoot.AddComponent<GroundMesh>());
                SetField(_map, "_dockCollection", new MapDockCollection(_map, null));
                var tiles = new MapTileGrid(12, 12);
                SetField(_map, "_tileGrid", tiles);
                var database = new Database();
                database.AddCave(new CaveData(null, "Stone", "sw", Array.Empty<string[]>(), true, true, false));
                var handles = new HeightmapHandle[13, 13];
                for (int x = 0; x <= 12; x++)
                {
                    for (int y = 0; y <= 12; y++)
                    {
                        var tile = new Tile(_map, x, y, null, database, null, new GroundResolver(), null);
                        tile.Cave.InitializeHeights(-40, 30);
                        tiles.SetTile(x, y, tile);
                        handles[x, y] = new HeightmapHandle(new Vector2Int(x, y), (int)(_height * 10));
                    }
                }
                var grid = _mapRoot.AddComponent<GridMesh>();
                SetField(grid, "heightmapHandles", handles);
                SetField(_map, "<SurfaceGridMesh>k__BackingField", grid);
                SetField(_map, "<CaveGridMesh>k__BackingField", grid);
                _collider = _mapRoot.AddComponent<BoxCollider>();
                _collider.center = new Vector3(24, _height - 0.05f, 24);
                _collider.size = new Vector3(48, 0.1f, 48);

                var handler = new MapHandler(null, null, new LoggerSource());
                ((MapRegistry)GetField(handler, "_registry")).SetMap(_map);
                _cameraRoot = new GameObject("Height selection camera");
                _cameraRoot.SetActive(false);
                Camera = _cameraRoot.AddComponent<MultiCamera>();
                var attached = _cameraRoot.GetComponent<Camera>();
                typeof(MultiCamera).GetProperty(nameof(MultiCamera.AttachedCamera)).SetValue(Camera, attached);
                attached.orthographic = cameraMode != CameraMode.Perspective;
                attached.orthographicSize = 30;
                attached.aspect = 1;
                attached.fieldOfView = 60;
                Camera.transform.rotation = Quaternion.Euler(cameraMode == CameraMode.Top ? 90 : 60, 0, 0);
                Camera.transform.position = new Vector3(24, _height, 24) - Camera.transform.forward * 80;
                SetField(Camera, "_level", realm == 0 ? 0 : -1);
                SetField(Camera, "_input", _input);
                var screen = new GameObject("Height selection screen", typeof(RectTransform));
                screen.transform.SetParent(_cameraRoot.transform);
                screen.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 800);
                SetField(Camera, "screen", screen);
                var box = new GameObject("Height selection box", typeof(RectTransform));
                box.transform.SetParent(screen.transform);
                SetField(Camera, "selectionBox", box.GetComponent<RectTransform>());
                var coordinator = _cameraRoot.AddComponent<CameraCoordinator>();
                SetField(coordinator, "_cameras", new[] { Camera });
                Updater = new HeightUpdater(null, null, null, coordinator, _input, handler, null, null, null);
                SetField(Updater, "_caveHeightMode", realm == 2 ? CaveHeightMode.Clearance : CaveHeightMode.Floor);
            }

            public Vector2 Project(Vector3 point) => (Vector2)Camera.AttachedCamera.WorldToViewportPoint(point) * 800;

            public void MovePointer(Vector3 point) =>
                typeof(MultiCamera).GetProperty(nameof(MultiCamera.MousePosition)).SetValue(Camera, Project(point));

            public Vector3 BeginSelection(bool missedRay)
            {
                Vector3 origin = new Vector3(6, _height, 6);
                MovePointer(origin);
                Physics.SyncTransforms();
                Assert.That(_collider.Raycast(Camera.AttachedCamera.ViewportPointToRay(Camera.MousePosition / 800),
                    out RaycastHit hit, 200), Is.True);
                InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Left));
                InputSystem.Update();
                Select(missedRay ? default : hit);
                FieldInfo state = typeof(HeightUpdater).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
                state.SetValue(Updater, Enum.Parse(state.FieldType, "Dragging"));
                InputSystem.Update();
                return missedRay ? origin : hit.point;
            }

            public List<HeightmapHandle> Select(RaycastHit hit = default) =>
                (List<HeightmapHandle>)typeof(HeightUpdater).GetMethod("UpdateHoveredHandlesComplexSelection",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Updater, new object[] { hit });

            public void Dispose()
            {
                _input.Disable();
                Object.DestroyImmediate(_input.asset);
                InputSystem.RemoveDevice(_mouse);
                Object.DestroyImmediate(_cameraRoot);
                Object.DestroyImmediate(_mapRoot);
                InputSystem.settings = _originalSettings;
                Object.DestroyImmediate(_testSettings);
            }
        }

        private sealed class GroundResolver : IGroundDataResolver
        {
            private readonly GroundData _ground = new GroundData("Grass", "gr", Array.Empty<string[]>(), null, null, false);
            public GroundData Resolve(string shortName) => _ground;
        }
    }
}
