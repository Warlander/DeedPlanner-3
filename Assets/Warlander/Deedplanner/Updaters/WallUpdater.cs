using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Data.Walls;
using Warlander.Deedplanner.Gui.Updaters;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Updaters
{
    public class WallUpdater : IUpdater
    {
        private readonly IWallUpdaterView _view;
        private readonly DPSettings _settings;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly DPInput _input;
        private readonly MapHandler _mapHandler;
        private readonly TabContext _tabContext;
        private readonly PreviewAtlasCatalog _previewAtlasCatalog;
        private readonly IDataCatalog _dataCatalog;

        public Tab TargetTab => Tab.Walls;

        private WallData _selectedWall;

        public WallUpdater(IWallUpdaterView view, DPSettings settings, CameraCoordinator cameraCoordinator,
            DPInput input, MapHandler mapHandler, TabContext tabContext, PreviewAtlasCatalog previewAtlasCatalog,
            IDataCatalog dataCatalog)
        {
            _view = view;
            _settings = settings;
            _cameraCoordinator = cameraCoordinator;
            _input = input;
            _mapHandler = mapHandler;
            _tabContext = tabContext;
            _previewAtlasCatalog = previewAtlasCatalog;
            _dataCatalog = dataCatalog;
        }

        public void Initialize()
        {
            _view.WallSelected += OnWallSelected;
            _view.ReverseChanged += OnReverseChanged;
            _view.AutomaticReverseChanged += OnAutomaticReverseChanged;

            foreach (WallData data in _dataCatalog.GetAllWalls())
            {
                foreach (string[] category in data.Categories)
                {
                    _previewAtlasCatalog.TryGetSprite(PreviewAtlasCategory.Walls, data.ShortName, out Sprite sprite);
                    _view.AddWallEntry(data, category, sprite);
                }
            }

            _view.SetReverseToggles(_settings.WallReverse, _settings.WallAutomaticReverse);
            _view.PushSelection();
        }

        public void Enable()
        {
            _tabContext.TileSelectionMode = TileSelectionMode.Borders;
        }

        public void Disable() { }

        private void OnWallSelected(WallData data)
        {
            _selectedWall = data;
        }

        private void OnReverseChanged(bool value)
        {
            _settings.Modify(settings =>
            {
                settings.WallReverse = value;
            });
        }

        private void OnAutomaticReverseChanged(bool value)
        {
            _settings.Modify(settings =>
            {
                settings.WallAutomaticReverse = value;
            });
        }

        public void Tick()
        {
            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() || _input.UpdatersShared.Deletion.WasReleasedThisFrame())
            {
                _mapHandler.Map.CommandManager.FinishAction();
            }

            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }

            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            GroundMesh groundMesh = raycast.transform.GetComponent<GroundMesh>();
            LevelEntity levelEntity = raycast.transform.GetComponent<LevelEntity>();
            Wall wallEntity = levelEntity as Wall;

            int floor = 0;
            int x = -1;
            int y = -1;
            bool horizontal = false;

            if (wallEntity && wallEntity.Valid)
            {
                floor = levelEntity.Level;
                if (_cameraCoordinator.Current.Level == floor + 1)
                {
                    floor++;
                }
                x = levelEntity.Tile.X;
                y = levelEntity.Tile.Y;
                EntityType type = levelEntity.Type;
                horizontal = (type == EntityType.Hwall || type == EntityType.Hfence);
            }
            else if (overlayMesh || groundMesh)
            {
                if (overlayMesh)
                {
                    floor = _cameraCoordinator.Current.Level;
                }
                else if (groundMesh)
                {
                    floor = 0;
                }
                TileSelectionHit tileSelectionHit = TileSelection.PositionToTileSelectionHit(raycast.point, TileSelectionMode.Borders);
                TileSelectionTarget target = tileSelectionHit.Target;
                if (target == TileSelectionTarget.Nothing)
                {
                    return;
                }
                x = tileSelectionHit.X;
                y = tileSelectionHit.Y;
                horizontal = (target == TileSelectionTarget.BottomBorder);
            }

            if (x < 0 || y < 0)
            {
                return;
            }

            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0)
            {
                Floor currentFloor = _mapHandler.Map[x, y].GetTileContent(floor) as Floor;
                bool shouldReverse = false;
                if (_settings.WallAutomaticReverse && horizontal)
                {
                    Floor nearFloor = _mapHandler.Map[x, y - 1].GetTileContent(floor) as Floor;
                    shouldReverse = currentFloor && !nearFloor;
                }
                else if (_settings.WallAutomaticReverse && !horizontal)
                {
                    Floor nearFloor = _mapHandler.Map[x - 1, y].GetTileContent(floor) as Floor;
                    shouldReverse = !currentFloor && nearFloor;
                }

                if (_settings.WallReverse)
                {
                    shouldReverse = !shouldReverse;
                }

                if (horizontal)
                {
                    _mapHandler.Map[x, y].SetHorizontalWall(_selectedWall, shouldReverse, floor);
                }
                else
                {
                    _mapHandler.Map[x, y].SetVerticalWall(_selectedWall, shouldReverse, floor);
                }
            }
            else if (_input.UpdatersShared.Deletion.ReadValue<float>() > 0)
            {
                if (floor != _cameraCoordinator.Current.Level)
                {
                    return;
                }
                if (horizontal)
                {
                    _mapHandler.Map[x, y].SetHorizontalWall(null, false, floor);
                }
                else
                {
                    _mapHandler.Map[x, y].SetVerticalWall(null, false, floor);
                }
            }
        }
    }
}
