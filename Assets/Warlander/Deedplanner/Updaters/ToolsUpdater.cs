using System;
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
        
        [SerializeField] private TMP_InputField materialsInputField = null;

        [SerializeField] private UnityList warningsList = null;

        [SerializeField] private Toggle buildingAllLevelsMaterialsToggle = null;
        [SerializeField] private Toggle buildingCurrentLevelMaterialsToggle = null;
        [SerializeField] private Toggle roomCurrentLevelMaterialsToggle = null;
        
        private ToolType currentTool = ToolType.MaterialsCalculator;

        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Tiles;
            
            RefreshMode();
            RefreshGui();
        }

        private void Update()
        {
            if (currentTool != ToolType.MaterialsCalculator)
            {
                // we need to react to actions on map only when calculating materials
                return;
            }
            
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }
            
            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            if (!overlayMesh)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                int floor = LayoutManager.Instance.CurrentCamera.Floor;
                int x = Mathf.FloorToInt(raycast.point.x / 4f);
                int y = Mathf.FloorToInt(raycast.point.z / 4f);
                Map map = GameManager.Instance.Map;
                Tile clickedTile = map[x, y];

                if (buildingAllLevelsMaterialsToggle.isOn)
                {
                    BuildingsSummary surfaceGroundSummary = new BuildingsSummary(GameManager.Instance.Map, 0);
                    Materials materials = new Materials();
                    Building building = surfaceGroundSummary.GetBuildingAtTile(clickedTile);
                    if (building == null)
                    {
                        ShowMaterialsWindow("No valid building on clicked tile");
                        return;
                    }
                    
                    foreach (TileSummary tileSummary in building.AllTiles)
                    {
                        Tile tile = map[tileSummary.X, tileSummary.Y];
                        materials.Add(tile.CalculateTileMaterials(tileSummary.TilePart));
                    }
                    
                    StringBuilder summary = new StringBuilder();
                    summary.Append("Carpentry needed: ").Append(building.GetCarpentryRequired()).AppendLine();
                    summary.AppendLine();
                    summary.Append(materials);
                    
                    ShowMaterialsWindow(summary.ToString());
                    if (Debug.isDebugBuild)
                    {
                        Debug.Log(building.CreateSummary());
                    }
                }
                else if (buildingCurrentLevelMaterialsToggle.isOn)
                {
                    BuildingsSummary surfaceGroundSummary = new BuildingsSummary(GameManager.Instance.Map, 0);
                    Materials materials = new Materials();
                    Building building = surfaceGroundSummary.GetBuildingAtTile(clickedTile);
                    if (building == null)
                    {
                        ShowMaterialsWindow("No valid building on clicked tile");
                        return;
                    }
                    
                    foreach (TileSummary tileSummary in building.AllTiles)
                    {
                        Tile tile = map[tileSummary.X, tileSummary.Y];
                        materials.Add(tile.CalculateFloorMaterials(floor, tileSummary.TilePart));
                    }
                    
                    StringBuilder summary = new StringBuilder();
                    if (floor == 0 || floor == -1)
                    {
                        summary.Append("Carpentry needed: ").Append(building.GetCarpentryRequired()).AppendLine();
                    }
                    else
                    {
                        summary.AppendLine("To calculate carpentry needed, please use this option on a ground floor");
                    }
                    summary.AppendLine();
                    summary.Append(materials);
                    
                    ShowMaterialsWindow(summary.ToString());
                    if (Debug.isDebugBuild)
                    {
                        Debug.Log(building.CreateSummary());
                    }
                }
                else if (roomCurrentLevelMaterialsToggle.isOn)
                {
                    BuildingsSummary surfaceGroundSummary = new BuildingsSummary(GameManager.Instance.Map, 0);
                    Materials materials = new Materials();
                    Room room = surfaceGroundSummary.GetRoomAtTile(clickedTile);
                    if (room == null)
                    {
                        ShowMaterialsWindow("No valid room on clicked tile");
                        return;
                    }
                    
                    foreach (TileSummary tileSummary in room.Tiles)
                    {
                        Tile tile = map[tileSummary.X, tileSummary.Y];
                        materials.Add(tile.CalculateFloorMaterials(floor, tileSummary.TilePart));
                    }
                    
                    ShowMaterialsWindow(materials.ToString());
                    if (Debug.isDebugBuild)
                    {
                        Debug.Log(room.CreateSummary());
                    }
                }
            }
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

            try
            {
                RefreshTileWarnings();
                if (warningsList.Values.Length == 0)
                {
                    warningsList.Add("No warnings for this map.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                warningsList.Clear();
                warningsList.Add("Some of warning checks failed. Please check program logs for errors.");
            }
        }

        private void RefreshTileWarnings()
        {
            BuildingsSummary surfaceGroundSummary = new BuildingsSummary(GameManager.Instance.Map, 0);
            Map map = GameManager.Instance.Map;

            foreach (Tile tile in map)
            {
                RefreshSlopedWallsWarningsTile(tile);
                RefreshEntityOutsideBuildingWarningsTile(surfaceGroundSummary, tile);
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

        private void RefreshEntityOutsideBuildingWarningsTile(BuildingsSummary buildingsSummary, Tile tile)
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
            GuiManager.Instance.ShowWindow(WindowId.Materials);
            materialsInputField.text = text;
        }
        
        private enum ToolType
        {
            MaterialsCalculator, MapWarnings
        }
    }
}