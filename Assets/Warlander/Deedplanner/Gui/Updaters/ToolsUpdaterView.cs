using System;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Gui.Widgets;

namespace Warlander.Deedplanner.Gui.Updaters
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

        [SerializeField] private Button _calculateMapMaterialsButton = null;

        public event Action<ToolsMode> ModeChanged;
        public event Action<ToolsMaterialsScope> MaterialsScopeChanged;
        public event Action MaterialsCalculationRequested;

        private void Awake()
        {
            _calculateMaterialsToggle.onValueChanged.AddListener(toggled => OnModeToggled(toggled, ToolsMode.MaterialsCalculator));
            _mapWarningsToggle.onValueChanged.AddListener(toggled => OnModeToggled(toggled, ToolsMode.MapWarnings));

            _buildingAllLevelsMaterialsToggle.onValueChanged.AddListener(toggled => OnScopeToggled(toggled, ToolsMaterialsScope.BuildingAllLevels));
            _buildingCurrentLevelMaterialsToggle.onValueChanged.AddListener(toggled => OnScopeToggled(toggled, ToolsMaterialsScope.BuildingCurrentLevel));
            _roomCurrentLevelMaterialsToggle.onValueChanged.AddListener(toggled => OnScopeToggled(toggled, ToolsMaterialsScope.RoomCurrentLevel));

            _calculateMapMaterialsButton.onClick.AddListener(OnCalculateMapMaterialsClicked);
        }

        public void ShowPanel(ToolsMode mode)
        {
            _calculateMaterialsPanelTransform.gameObject.SetActive(mode == ToolsMode.MaterialsCalculator);
            _mapWarningsPanelTransform.gameObject.SetActive(mode == ToolsMode.MapWarnings);
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
