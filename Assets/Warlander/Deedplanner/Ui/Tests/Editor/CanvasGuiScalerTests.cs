using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Settings;
using Warlogic.Settings;
using Warlogic.Settings.Ugui;

namespace Warlander.Deedplanner.Ui.Tests
{
    public class CanvasGuiScalerTests
    {
        private Mouse _mouse;
        private DPInput _input;
        private DeedPlannerSettings _settings;
        private GameObject _root;
        private CanvasScaler _canvas;
        private CanvasGuiScaler _scaler;

        [SetUp]
        public void SetUp()
        {
            _mouse = InputSystem.AddDevice<Mouse>();
            _input = new DPInput();
            _input.UI.Enable();
            var logger = new LoggerSource(new LogLevelFilter()).Create(DeedPlannerSettings.Category);
            _settings = DeedPlannerSettings.Create(logger, new EmptyStore());
            _root = new GameObject("Scale Test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _canvas = _root.GetComponent<CanvasScaler>();
            _scaler = _root.AddComponent<CanvasGuiScaler>();
            SetField("_settings", _settings.Ui);
            SetField("_input", _input);
            SetField("_canvasScaler", _canvas);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            _input.Disable();
            UnityEngine.Object.DestroyImmediate(_input.asset);
            InputSystem.RemoveDevice(_mouse);
        }

        [Test]
        public void StartupScaleAppliesEvenWhilePointerIsHeld()
        {
            _settings.Ui.GuiScale = 9;
            SetPointerPressed(true);

            InvokeLifecycle("Start");

            AssertScale(9);
        }

        [Test]
        public void SliderKeepsCanvasStableUntilReleaseThenAppliesFinalScale()
        {
            InvokeLifecycle("Start");
            var prefab = AssetDatabase.LoadAssetAtPath<SliderSettingWidget>("Assets/Prefabs/Settings/SliderSettingRow.prefab");
            SliderSettingWidget widget = UnityEngine.Object.Instantiate(prefab, _root.transform);
            var setting = (IntSetting) _settings.Registry.GetSetting("guiScale");
            widget.Bind(setting, setting.Min, setting.Max);
            Slider slider = widget.GetComponentInChildren<Slider>();
            SetPointerPressed(true);

            slider.value = 9;
            InvokeLifecycle("Update");
            AssertScale(10);
            slider.value = 20;
            InvokeLifecycle("Update");
            AssertScale(10);

            SetPointerPressed(false);
            InvokeLifecycle("Update");

            AssertScale(20);
        }

        [Test]
        public void ScaleChangesWithoutPointerPressApplyImmediately()
        {
            InvokeLifecycle("Start");

            _settings.Ui.GuiScale = 15;

            AssertScale(15);
        }

        [Test]
        public void AnotherPointerReleaseKeepsCanvasStableWhileFirstPointerIsHeld()
        {
            InvokeLifecycle("Start");
            SetPointerPressed(true);
            Mouse secondMouse = InputSystem.AddDevice<Mouse>();
            try
            {
                InputSystem.QueueStateEvent(secondMouse, new MouseState().WithButton(MouseButton.Left, true));
                InputSystem.Update();
                _settings.Ui.GuiScale = 20;
                InputSystem.QueueStateEvent(secondMouse, new MouseState());
                InputSystem.Update();

                InvokeLifecycle("Update");
                AssertScale(10);

                SetPointerPressed(false);
                InvokeLifecycle("Update");
                AssertScale(20);
            }
            finally
            {
                InputSystem.RemoveDevice(secondMouse);
            }
        }

        private void SetPointerPressed(bool pressed)
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Left, pressed));
            InputSystem.Update();
            Assert.That(_mouse.leftButton.isPressed, Is.EqualTo(pressed));
        }

        private void AssertScale(int scale)
        {
            Assert.That(_canvas.referenceResolution,
                Is.EqualTo(new Vector2(Constants.DefaultGuiWidth, Constants.DefaultGuiHeight) *
                    (scale * Constants.GuiScaleUnitsToRealScale)));
        }

        private void SetField(string name, object value)
        {
            typeof(CanvasGuiScaler).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_scaler, value);
        }

        private void InvokeLifecycle(string name)
        {
            typeof(CanvasGuiScaler).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_scaler, Array.Empty<object>());
        }

        private sealed class EmptyStore : ISettingsStore
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
