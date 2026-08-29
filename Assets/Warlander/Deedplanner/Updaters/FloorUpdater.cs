using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Docks;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Gui.Updaters;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;

namespace Warlander.Deedplanner.Updaters
{
    public class FloorUpdater : IUpdater
    {
        private enum DockStroke
        {
            None, Paint, Erase
        }

        private readonly IFloorUpdaterView _view;
        private readonly TooltipHandler _tooltipHandler;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly DPInput _input;
        private readonly MapHandler _mapHandler;
        private readonly TabContext _tabContext;
        private readonly DockFactory _dockFactory;
        private readonly ISharedMaterials _sharedMaterials;
        private readonly PreviewAtlasCatalog _previewAtlasCatalog;
        private readonly IDataCatalog _dataCatalog;

        public Tab TargetTab => Tab.Floors;

        private FloorData _selectedFloor;
        private EntityOrientation _orientation = EntityOrientation.Down;
        private FloorPaintMode _paintMode = FloorPaintMode.Floors;
        private bool _dockSupportAuto = true;
        private DockSupportData _selectedDockSupport;
        private DockSupportData _lastPillarSupport;

        private DockStroke _dockStroke = DockStroke.None;
        private int _strokeHeight;
        private int _strokeAnchorLevel;
        private Tile _lastStrokeTile;
        private Tile _previousPaintedTile;
        private readonly HashSet<Tile> _paintedTiles = new HashSet<Tile>();
        private readonly List<Dock> _invalidMarkers = new List<Dock>();

        public FloorPaintMode PaintMode => _paintMode;
        public bool DockSupportAuto => _dockSupportAuto;
        public DockSupportData SelectedDockSupport => _selectedDockSupport;

        public FloorUpdater(IFloorUpdaterView view, TooltipHandler tooltipHandler, CameraCoordinator cameraCoordinator,
            DPInput input, MapHandler mapHandler, TabContext tabContext, DockFactory dockFactory,
            ISharedMaterials sharedMaterials, PreviewAtlasCatalog previewAtlasCatalog, IDataCatalog dataCatalog)
        {
            _view = view;
            _tooltipHandler = tooltipHandler;
            _cameraCoordinator = cameraCoordinator;
            _input = input;
            _mapHandler = mapHandler;
            _tabContext = tabContext;
            _dockFactory = dockFactory;
            _sharedMaterials = sharedMaterials;
            _previewAtlasCatalog = previewAtlasCatalog;
            _dataCatalog = dataCatalog;
        }

        public void Initialize()
        {
            _view.FloorSelected += OnFloorSelected;
            _view.OrientationChanged += OnOrientationChanged;
            _view.PaintModeChanged += OnPaintModeChanged;
            _view.DockSupportChanged += OnDockSupportChanged;

            foreach (FloorData data in _dataCatalog.GetAllFloors())
            {
                foreach (string[] category in data.Categories)
                {
                    _previewAtlasCatalog.TryGetSprite(PreviewAtlasCategory.Floors, data.ShortName, out Sprite sprite);
                    _view.AddFloorEntry(data, category, sprite);
                }

                if (data.SupportsDock)
                {
                    _view.AddDockFloorEntry(data);
                }
            }

            _view.PushSelection();
            _lastPillarSupport = _dataCatalog.GetDockSupport("dwp");
        }

        public void Enable()
        {
            _tabContext.TileSelectionMode = TileSelectionMode.Tiles;
        }

        public void Disable()
        {
            ResetDockStroke();
        }

        private void OnFloorSelected(FloorData data)
        {
            _selectedFloor = data;
        }

        private void OnOrientationChanged(EntityOrientation orientation)
        {
            _orientation = orientation;
        }

        private void OnPaintModeChanged(FloorPaintMode mode)
        {
            _paintMode = mode;
            _view.SetDockSupportSectionVisible(mode == FloorPaintMode.Docks);
            _view.PushSelection();
            ResetDockStroke();
        }

        private void OnDockSupportChanged(bool auto, string supportShortName)
        {
            _dockSupportAuto = auto;
            _selectedDockSupport = supportShortName == null ? null : _dataCatalog.GetDockSupport(supportShortName);
            if (_selectedDockSupport != null && (_selectedDockSupport.Type == DockSupportType.WoodPillar || _selectedDockSupport.Type == DockSupportType.StonePillar))
            {
                _lastPillarSupport = _selectedDockSupport;
            }
        }

