using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Updaters
{
    public class HeightUpdaterView : MonoBehaviour, IHeightUpdaterView
    {
        [SerializeField] private Toggle _selectAndDragToggle = null;
        [SerializeField] private Toggle _createRampsToggle = null;
        [SerializeField] private Toggle _levelAreaToggle = null;
        [SerializeField] private Toggle _paintTerrainToggle = null;

        [SerializeField] private RectTransform _handlesSettingsTransform = null;
        [SerializeField] private RectTransform _paintingSettingsTransform = null;

        [SerializeField] private RectTransform _selectAndDragInstructionsTransform = null;
        [SerializeField] private RectTransform _createRampsInstructionsTransform = null;
        [SerializeField] private RectTransform _levelAreaInstructionsTransform = null;
        [SerializeField] private RectTransform _paintTerrainInstructionsTransform = null;

        [SerializeField] private TMP_InputField _dragSensitivityInput = null;
        [SerializeField] private Toggle _respectOriginalSlopesToggle = null;

        [SerializeField] private TMP_InputField _targetHeightInput = null;

        public event Action<HeightMode> ModeChanged;
        public event Action<string> DragSensitivityChanged;
        public event Action<bool> RespectOriginalSlopesChanged;
        public event Action<string> TargetHeightChanged;

        private void Awake()
        {
            _selectAndDragToggle.onValueChanged.AddListener(toggled => OnModeToggled(toggled, HeightMode.SelectAndDrag));
            _createRampsToggle.onValueChanged.AddListener(toggled => OnModeToggled(toggled, HeightMode.CreateRamps));
            _levelAreaToggle.onValueChanged.AddListener(toggled => OnModeToggled(toggled, HeightMode.LevelArea));
            _paintTerrainToggle.onValueChanged.AddListener(toggled => OnModeToggled(toggled, HeightMode.PaintTerrain));

            _dragSensitivityInput.onValueChanged.AddListener(OnDragSensitivityChanged);
            _respectOriginalSlopesToggle.onValueChanged.AddListener(OnRespectOriginalSlopesChanged);
            _targetHeightInput.onValueChanged.AddListener(OnTargetHeightChanged);
        }

        public void ShowModePanels(HeightMode mode)
        {
            _handlesSettingsTransform.gameObject.SetActive(mode == HeightMode.SelectAndDrag || mode == HeightMode.CreateRamps);
            _paintingSettingsTransform.gameObject.SetActive(mode == HeightMode.LevelArea || mode == HeightMode.PaintTerrain);

            _selectAndDragInstructionsTransform.gameObject.SetActive(mode == HeightMode.SelectAndDrag);
            _createRampsInstructionsTransform.gameObject.SetActive(mode == HeightMode.CreateRamps);
            _levelAreaInstructionsTransform.gameObject.SetActive(mode == HeightMode.LevelArea);
            _paintTerrainInstructionsTransform.gameObject.SetActive(mode == HeightMode.PaintTerrain);
        }

        public void SetDragSensitivity(string text)
        {
            _dragSensitivityInput.SetTextWithoutNotify(text);
        }

        public void SetRespectOriginalSlopes(bool value)
        {
            _respectOriginalSlopesToggle.SetIsOnWithoutNotify(value);
        }

        private void OnModeToggled(bool toggledOn, HeightMode mode)
        {
            if (toggledOn)
            {
                ModeChanged?.Invoke(mode);
            }
        }

        private void OnDragSensitivityChanged(string value)
        {
            DragSensitivityChanged?.Invoke(value);
        }

        private void OnRespectOriginalSlopesChanged(bool value)
        {
            RespectOriginalSlopesChanged?.Invoke(value);
        }

        private void OnTargetHeightChanged(string value)
        {
            TargetHeightChanged?.Invoke(value);
        }
    }
}
