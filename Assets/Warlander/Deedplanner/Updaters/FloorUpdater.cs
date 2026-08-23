using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Docks;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Gui.Updaters;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;

namespace Warlander.Deedplanner.Updaters
{
    public class FloorUpdater : IUpdater
    {
        private readonly IFloorUpdaterView _view;
        private readonly TooltipHandler _tooltipHandler;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly DPInput _input;
        private readonly MapHandler _mapHandler;
        private readonly TabContext _tabContext;

        public Tab TargetTab => Tab.Floors;

        private FloorData _selectedFloor;
        private EntityOrientation _orientation = EntityOrientation.Down;
        private FloorPaintMode _paintMode = FloorPaintMode.Floors;
        private bool _dockSupportAuto = true;
        private DockSupportData _selectedDockSupport;

        public FloorPaintMode PaintMode => _paintMode;
        public bool DockSupportAuto => _dockSupportAuto;
        public DockSupportData SelectedDockSupport => _selectedDockSupport;

        public FloorUpdater(IFloorUpdaterView view, TooltipHandler tooltipHandler, CameraCoordinator cameraCoordinator,
            DPInput input, MapHandler mapHandler, TabContext tabContext)
        {
            _view = view;
            _tooltipHandler = tooltipHandler;
            _cameraCoordinator = cameraCoordinator;
            _input = input;
            _mapHandler = mapHandler;
            _tabContext = tabContext;
        }

        public void Initialize()
        {
            _view.FloorSelected += OnFloorSelected;
            _view.OrientationChanged += OnOrientationChanged;
            _view.PaintModeChanged += OnPaintModeChanged;
            _view.DockSupportChanged += OnDockSupportChanged;

            foreach (FloorData data in Database.Floors.Values)
            {
                foreach (string[] category in data.Categories)
                {
                    _view.AddFloorEntry(data, category);
                }
            }

            _view.PushSelection();
        }

        public void Enable()
        {
            _tabContext.TileSelectionMode = TileSelectionMode.Tiles;
        }

        public void Disable() { }

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
        }

        private void OnDockSupportChanged(bool auto, DockSupportData support)
        {
            _dockSupportAuto = auto;
            _selectedDockSupport = support;
        }

        public void Tick()
        {
            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() || _input.UpdatersShared.Deletion.WasReleasedThisFrame())
            {
                _mapHandler.Map.CommandManager.FinishAction();
            }

            // Dock painting lands with the drag-paint interaction; floor painting stays out of dock mode.
            if (_paintMode == FloorPaintMode.Docks)
            {
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
                _mapHandler.Map[x, y].SetFloor(data, _orientation, floor);
            }
            else if (_input.UpdatersShared.Deletion.ReadValue<float>() > 0)
            {
                _mapHandler.Map[x, y].SetFloor(null, _orientation, floor);
            }
        }
    }
}
