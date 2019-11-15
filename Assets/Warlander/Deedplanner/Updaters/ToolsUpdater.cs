using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Summary;
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
        private MapSummary mapSummary;
        
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
                mapSummary = new MapSummary(GameManager.Instance.Map);
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

            RefreshSlopedWallsWarnings();
            
            if (warningsList.Values.Length == 0)
            {
                warningsList.Add("No warnings for this map");
            }
        }

        private void RefreshSlopedWallsWarnings()
        {
            Map map = GameManager.Instance.Map;

            foreach (Tile tile in map)
            {
                RefreshSlopedWallsWarningsTile(tile);
            }
        }

        private void RefreshSlopedWallsWarningsTile(Tile tile)
        {
            for (int i = Constants.NegativeFloorLimit; i < Constants.FloorLimit; i++)
            {
                Wall vWall = tile.GetVerticalWall(i);
                if (vWall && vWall.Data.HouseWall && vWall.SlopeDifference != 0)
                {
                    warningsList.Add(CreateWarningString(tile, "Building wall on sloped terrain"));
                    break;
                }
                
                Wall hWall = tile.GetHorizontalWall(i);
                if (hWall && hWall.Data.HouseWall && hWall.SlopeDifference != 0)
                {
                    warningsList.Add(CreateWarningString(tile, "Building wall on sloped terrain"));
                    break;
                }
            }
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