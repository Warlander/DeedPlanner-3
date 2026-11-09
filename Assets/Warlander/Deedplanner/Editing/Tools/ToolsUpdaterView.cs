using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Ui.Widgets;

namespace Warlander.Deedplanner.Editing
{
    public class ToolsUpdaterView : MonoBehaviour, IToolsUpdaterView
    {
        [SerializeField] private Toggle _calculateMaterialsToggle = null;
        [SerializeField] private Toggle _mapWarningsToggle = null;

        [SerializeField] private RectTransform _calculateMaterialsPanelTransform = null;
        [SerializeField] private RectTransform _mapWarningsPanelTransform = null;

        [SerializeField] private UnityList _warningsList = null;

        [SerializeField] private Toggle _buildingAllLevelsMaterialsToggle = null;
        [SerializeField] private Toggle _buildingCurrentLevelMaterialsToggle = null;
        [SerializeField] private Toggle _roomCurrentLevelMaterialsToggle = null;
        [SerializeField] private Toggle _areaAllLevelsMaterialsToggle = null;
        [SerializeField] private Toggle _areaCurrentLevelMaterialsToggle = null;

        [SerializeField] private Button _calculateMapMaterialsButton = null;

        public event Action<ToolsMode> ModeChanged;
        public event Action<ToolsMaterialsScope> MaterialsScopeChanged;
        public event Action MaterialsCalculationRequested;

        private RectTransform _areaSelectionRoot;
        private AreaSelectionOutline _areaSelectionOutline;

        private void Awake()
        {
            _calculateMaterialsToggle.onValueChanged.AddListener(toggled => OnModeToggled(toggled, ToolsMode.MaterialsCalculator));
            _mapWarningsToggle.onValueChanged.AddListener(toggled => OnModeToggled(toggled, ToolsMode.MapWarnings));

            _buildingAllLevelsMaterialsToggle.onValueChanged.AddListener(toggled => OnScopeToggled(toggled, ToolsMaterialsScope.BuildingAllLevels));
            _buildingCurrentLevelMaterialsToggle.onValueChanged.AddListener(toggled => OnScopeToggled(toggled, ToolsMaterialsScope.BuildingCurrentLevel));
            _roomCurrentLevelMaterialsToggle.onValueChanged.AddListener(toggled => OnScopeToggled(toggled, ToolsMaterialsScope.RoomCurrentLevel));
            _areaAllLevelsMaterialsToggle.onValueChanged.AddListener(toggled => OnScopeToggled(toggled, ToolsMaterialsScope.AreaAllLevels));
            _areaCurrentLevelMaterialsToggle.onValueChanged.AddListener(toggled => OnScopeToggled(toggled, ToolsMaterialsScope.AreaCurrentLevel));

            _calculateMapMaterialsButton.onClick.AddListener(OnCalculateMapMaterialsClicked);
        }

        public void ShowPanel(ToolsMode mode)
        {
            _calculateMaterialsPanelTransform.gameObject.SetActive(mode == ToolsMode.MaterialsCalculator);
            _mapWarningsPanelTransform.gameObject.SetActive(mode == ToolsMode.MapWarnings);
        }

        public void ShowAreaSelection(RectTransform screen, IReadOnlyList<Vector2> segments)
        {
            if (!_areaSelectionRoot)
            {
                _areaSelectionRoot = new GameObject("Materials Area Selection", typeof(RectTransform),
                    typeof(RectMask2D)).GetComponent<RectTransform>();
                _areaSelectionOutline = new GameObject("Outline", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(AreaSelectionOutline)).GetComponent<AreaSelectionOutline>();
                _areaSelectionOutline.transform.SetParent(_areaSelectionRoot, false);
                _areaSelectionOutline.rectTransform.anchorMin = Vector2.zero;
                _areaSelectionOutline.rectTransform.anchorMax = Vector2.one;
                _areaSelectionOutline.rectTransform.offsetMin = Vector2.zero;
                _areaSelectionOutline.rectTransform.offsetMax = Vector2.zero;
                _areaSelectionOutline.color = Color.yellow;
                _areaSelectionOutline.raycastTarget = false;
            }
            _areaSelectionRoot.SetParent(screen, false);
            _areaSelectionRoot.gameObject.layer = screen.gameObject.layer;
            _areaSelectionOutline.gameObject.layer = screen.gameObject.layer;
            _areaSelectionRoot.anchorMin = Vector2.zero;
            _areaSelectionRoot.anchorMax = Vector2.one;
            _areaSelectionRoot.offsetMin = Vector2.zero;
            _areaSelectionRoot.offsetMax = Vector2.zero;
            _areaSelectionRoot.gameObject.SetActive(true);
            _areaSelectionOutline.SetSegments(segments);
        }

        public void HideAreaSelection()
        {
            if (_areaSelectionRoot) _areaSelectionRoot.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_areaSelectionRoot) Destroy(_areaSelectionRoot.gameObject);
        }

        public void ClearWarnings()
        {
            _warningsList.Clear();
        }

        public void AddWarning(string text)
        {
            _warningsList.Add(text);
        }

        private void OnModeToggled(bool toggledOn, ToolsMode mode)
        {
            if (toggledOn)
            {
                ModeChanged?.Invoke(mode);
            }
        }

        private void OnScopeToggled(bool toggledOn, ToolsMaterialsScope scope)
        {
            if (toggledOn)
            {
                MaterialsScopeChanged?.Invoke(scope);
            }
        }

        private void OnCalculateMapMaterialsClicked()
        {
            MaterialsCalculationRequested?.Invoke();
        }
    }
}