        public void Tick()
        {
            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() || _input.UpdatersShared.Deletion.WasReleasedThisFrame())
            {
                _mapHandler.Map.CommandManager.FinishAction();
            }

            if (_paintMode == FloorPaintMode.Docks)
            {
                TickDockMode();
                return;
            }

            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }

            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            LevelEntity levelEntity = raycast.transform.GetComponent<LevelEntity>();

            int floor = 0;
            int x = -1;
            int y = -1;
            if (levelEntity && levelEntity.Valid)
            {
                floor = levelEntity.Level;
                x = levelEntity.Tile.X;
                y = levelEntity.Tile.Y;
            }
            else if (overlayMesh)
            {
                floor = _cameraCoordinator.Current.Level;
                x = Mathf.FloorToInt(raycast.point.x / 4f);
                y = Mathf.FloorToInt(raycast.point.z / 4f);
            }

            if (x < 0 || y < 0)
            {
                return;
            }

            FloorData data = _selectedFloor;
            if (data.Opening && (floor == 0 || floor == -1))
            {
                _tooltipHandler.ShowTooltipText("<color=red><b>It's not possible to place openings/stairs on ground floor</b></color>");
                return;
            }

            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0)
            {
                Dock dockAtTile = _mapHandler.Map[x, y].Dock;
                if (dockAtTile != null && floor == dockAtTile.AnchorLevel)
                {
                    _tooltipHandler.ShowTooltipText("<color=red><b>There's already a dock at this level</b></color>");
                    return;
                }

                _mapHandler.Map[x, y].SetFloor(data, _orientation, floor);
            }
            else if (_input.UpdatersShared.Deletion.ReadValue<float>() > 0)
            {
                _mapHandler.Map[x, y].SetFloor(null, _orientation, floor);
            }
        }

        private void TickDockMode()
        {
            Map map = _mapHandler.Map;

            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() || _input.UpdatersShared.Deletion.WasReleasedThisFrame())
            {
                ResetDockStroke();
                return;
            }

            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }

            int x = Mathf.FloorToInt(raycast.point.x / 4f);
            int y = Mathf.FloorToInt(raycast.point.z / 4f);
            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
            {
                return;
            }

            Tile tile = map[x, y];

            bool placementHeld = _input.UpdatersShared.Placement.ReadValue<float>() > 0;
            bool deletionHeld = _input.UpdatersShared.Deletion.ReadValue<float>() > 0;

            if (_dockStroke == DockStroke.None)
            {
                if (_input.UpdatersShared.Placement.WasPressedThisFrame())
                {
                    TryStartPaintStroke(raycast, tile);
                }
                else if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
                {
                    _dockStroke = DockStroke.Erase;
                    _lastStrokeTile = null;
                }
                return;
            }

            if (_dockStroke == DockStroke.Paint && placementHeld)
            {
                _tooltipHandler.ShowTooltipText("h " + _strokeHeight);
                if (tile != _lastStrokeTile)
                {
                    _lastStrokeTile = tile;
                    if (!_paintedTiles.Contains(tile))
                    {
                        TryPaintTile(tile);
                    }
                }
            }
            else if (_dockStroke == DockStroke.Erase && deletionHeld)
            {
                if (tile != _lastStrokeTile)
                {
                    _lastStrokeTile = tile;
                    Dock dock = tile.Dock;
                    if (dock != null)
                    {
                        map.CommandManager.AddToActionAndExecute(new DockRemovalCommand(map, dock));
                    }
                }
            }
        }

        private void TryStartPaintStroke(RaycastHit raycast, Tile tile)
        {
            Dock hitDock = raycast.transform.GetComponent<Dock>();
            Floor hitFloor = raycast.transform.GetComponent<Floor>();

            if (hitDock != null && hitDock.Tile != null)
            {
                _strokeHeight = hitDock.Height;
                _strokeAnchorLevel = hitDock.AnchorLevel;
            }
            else if (hitFloor != null && hitFloor.Valid && hitFloor.Level >= 0)
            {
                _strokeHeight = tile.GetHeightForLevelOnTile(hitFloor.Level) + hitFloor.Level * 30;
                _strokeAnchorLevel = hitFloor.Level;
            }
            else if (TryPlaceStarterFloor(tile))
            {
                _strokeHeight = tile.GetHeightForLevelOnTile(0);
                _strokeAnchorLevel = 0;
            }
            else
            {
                return;
            }

            _dockStroke = DockStroke.Paint;
            _lastStrokeTile = tile;
            _previousPaintedTile = tile;
            _paintedTiles.Clear();
            _paintedTiles.Add(tile);
        }

        // The clicked tile only anchors the stroke: an empty tile gets a ground floor, and the
        // anchor itself is never converted - docks appear from the second tile onward.
        private bool TryPlaceStarterFloor(Tile tile)
        {
            if (_selectedFloor.Opening)
            {
                _tooltipHandler.ShowTooltipText("<color=red><b>It's not possible to place openings/stairs on ground floor</b></color>");
                return false;
            }

            if (tile.GetTileContent(0) == null)
            {
                tile.SetFloor(_selectedFloor, _orientation, 0);
            }

            return true;
        }

        private void TryPaintTile(Tile tile)
        {
            Map map = _mapHandler.Map;

            DockHardBlock block = DockSupportResolver.GetHardBlock(map, tile.X, tile.Y, _strokeHeight);
            if (block != DockHardBlock.None)
            {
                CreateInvalidMarker(tile);
                _paintedTiles.Add(tile);
                return;
            }

            DockSupportData support = ResolveSupport(map, tile, out EntityOrientation braceDir);
            Dock replacedDock = tile.Dock;
            Dock newDock = _dockFactory.CreateDock(map, tile.X, tile.Y, _strokeHeight, _selectedFloor, support, braceDir,
                _strokeAnchorLevel);
            map.CommandManager.AddToActionAndExecute(new DockPlacementCommand(map, newDock, replacedDock));
            _paintedTiles.Add(tile);
            _previousPaintedTile = tile;
        }

        private DockSupportData ResolveSupport(Map map, Tile tile, out EntityOrientation braceDir)
        {
            if (_dockSupportAuto)
            {
                return DockSupportResolver.ResolveAutoSupport(map, tile.X, tile.Y, _strokeHeight,
                    _lastPillarSupport, _dataCatalog, out braceDir);
            }

            DockSupportData support = _selectedDockSupport;
            if (support != null && support.Type == DockSupportType.Brace)
            {
                DockSupportResolver.TryPickBraceSide(map, tile.X, tile.Y, _strokeHeight, _previousPaintedTile,
                    out braceDir);
            }
            else
            {
                braceDir = EntityOrientation.Up;
            }

            return support;
        }

        private void CreateInvalidMarker(Tile tile)
        {
            GameObject markerObject = new GameObject("Invalid Dock Marker", typeof(Dock));
            Dock marker = markerObject.GetComponent<Dock>();
            marker.Initialize(tile, _strokeHeight, _selectedFloor, null, EntityOrientation.Up,
                _sharedMaterials.GhostMaterial);

            BoxCollider markerCollider = markerObject.GetComponent<BoxCollider>();
            if (markerCollider)
            {
                markerCollider.enabled = false;
            }

            marker.ModelLoaded += OnMarkerModelLoaded;
            if (marker.Model)
            {
                TintMarker(marker.Model);
            }

            _invalidMarkers.Add(marker);
        }

        private void OnMarkerModelLoaded(DynamicModelBehaviour behaviour, GameObject model)
        {
            TintMarker(model);
        }

        private void TintMarker(GameObject model)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor(ShaderPropertyIds.BaseColor, new Color(1f, 0.15f, 0.15f, 0.6f));
            foreach (Renderer childRenderer in model.GetComponentsInChildren<Renderer>())
            {
                childRenderer.sharedMaterial = _sharedMaterials.GhostMaterial;
                childRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ResetDockStroke()
        {
            foreach (Dock marker in _invalidMarkers)
            {
                if (marker)
                {
                    Object.Destroy(marker.gameObject);
                }
            }
            _invalidMarkers.Clear();

            _dockStroke = DockStroke.None;
            _lastStrokeTile = null;
            _previousPaintedTile = null;
            _paintedTiles.Clear();
        }
    }
}
