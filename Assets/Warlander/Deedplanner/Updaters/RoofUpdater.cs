using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Gui.Updaters;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;

namespace Warlander.Deedplanner.Updaters
{
    public class RoofUpdater : IUpdater
    {
        private readonly IRoofUpdaterView _view;
        private readonly TooltipHandler _tooltipHandler;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly DPInput _input;
        private readonly MapHandler _mapHandler;
        private readonly TabContext _tabContext;
        private readonly IDataCatalog _dataCatalog;

        public Tab TargetTab => Tab.Roofs;

        private RoofData _selectedRoof;

        public RoofUpdater(IRoofUpdaterView view, TooltipHandler tooltipHandler, CameraCoordinator cameraCoordinator,
            DPInput input, MapHandler mapHandler, TabContext tabContext, IDataCatalog dataCatalog)
        {
            _view = view;
            _tooltipHandler = tooltipHandler;
            _cameraCoordinator = cameraCoordinator;
            _input = input;
            _mapHandler = mapHandler;
            _tabContext = tabContext;
            _dataCatalog = dataCatalog;
        }

        public void Initialize()
        {
            _view.RoofSelected += OnRoofSelected;

            foreach (RoofData data in _dataCatalog.GetAllRoofs())
            {
                _view.AddRoofEntry(data);
            }

            _view.PushSelection();
        }

        public void Enable()
        {
            _tabContext.TileSelectionMode = TileSelectionMode.Tiles;
        }

        public void Disable() { }

        private void OnRoofSelected(RoofData data)
        {
            _selectedRoof = data;
        }

        public void Tick()
        {
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

            if (floor == 0 || floor == -1)
            {
                _tooltipHandler.ShowTooltipText("<color=red><b>It's not possible to place roofs on ground floor</b></color>");
                return;
            }

            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0)
            {
                _mapHandler.Map[x, y].SetRoof(_selectedRoof, floor);
            }
            else if (_input.UpdatersShared.Deletion.ReadValue<float>() > 0)
            {
                _mapHandler.Map[x, y].SetRoof(null, floor);
            }

            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() || _input.UpdatersShared.Deletion.WasReleasedThisFrame())
            {
                _mapHandler.Map.CommandManager.FinishAction();
            }
        }
    }
}
