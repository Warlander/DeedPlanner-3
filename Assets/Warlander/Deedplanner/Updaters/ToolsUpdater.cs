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
        private BuildingsSummary buildingsSummary;
        
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
                buildingsSummary = new BuildingsSummary(GameManager.Instance.Map);
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

            RefreshTileWarnings();
            
            if (warningsList.Values.Length == 0)
            {
                warningsList.Add("No warnings for this map.\nIf this message is missing, some of checks failed.");
            }
        }

        private void RefreshTileWarnings()
        {
            Map map = GameManager.Instance.Map;

            foreach (Tile tile in map)
            {
                RefreshSlopedWallsWarningsTile(tile);
                RefreshEntityOutsideBuildingWarningsTile(tile);
            }
        }

        private void RefreshSlopedWallsWarningsTile(Tile tile)
        {
            const string warningText = "\nBuilding wall on sloped terrain.";
            
            for (int i = Constants.NegativeFloorLimit; i < Constants.FloorLimit; i++)
            {
                Wall vWall = tile.GetVerticalWall(i);
                if (vWall && vWall.Data.HouseWall && vWall.SlopeDifference != 0)
                {
                    warningsList.Add(CreateWarningString(tile, warningText));
                    break;
                }
                
                Wall hWall = tile.GetHorizontalWall(i);
                if (hWall && hWall.Data.HouseWall && hWall.SlopeDifference != 0)
                {
                    warningsList.Add(CreateWarningString(tile, warningText));
                    break;
                }
            }
        }

        private void RefreshEntityOutsideBuildingWarningsTile(Tile tile)
        {
            const string tileWarningText = "Floor or roof outside known building.\nPlease make sure all ground level walls are built.";
            const string wallWarningText = "Wall outside known building.\nPlease make sure all ground level walls are built.";

            bool containsFloor = buildingsSummary.ContainsFloor(tile);
            bool containsVerticalWall = buildingsSummary.ContainsVerticalWall(tile);
            bool containsHorizontalWall = buildingsSummary.ContainsHorizontalWall(tile);

            if (containsHorizontalWall && containsVerticalWall)
            {
                return;
            }
            
            for (int i = Constants.NegativeFloorLimit; i < Constants.FloorLimit; i++)
            {
                TileEntity floorRoof = tile.GetTileContent(i);
                if (!containsFloor && floorRoof)
                {
                    warningsList.Add(CreateWarningString(tile, tileWarningText));
                }
                
                Wall vWall = tile.GetVerticalWall(i);
                if (!containsVerticalWall && vWall && vWall.Data.HouseWall)
                {
                    warningsList.Add(CreateWarningString(tile, wallWarningText));
                    break;
                }
                
                Wall hWall = tile.GetHorizontalWall(i);
                if (!containsHorizontalWall && hWall && hWall.Data.HouseWall)
                {
                    warningsList.Add(CreateWarningString(tile, wallWarningText));
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