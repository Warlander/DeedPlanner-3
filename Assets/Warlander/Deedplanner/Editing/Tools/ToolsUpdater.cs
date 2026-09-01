using Warlander.Deedplanner.Persistence;
using System;
using System.Text;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner.Data.Summary;
using Warlander.Deedplanner.Data.Walls;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Windows;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Logging;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Editing
{
    public class ToolsUpdater : IUpdater
    {
        public static readonly LogCategory Category = new LogCategory("Tools");

        private readonly IToolsUpdaterView _view;
        private readonly WindowCoordinator _windowCoordinator;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly MapHandler _mapHandler;
        private readonly DPInput _input;
        private readonly TabContext _tabContext;
        private readonly ICategoryLogger _logger;

        public Tab TargetTab => Tab.Tools;

        private ToolsMode _currentTool = ToolsMode.MaterialsCalculator;
        private ToolsMaterialsScope _materialsScope = ToolsMaterialsScope.BuildingAllLevels;
        private int _warningsAdded;

        public ToolsUpdater(IToolsUpdaterView view, WindowCoordinator windowCoordinator, CameraCoordinator cameraCoordinator,
            MapHandler mapHandler, DPInput input, TabContext tabContext, ILoggerSource loggerSource)
        {
            _view = view;
            _windowCoordinator = windowCoordinator;
            _cameraCoordinator = cameraCoordinator;
            _mapHandler = mapHandler;
            _input = input;
            _tabContext = tabContext;
            _logger = loggerSource.Create(Category);
        }

        public void Initialize()
        {
            _view.ModeChanged += OnModeChanged;
            _view.MaterialsScopeChanged += OnMaterialsScopeChanged;
            _view.MaterialsCalculationRequested += CalculateMapMaterials;
        }

        public void Enable()
        {
            _tabContext.TileSelectionMode = TileSelectionMode.Tiles;

            RefreshGui();
        }

        public void Disable() { }

        private void OnModeChanged(ToolsMode mode)
        {
            _currentTool = mode;
            RefreshGui();
        }

        private void OnMaterialsScopeChanged(ToolsMaterialsScope scope)
        {
            _materialsScope = scope;
        }

        public void Tick()
        {
            if (_currentTool != ToolsMode.MaterialsCalculator)
            {
                // we need to react to actions on map only when calculating materials
                return;
            }

            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }

            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            if (!overlayMesh)
            {
                return;
            }

            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                int floor = _cameraCoordinator.Current.Level;
                int x = Mathf.FloorToInt(raycast.point.x / 4f);
                int y = Mathf.FloorToInt(raycast.point.z / 4f);
                Map map = _mapHandler.Map;
                Tile clickedTile = map[x, y];

                if (_materialsScope == ToolsMaterialsScope.BuildingAllLevels)
                {
                    BuildingsSummary surfaceGroundSummary = new BuildingsSummary(map, 0);
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
                    summary.Append("Total tiles: ").Append(building.TilesCount).AppendLine();
                    summary.AppendLine();
                    summary.Append(materials);

                    ShowMaterialsWindow(summary.ToString());
                    if (Debug.isDebugBuild)
                    {
                        _logger.Message(building.CreateSummary());
                    }
                }
                else if (_materialsScope == ToolsMaterialsScope.BuildingCurrentLevel)
                {
                    BuildingsSummary surfaceGroundSummary = new BuildingsSummary(map, floor);
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
                        materials.Add(tile.CalculateLevelMaterials(floor, tileSummary.TilePart));
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
                    summary.Append("Rooms on this level: ").Append(building.RoomsCount).AppendLine();
                    summary.Append("Tiles on this level: ").Append(building.TilesCount).AppendLine();
                    summary.AppendLine();
                    summary.Append(materials);

                    ShowMaterialsWindow(summary.ToString());
                    if (Debug.isDebugBuild)
                    {
                        _logger.Message(building.CreateSummary());
                    }
                }
                else if (_materialsScope == ToolsMaterialsScope.RoomCurrentLevel)
                {
                    BuildingsSummary surfaceGroundSummary = new BuildingsSummary(map, floor);
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
                        materials.Add(tile.CalculateLevelMaterials(floor, tileSummary.TilePart));
                    }

                    StringBuilder summary = new StringBuilder();
                    summary.Append("Tiles in this room: ").Append(room.Tiles.Count).AppendLine();
                    summary.AppendLine();
                    summary.Append(materials);

                    ShowMaterialsWindow(summary.ToString());
                    if (Debug.isDebugBuild)
                    {
                        _logger.Message(room.CreateSummary());
                    }
                }
            }
        }

        private void RefreshGui()
        {
            _view.ShowPanel(_currentTool);

            if (_currentTool == ToolsMode.MapWarnings)
            {
                RefreshMapWarnings();
            }
        }

        private void RefreshMapWarnings()
        {
            _view.ClearWarnings();
            _warningsAdded = 0;

            try
            {
                RefreshTileWarnings();
                if (_warningsAdded == 0)
                {
                    _view.AddWarning("No warnings for this map.");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
                _view.ClearWarnings();
                _view.AddWarning("Some of warning checks failed. Please check program logs for errors.");
            }
        }

        private void RefreshTileWarnings()
        {
            BuildingsSummary surfaceGroundSummary = new BuildingsSummary(_mapHandler.Map, 0);
            Map map = _mapHandler.Map;

            foreach (Tile tile in map)
            {
                RefreshSlopedWallsWarningsTile(tile);
                RefreshEntityOutsideBuildingWarningsTile(surfaceGroundSummary, tile);
                RefreshBuildingsTouchingWarningsTile(surfaceGroundSummary, tile);
                RefreshTokenInsideBuildingWarningsTile(surfaceGroundSummary, tile);
                RefreshDockWarningsTile(tile);
            }
        }

        private void RefreshDockWarningsTile(Tile tile)
        {
            Dock dock = tile.Dock;
            if (dock == null)
            {
                return;
            }

            foreach (string error in dock.ValidationErrors)
            {
                AddWarning(CreateWarningString(tile, error));
            }
        }

        private void RefreshSlopedWallsWarningsTile(Tile tile)
        {
            const string warningText = "\nBuilding wall on sloped terrain.";

            for (int i = Constants.NegativeLevelLimit; i < Constants.LevelLimit; i++)
            {
                Wall vWall = tile.GetVerticalWall(i);
                if (vWall && vWall.Data.HouseWall && vWall.SlopeDifference != 0)
                {
                    AddWarning(CreateWarningString(tile, warningText));
                    break;
                }

                Wall hWall = tile.GetHorizontalWall(i);
                if (hWall && hWall.Data.HouseWall && hWall.SlopeDifference != 0)
                {
                    AddWarning(CreateWarningString(tile, warningText));
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

            for (int i = Constants.NegativeLevelLimit; i < Constants.LevelLimit; i++)
            {
                LevelEntity floorRoof = tile.GetTileContent(i);
                if (!containsFloor && floorRoof)
                {
                    AddWarning(CreateWarningString(tile, tileWarningText));
                }

                Wall vWall = tile.GetVerticalWall(i);
                if (!containsVerticalWall && vWall && vWall.Data.HouseWall)
                {
                    AddWarning(CreateWarningString(tile, wallWarningText));
                    break;
                }

                Wall hWall = tile.GetHorizontalWall(i);
                if (!containsHorizontalWall && hWall && hWall.Data.HouseWall)
                {
                    AddWarning(CreateWarningString(tile, wallWarningText));
                    break;
                }
            }
        }

        private void RefreshBuildingsTouchingWarningsTile(BuildingsSummary buildingsSummary, Tile tile)
        {
            const string buildingsTouchingWarningText = "Buildings sharing corner.\nTwo buildings cannot share any corner.";

            Building building = buildingsSummary.GetBuildingAtTile(tile);
            if (building == null)
            {
                return;
            }

            Building leftTop = buildingsSummary.GetBuildingAtCoords(tile.X - 1, tile.Y + 1);
            if (leftTop != null && building != leftTop)
            {
                AddWarning(CreateWarningString(tile, buildingsTouchingWarningText));
            }

            Building rightTop = buildingsSummary.GetBuildingAtCoords(tile.X + 1, tile.Y + 1);
            if (rightTop != null && building != rightTop)
            {
                AddWarning(CreateWarningString(tile, buildingsTouchingWarningText));
            }
        }

        private void RefreshTokenInsideBuildingWarningsTile(BuildingsSummary buildingsSummary, Tile tile)
        {
            const string tokenInsideBuildingWarningText = "Deed token inside building.\nTokens must be placed outside.";

            Decoration centralDecoration = tile.GetCentralDecoration();

            // TODO: update the objects.xml schema to not require ShortName lookup here
            if (centralDecoration && centralDecoration.Data.ShortName == "token" && buildingsSummary.GetBuildingAtTile(tile) != null)
            {
                AddWarning(CreateWarningString(tile, tokenInsideBuildingWarningText));
            }
        }

        private void AddWarning(string text)
        {
            _warningsAdded++;
            _view.AddWarning(text);
        }

        private string CreateWarningString(Tile tile, string text)
        {
            StringBuilder build = new StringBuilder();
            build.Append("(").Append(tile.X).Append(", ").Append(tile.Y).Append(") ").Append(text);
            return build.ToString();
        }

        public void CalculateMapMaterials()
        {
            Materials mapMaterials = _mapHandler.Map.CalculateMapMaterials();

            BuildingsSummary surfaceGroundSummary = new BuildingsSummary(_mapHandler.Map, 0);

            StringBuilder build = new StringBuilder();
            build.Append("Total buildings: ").Append(surfaceGroundSummary.BuildingsCount).AppendLine();
            build.Append("Total rooms: ").Append(surfaceGroundSummary.RoomsCount).AppendLine();
            build.AppendLine();
            build.Append(mapMaterials);

            ShowMaterialsWindow(build.ToString());
        }

        private void ShowMaterialsWindow(string text)
        {
            _windowCoordinator.CreateWindow<TextWindow>(WindowNames.TextWindow).ShowText("Materials", text);
        }
    }
}
