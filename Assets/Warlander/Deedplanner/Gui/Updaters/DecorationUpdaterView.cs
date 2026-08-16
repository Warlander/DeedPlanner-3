using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Gui.Widgets;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public class DecorationUpdaterView : MonoBehaviour, IDecorationUpdaterView
    {
        [SerializeField] private UnityTree _decorationsTree;

        [SerializeField] private Toggle _snapToGridToggle;
        [SerializeField] private Toggle _rotationSnappingToggle;
        [SerializeField] private TMP_InputField _rotationSensitivityInput;

        public event Action<DecorationData> DecorationSelected;
        public event Action<bool> SnapToGridChanged;
        public event Action<bool> RotationSnappingChanged;
        public event Action<string> RotationSensitivityChanged;

        private void Awake()
        {
            _decorationsTree.ValueChanged += OnDecorationsTreeValueChanged;
            _snapToGridToggle.onValueChanged.AddListener(OnSnapToGridChanged);
            _rotationSnappingToggle.onValueChanged.AddListener(OnRotationSnappingChanged);
            _rotationSensitivityInput.onValueChanged.AddListener(OnRotationSensitivityChanged);
        }

        public void AddDecorationEntry(DecorationData data, string[] category)
        {
            _decorationsTree.Add(data, category);
        }

        public void SetSnapToGrid(bool value)
        {
            _snapToGridToggle.SetIsOnWithoutNotify(value);
        }

        public void SetRotationSnapping(bool value)
        {
            _rotationSnappingToggle.SetIsOnWithoutNotify(value);
        }

        public void SetRotationSensitivity(string text)
        {
            _rotationSensitivityInput.SetTextWithoutNotify(text);
        }

        public void PushSelection()
        {
            OnDecorationsTreeValueChanged(_decorationsTree.SelectedValue);
        }

        private void OnDecorationsTreeValueChanged(object value)
        {
            DecorationSelected?.Invoke(value as DecorationData);
        }

        private void OnSnapToGridChanged(bool value)
        {
            SnapToGridChanged?.Invoke(value);
        }

        private void OnRotationSnappingChanged(bool value)
        {
            RotationSnappingChanged?.Invoke(value);
        }

        private void OnRotationSensitivityChanged(string value)
        {
            RotationSensitivityChanged?.Invoke(value);
        }
    }
}
