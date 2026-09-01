using Warlander.Deedplanner.Persistence;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Editing
{
    public class GroundUpdater : IUpdater
    {
        private readonly IGroundUpdaterView _view;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly DPInput _input;
        private readonly MapHandler _mapHandler;
        private readonly TabContext _tabContext;
        private readonly PreviewAtlasCatalog _previewAtlasCatalog;
        private readonly IDataCatalog _dataCatalog;

        public Tab TargetTab => Tab.Ground;

        private GroundData _leftClickData;
        private GroundData _rightClickData;
        private bool _leftClickTargeted = true;
        private GroundTool _tool = GroundTool.Pencil;
        private bool _editCorners = true;

        public GroundUpdater(IGroundUpdaterView view, CameraCoordinator cameraCoordinator, DPInput input,
            MapHandler mapHandler, TabContext tabContext, PreviewAtlasCatalog previewAtlasCatalog,
            IDataCatalog dataCatalog)
        {
            _view = view;
            _cameraCoordinator = cameraCoordinator;
            _input = input;
            _mapHandler = mapHandler;
            _tabContext = tabContext;
            _previewAtlasCatalog = previewAtlasCatalog;
            _dataCatalog = dataCatalog;
        }

        public void Initialize()
        {
            _view.GroundSelected += OnGroundSelected;
            _view.ToolChanged += OnToolChanged;
            _view.LeftClickTargetChanged += OnLeftClickTargetChanged;
            _view.EditCornersChanged += OnEditCornersChanged;

            _leftClickData = _dataCatalog.DefaultGroundData;
            _view.SetLeftClickData(_leftClickData, GetSprite(_leftClickData));
            _rightClickData = _dataCatalog.DefaultSecondaryGroundData;
            _view.SetRightClickData(_rightClickData, GetSprite(_rightClickData));

            foreach (GroundData data in _dataCatalog.GetAllGrounds())
            {
                foreach (string[] category in data.Categories)
                {
                    _view.AddGroundEntry(data, category, GetSprite(data));
                }
            }
        }

        public void Enable()
        {
            UpdateSelectionMode();
        }

        public void Disable() { }

        private void UpdateSelectionMode()
        {
            if (_editCorners)
            {
                _tabContext.TileSelectionMode = TileSelectionMode.Everything;
            }
            else
            {
                _tabContext.TileSelectionMode = TileSelectionMode.Tiles;
            }
        }

        private void OnGroundSelected(GroundData groundData)
        {
            if (_leftClickTargeted)
            {
                _leftClickData = groundData;
                _view.SetLeftClickData(groundData, GetSprite(groundData));
            }
            else
            {
                _rightClickData = groundData;
                _view.SetRightClickData(groundData, GetSprite(groundData));
            }
        }

        private void OnToolChanged(GroundTool tool)
        {
            _tool = tool;
        }

        private void OnLeftClickTargetChanged(bool targeted)
        {
            _leftClickTargeted = targeted;
        }

        private void OnEditCornersChanged(bool editCorners)
        {
            _editCorners = editCorners;
            UpdateSelectionMode();
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

            Map map = _mapHandler.Map;
            int tileX = Mathf.FloorToInt(raycast.point.x / 4f);
            int tileZ = Mathf.FloorToInt(raycast.point.z / 4f);
            Tile tile = map[tileX, tileZ];
            Ground ground = tile.Ground;

            if (_input.GroundUpdater.PickTile.IsPressed())
            {
                if (_input.UpdatersShared.Placement.WasPressedThisFrame())
                {
                    _leftClickData = ground.Data;
                    _view.SetLeftClickData(_leftClickData, GetSprite(_leftClickData));
                }
                else if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
                {
                    _rightClickData = ground.Data;
                    _view.SetRightClickData(_rightClickData, GetSprite(_rightClickData));
                }
            }

            GroundData currentClickData = GetCurrentClickData();
            if (currentClickData == null)
            {
                return;
            }

            if (_tool == GroundTool.Pencil)
            {
                if (_editCorners && _leftClickData.Diagonal)
                {
                    TileSelectionHit hit = TileSelection.PositionToTileSelectionHit(raycast.point, TileSelectionMode.TilesAndCorners);
                    if (hit.Target == TileSelectionTarget.InnerTile || hit.Target == TileSelectionTarget.Nothing)
                    {
                        ground.RoadDirection = RoadDirection.Center;
                    }
                    else if (hit.X - tile.X == 0 && hit.Y - tile.Y == 0)
                    {
                        ground.RoadDirection = RoadDirection.SW;
                    }
                    else if (hit.X - tile.X == 1 && hit.Y - tile.Y == 0)
                    {
                        ground.RoadDirection = RoadDirection.SE;
                    }
                    else if (hit.X - tile.X == 0 && hit.Y - tile.Y == 1)
                    {
                        ground.RoadDirection = RoadDirection.NW;
                    }
                    else if (hit.X - tile.X == 1 && hit.Y - tile.Y == 1)
                    {
                        ground.RoadDirection = RoadDirection.NE;
                    }
                }
                else
                {
                    ground.RoadDirection = RoadDirection.Center;
                }
                ground.Data = currentClickData;
            }
            else if (_tool == GroundTool.Fill)
            {
                GroundData toReplace = tile.Ground.Data;
                FloodFill(tile, currentClickData, toReplace);
            }
        }

        private void FloodFill(Tile tile, GroundData data, GroundData toReplace)
        {
            if (data == toReplace)
            {
                return;
            }
            Map map = _mapHandler.Map;
            Stack<Tile> checkStack = new Stack<Tile>();
            checkStack.Push(tile);
            HashSet<Tile> tilesToChange = new HashSet<Tile>();

            while (checkStack.Count != 0)
            {
                Tile anchor = checkStack.Pop();
                if (anchor.Ground.Data == toReplace && !tilesToChange.Contains(anchor))
                {
                    tilesToChange.Add(anchor);
                    AddTileIfNotNull(checkStack, map[anchor.X - 1, anchor.Y]);
                    AddTileIfNotNull(checkStack, map[anchor.X + 1, anchor.Y]);
                    AddTileIfNotNull(checkStack, map[anchor.X, anchor.Y - 1]);
                    AddTileIfNotNull(checkStack, map[anchor.X, anchor.Y + 1]);
                }
            }

            foreach (Tile tileToChange in tilesToChange)
            {
                tileToChange.Ground.Data = data;
            }
        }

        private void AddTileIfNotNull(Stack<Tile> stack, Tile tile)
        {
            if (tile != null)
            {
                stack.Push(tile);
            }
        }

        private GroundData GetCurrentClickData()
        {
            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0)
            {
                return _leftClickData;
            }
            else if (_input.UpdatersShared.Deletion.ReadValue<float>() > 0)
            {
                return _rightClickData;
            }

            return null;
        }

        private Sprite GetSprite(GroundData data)
        {
            _previewAtlasCatalog.TryGetSprite(PreviewAtlasCategory.Grounds, data.ShortName, out Sprite sprite);
            return sprite;
        }
    }
}
