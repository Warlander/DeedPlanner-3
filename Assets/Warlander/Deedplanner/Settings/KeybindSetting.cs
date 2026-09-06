using System;
using UnityEngine.InputSystem;
using Warlogic.Settings;
using Warlogic.Settings.Ugui;

namespace Warlander.Deedplanner.Settings
{
    /// <summary>
    /// Setting wrapping one Input System binding. Value is the effective binding path.
    /// Persistence goes through InputSettings (JSON binding overrides in the unified store).
    /// </summary>
    public sealed class KeybindSetting : ISetting<string>, IGroupedSetting
    {
        private readonly InputAction _action;
        private readonly int _bindingIndex;
        private readonly InputSettings _inputSettings;

        public string Key { get; }
        public string Label { get; }
        public string Group { get; }
        public string Description => null;
        public ApplyMode ApplyMode => ApplyMode.Immediate;
        public bool IsDirty => false;

        public event Action Changed;

        public KeybindSetting(InputAction action, int bindingIndex, InputSettings inputSettings)
        {
            _action = action;
            _bindingIndex = bindingIndex;
            _inputSettings = inputSettings;

            Group = action.actionMap.name;
            Key = $"keybind.{action.actionMap.name}.{action.name}.{bindingIndex}";
            Label = ComputeLabel();

            _inputSettings.SettingsReset += OnSettingsReset;
        }

        public string Value
        {
            get => _action.bindings[_bindingIndex].effectivePath;
            set
            {
                _action.ApplyBindingOverride(_bindingIndex, value);
                _inputSettings.Save();
                Changed?.Invoke();
            }
        }

        public string StagedValue
        {
            get => Value;
            set => Value = value;
        }

        public string DefaultValue => _action.bindings[_bindingIndex].path;

        public string DisplayString => _action.bindings[_bindingIndex].ToDisplayString();

        public void Commit() { }

        public void Revert() { }

        public bool IsEnabled()
        {
            return true;
        }

        public void ResetToDefault()
        {
            _action.RemoveBindingOverride(_bindingIndex);
            _inputSettings.Save();
            Changed?.Invoke();
        }

        public void PerformInteractiveRebind(Action onSuccess, Action onCancel)
        {
            _action.Disable();

            _action.PerformInteractiveRebinding(_bindingIndex).OnComplete(operation =>
            {
                operation.Dispose();
                _action.Enable();
                _inputSettings.Save();
                Changed?.Invoke();
                onSuccess?.Invoke();
            }).OnCancel(operation =>
            {
                operation.Dispose();
                _action.Enable();
                onCancel?.Invoke();
            }).Start();
        }

        private string ComputeLabel()
        {
            string bindingName = _action.bindings[_bindingIndex].name;
            if (string.IsNullOrEmpty(bindingName) == false)
            {
                return $"{_action.name} ({bindingName})";
            }
            return _action.name;
        }

        private void OnSettingsReset()
        {
            Changed?.Invoke();
        }
    }
}
