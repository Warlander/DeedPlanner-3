using Warlander.Deedplanner.Persistence;
using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Domain.Entities.Walls;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner.Ui.Tooltips;

namespace Warlander.Deedplanner.Editing
{
    public class WallUpdater : IUpdater
    {
        private readonly IWallUpdaterView _view;
        private readonly EditingSettings _settings;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly DPInput _input;
        private readonly MapHandler _mapHandler;
        private readonly TabContext _tabContext;
        private readonly PreviewAtlasCatalog _previewAtlasCatalog;
        private readonly IDataCatalog _dataCatalog;
        private readonly IMapEditFacade _mapEditFacade;
        private readonly TooltipHandler _tooltipHandler;

        public Tab TargetTab => Tab.Walls;

        private WallData _selectedWall;
        private int? _paintLevel;
        private int? _paintDockHeight;
        private int _paintDockAnchorLevel;

        public WallUpdater(IWallUpdaterView view, EditingSettings settings, CameraCoordinator cameraCoordinator,
            DPInput input, MapHandler mapHandler, TabContext tabContext, PreviewAtlasCatalog previewAtlasCatalog,
            IDataCatalog dataCatalog, IMapEditFacade mapEditFacade, TooltipHandler tooltipHandler)
        {
            _view = view;
            _settings = settings;
            _cameraCoordinator = cameraCoordinator;
            _input = input;
            _mapHandler = mapHandler;
            _tabContext = tabContext;
            _previewAtlasCatalog = previewAtlasCatalog;
            _dataCatalog = dataCatalog;
            _mapEditFacade = mapEditFacade;
            _tooltipHandler = tooltipHandler;
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

        public void Disable()
        {
            _paintLevel = null;
        }

        private void OnWallSelected(WallData data)
        {
            _selectedWall = data;
        }

        private void OnReverseChanged(bool value)
        {
            _settings.WallReverse = value;
        }

        private void OnAutomaticReverseChanged(bool value)
        {
            _settings.WallAutomaticReverse = value;
        }

        public void Tick()
        {
            bool painting = _input.UpdatersShared.Placement.ReadValue<float>() > 0;
            if (!painting)
            {
                _paintLevel = null;
            }
            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() || _input.UpdatersShared.Deletion.WasReleasedThisFrame())
            {
                _mapEditFacade.FinishAction();
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
            Floor hitFloor = levelEntity as Floor;
            Dock hitDock = raycast.transform.GetComponent<Dock>();

            int floor = 0;
            int x = -1;
            int y = -1;
            bool horizontal = false;
            WallSupportSide supportSide = WallSupportSide.Ground;

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
                supportSide = wallEntity.SupportSide;
            }
            else if (overlayMesh || groundMesh || hitDock || hitFloor || _cameraCoordinator.Current.HasCurrentCaveHit)
            {
                if (hitDock)
                {
                    floor = hitDock.AnchorLevel;
                }
                else if (hitFloor)
                {
                    floor = hitFloor.Level;
                    hitDock = hitFloor.Tile.GetDock(floor < 0 ? DockRealm.Cave : DockRealm.Surface);
                }
                else if (overlayMesh)
                {
                    floor = _cameraCoordinator.Current.Level;
                }
                else if (groundMesh)
                {
                    floor = 0;
                }
                else
                {
                    floor = _cameraCoordinator.Current.Level;
                    int caveX = _cameraCoordinator.Current.CurrentCaveHit.CellX;
                    int caveY = _cameraCoordinator.Current.CurrentCaveHit.CellY;
                    if (!_mapHandler.Map.Caves.IsOpen(caveX, caveY))
                    {
                        return;
                    }
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
                if (hitDock != null)
                {
                    supportSide = hitDock.Tile.X == x && hitDock.Tile.Y == y
                        ? WallSupportSide.Tile : WallSupportSide.OppositeTile;
                }
            }

            Map map = _mapHandler.Map;
            if (x < 0 || y < 0 || x > map.Width || y > map.Height)
            {
                return;
            }

            if (painting && !TryResolvePaintBase(map, x, y, horizontal, ref floor, ref supportSide))
            {
                _tooltipHandler.ShowTooltipText("No support deck at the stroke's locked height on this edge.", 11);
                return;
            }

            Wall existingWall = horizontal ? map[x, y].GetHorizontalWallOrFence(floor)
                : map[x, y].GetVerticalWallOrFence(floor);
            bool differentBase = existingWall != null && existingWall.SupportSide != supportSide;
            string baseTooltip = supportSide == WallSupportSide.Ground ? "Wall base: Ground" : "Wall base: Support deck";
            _tooltipHandler.ShowTooltipText(painting ? baseTooltip + " (locked)" : baseTooltip, 10);
            if (differentBase)
            {
                _tooltipHandler.ShowTooltipText(supportSide == WallSupportSide.Ground
                    ? "Click to replan this wall on the ground."
                    : "Click to replan this wall on the deck.", 11);
            }

            if (painting)
            {
                Floor currentFloor = GetFloor(_mapHandler.Map[x, y], floor);
                bool shouldReverse = false;
                if (_settings.WallAutomaticReverse && horizontal)
                {
                    Floor nearFloor = GetFloor(_mapHandler.Map[x, y - 1], floor);
                    shouldReverse = ShouldReverseAutomatically(currentFloor, nearFloor, true);
                }
                else if (_settings.WallAutomaticReverse && !horizontal)
                {
                    Floor nearFloor = GetFloor(_mapHandler.Map[x - 1, y], floor);
                    shouldReverse = ShouldReverseAutomatically(currentFloor, nearFloor, false);
                }

                if (_settings.WallReverse)
                {
                    shouldReverse = !shouldReverse;
                }

                if (horizontal)
                {
                    _mapEditFacade.SetHorizontalWall(x, y, _selectedWall, shouldReverse, floor, supportSide);
                }
                else
                {
                    _mapEditFacade.SetVerticalWall(x, y, _selectedWall, shouldReverse, floor, supportSide);
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
                    _mapEditFacade.SetHorizontalWall(x, y, null, false, floor);
                }
                else
                {
                    _mapEditFacade.SetVerticalWall(x, y, null, false, floor);
                }
            }
        }

        private bool TryResolvePaintBase(Map map, int x, int y, bool horizontal, ref int level,
            ref WallSupportSide supportSide)
        {
            DockRealm realm = (_paintLevel ?? level) < 0 ? DockRealm.Cave : DockRealm.Surface;
            Tile tile = map[x, y];
            Dock own = tile.GetDock(realm);
            Dock opposite = map.GetRelativeTile(tile, horizontal ? 0 : -1, horizontal ? -1 : 0)?.GetDock(realm);
            if (!_paintLevel.HasValue)
            {
                _paintLevel = level;
                Dock dock = supportSide == WallSupportSide.Ground ? null
                    : supportSide == WallSupportSide.Tile ? own : opposite;
                _paintDockHeight = dock != null ? dock.Height : null;
                _paintDockAnchorLevel = dock != null ? dock.AnchorLevel : 0;
            }

            level = _paintLevel.Value;
            if (!_paintDockHeight.HasValue)
            {
                supportSide = WallSupportSide.Ground;
                return true;
            }

            bool ownMatches = own != null && own.Height == _paintDockHeight.Value
                && own.AnchorLevel == _paintDockAnchorLevel;
            bool oppositeMatches = opposite != null && opposite.Height == _paintDockHeight.Value
                && opposite.AnchorLevel == _paintDockAnchorLevel;
            if (oppositeMatches && (supportSide == WallSupportSide.OppositeTile || !ownMatches))
            {
                supportSide = WallSupportSide.OppositeTile;
                return true;
            }
            if (ownMatches)
            {
                supportSide = WallSupportSide.Tile;
                return true;
            }
            return false;
        }

        private static bool ShouldReverseAutomatically(Floor currentFloor, Floor nearFloor, bool horizontal)
        {
            return horizontal ? currentFloor && !nearFloor : !currentFloor && nearFloor;
        }

        private static Floor GetFloor(Tile tile, int floor)
        {
            return tile?.GetTileContent(floor) as Floor;
        }
    }
}
