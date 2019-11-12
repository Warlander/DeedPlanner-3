using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Walls;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public class ToolsUpdater : AbstractUpdater
    {
        [SerializeField] private Toggle calculateMaterialsToggle = null;
        [SerializeField] private Toggle mapWarningsToggle = null;
        
        [SerializeField] private RectTransform calculateMaterialsPanelTransform = null;
        [SerializeField] private RectTransform mapWarningsPanelTransform = null;

        [SerializeField] private RectTransform materialsWindowTransform = null;
        [SerializeField] private TMP_InputField materialsInputField = null;

        [SerializeField] private UnityList warningsList = null;

        private ToolType currentTool = ToolType.MaterialsCalculator;
        
        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Tiles;
            
            RefreshMode();
            RefreshGui();
        }

        private void Update()
        {
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }
            
            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            
            
        }
        
        private void RefreshMode()
        {
            if (calculateMaterialsToggle.isOn)
            {
                currentTool = ToolType.MaterialsCalculator;
            }
            else if (mapWarningsToggle.isOn)
            {
                currentTool = ToolType.MapWarnings;
            }
        }

        private void RefreshGui()
        {
            calculateMaterialsPanelTransform.gameObject.SetActive(currentTool == ToolType.MaterialsCalculator);
            mapWarningsPanelTransform.gameObject.SetActive(currentTool == ToolType.MapWarnings);
            
            if (mapWarningsPanelTransform.gameObject.activeSelf)
            {
                RefreshMapWarnings();
            }
        }

        private void RefreshMapWarnings()
        {
            warningsList.Clear();
            
            
        }

        private string CreateWarningString(Tile tile, string text)
        {
            StringBuilder build = new StringBuilder();
            build.Append("(").Append(tile.X).Append(", ").Append(tile.Y).Append(") ").Append(text);
            return build.ToString();
        }

        public void OnModeChange(bool toggledOn)
        {
            if (!toggledOn)
            {
                return;
            }

            RefreshMode();
            RefreshGui();
        }
        
        public void CalculateMapMaterials()
        {
            Materials mapMaterials = GameManager.Instance.Map.CalculateMapMaterials();
            
            ShowMaterialsWindow(mapMaterials.ToString());
        }

        private void ShowMaterialsWindow(string text)
        {
            materialsWindowTransform.gameObject.SetActive(true);
            materialsInputField.text = text;
        }
        
        private enum ToolType
        {
            MaterialsCalculator, MapWarnings
        }
    }
}