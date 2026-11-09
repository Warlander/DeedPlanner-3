using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Warlander.Deedplanner.Bridges;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Domain.Entities.Decorations;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Persistence;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Deedplanner.Settings;
using Warlogic.Settings;
using Object = UnityEngine.Object;
using UnityInputSettings = UnityEngine.InputSystem.InputSettings;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class DecorationUpdaterTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private DPInput _input;
        private Mouse _mouse;
        private SharedMaterials _materials;
        private UnityInputSettings _originalInputSettings;
        private UnityInputSettings _testInputSettings;

        [SetUp]
        public void SetUp()
        {
            _originalInputSettings = InputSystem.settings;
            _testInputSettings = Object.Instantiate(_originalInputSettings);
            // Gameplay actions otherwise stay disabled in EditMode.
            _testInputSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            InputSystem.settings = _testInputSettings;
        }

        [TearDown]
        public void TearDown()
        {
            if (_input != null)
            {
                _input.Disable();
                Object.DestroyImmediate(_input.asset);
            }
            if (_mouse != null) InputSystem.RemoveDevice(_mouse);
            foreach (GameObject gameObject in _objects) Object.DestroyImmediate(gameObject);
            _objects.Clear();
            if (_materials) Object.DestroyImmediate(_materials);
            _input = null;
            _mouse = null;
            _materials = null;
            InputSystem.settings = _originalInputSettings;
            Object.DestroyImmediate(_testInputSettings);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Tick_RightClickBridgeWithoutTarget_DoesNotThrowAndCancelsPlacement(bool placing)
        {
            var cameraRoot = CreateObject("Test camera");
            cameraRoot.SetActive(false);
            var camera = cameraRoot.AddComponent<MultiCamera>();
            var coordinator = cameraRoot.AddComponent<CameraCoordinator>();
            SetField(coordinator, "_cameras", new[] { camera });

            var bridge = CreateObject("Test bridge");
            bridge.AddComponent<BridgePart>();
            var collider = bridge.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            Assert.That(collider.Raycast(new Ray(Vector3.up * 10f, Vector3.down), out RaycastHit hit, 20f), Is.True);
            typeof(MultiCamera).GetProperty(nameof(MultiCamera.CurrentRaycast)).SetValue(camera, hit);

            var loggerSource = new LoggerSource();
            EditingSettings settings = DeedPlannerSettings.Create(
                loggerSource.Create(DeedPlannerSettings.Category), new MemoryStore()).Editing;
            _input = new DPInput();
            _mouse = InputSystem.AddDevice<Mouse>();
            _input.devices = new InputDevice[] { _mouse };
            _input.Enable();
            _materials = ScriptableObject.CreateInstance<SharedMaterials>();
            var updater = new DecorationUpdater(null, settings, coordinator, _input,
                new MapHandler(null, null, loggerSource), null, _materials, null, loggerSource, null, null, null);
            var ghost = CreateObject("Loaded decoration preview");
            ghost.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            var data = new DecorationData(null, "Test object", "test", Array.Empty<string[]>(), "normal",
                false, false, false, false, false, null);
            SetField(updater, "_selectedDecoration", data);
            SetField(updater, "_lastGhostData", data);
            SetField(updater, "_ghostObject", ghost);
            SetField(updater, "_placingDecoration", placing);
            SetField(updater, "_isScrollRotate", placing);

            InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Right));
            InputSystem.Update();
            Assert.That(_input.UpdatersShared.Deletion.WasPerformedThisFrame(), Is.True);

            Assert.DoesNotThrow(updater.Tick);

            Assert.That(GetField(updater, "_targetedTile"), Is.Null);
            Assert.That(GetField(updater, "_placingDecoration"), Is.False);
            Assert.That(GetField(updater, "_isScrollRotate"), Is.False);
            Assert.That(ghost.transform.localRotation, Is.EqualTo(Quaternion.identity));
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private sealed class MemoryStore : ISettingsStore
        {
            public bool TryLoad(string key, out string value)
            {
                value = null;
                return false;
            }

            public void Save(string key, string value) { }
        }
    }
}
