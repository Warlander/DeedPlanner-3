using System.Collections.Generic;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Persistence;

namespace Warlander.Deedplanner.Editing
{
    public class CaveUpdater : IUpdater
    {
        private readonly ICaveUpdaterView _view;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly TabContext _tabContext;
        private readonly IDataCatalog _dataCatalog;
        private readonly DPInput _input;
        private readonly MapHandler _mapHandler;
        private readonly ICaveRenderOptions _renderOptions;

        private CaveData _primaryData;
        private CaveData _secondaryData;
        private bool _primaryTargeted = true;
        private CaveTool _tool = CaveTool.Pencil;
        private ICaveEditStroke _stroke;

        public Tab TargetTab => Tab.Caves;

        public CaveUpdater(ICaveUpdaterView view, CameraCoordinator cameraCoordinator, TabContext tabContext,
            IDataCatalog dataCatalog, DPInput input, MapHandler mapHandler, ICaveRenderOptions renderOptions)
        {
            _view = view;
            _cameraCoordinator = cameraCoordinator;
            _tabContext = tabContext;
            _dataCatalog = dataCatalog;
            _input = input;
            _mapHandler = mapHandler;
            _renderOptions = renderOptions;
        }

        public void Initialize()
        {
            _view.CaveSelected += OnCaveSelected;
            _view.ToolChanged += tool => _tool = tool;
            _view.PrimaryTargetChanged += value => _primaryTargeted = value;
            _view.CullingChanged += _renderOptions.SetCullingEnabled;

            _primaryData = _dataCatalog.GetCave("sfl");
            _secondaryData = _dataCatalog.DefaultCaveData;
            _view.SetPrimaryData(_primaryData);
            _view.SetSecondaryData(_secondaryData);
            _view.SetCullingEnabled(_renderOptions.IsCullingEnabled());

            foreach (CaveData data in _dataCatalog.GetAllCaves())
            {
                if (!data.Show || data.Entrance)
                {
                    continue;
                }

                foreach (string[] category in data.Categories)
                {
                    _view.AddCaveEntry(data, category);
                }
            }
        }

        public void Enable()
        {
            _tabContext.TileSelectionMode = TileSelectionMode.Tiles;
        }

        public void Disable()
        {
            FinishStroke();
        }

        public void Tick()
        {
            Map map = _mapHandler.Map;
            if (map == null)
            {
                return;
            }

            if (_input.UpdatersShared.Placement.WasReleasedThisFrame()
                || _input.UpdatersShared.Deletion.WasReleasedThisFrame())
            {
                FinishStroke();
            }

            MultiCamera camera = _cameraCoordinator.Current;
            if (!camera.HasCurrentCaveHit)
            {
                return;
            }

            CaveHit hit = camera.CurrentCaveHit;
            if (!_input.UpdatersShared.PickTile.IsPressed())
            {
                Paint(map, hit.CellX, hit.CellY);
                return;
            }

            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                SetPrimary(map[hit.CellX, hit.CellY].Cave.Terrain);
            }
            else if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
            {
                SetSecondary(map[hit.CellX, hit.CellY].Cave.Terrain);
            }
        }

        private void Paint(Map map, int x, int y)
        {
            CaveData data = GetCurrentPaintData();
            if (data == null)
            {
                return;
            }

            if (_tool == CaveTool.Fill)
            {
                if (_input.UpdatersShared.Placement.WasPressedThisFrame()
                    || _input.UpdatersShared.Deletion.WasPressedThisFrame())
                {
                    Fill(map, x, y, data);
                }
                return;
            }

            if (_stroke == null)
            {
                _stroke = map.CaveEditor.BeginTerrainStroke(data, CaveOccupiedCellPolicy.PreserveAndHide);
            }

            _stroke.ApplyAt(x, y);
        }

        private void Fill(Map map, int x, int y, CaveData data)
        {
            CaveData replaced = map[x, y].Cave.Terrain;
            if (replaced == data)
            {
                return;
            }

            using (ICaveEditStroke stroke = map.CaveEditor.BeginTerrainStroke(data,
                       CaveOccupiedCellPolicy.PreserveAndHide))
            {
                var pending = new Stack<CavePaintCell>();
                var visited = new HashSet<int>();
                pending.Push(new CavePaintCell(x, y));

                while (pending.Count > 0)
                {
                    CavePaintCell coordinate = pending.Pop();
                    if (!ContainsCell(map, coordinate.X, coordinate.Y)
                        || !visited.Add(coordinate.Y * map.Width + coordinate.X)
                        || map[coordinate.X, coordinate.Y].Cave.Terrain != replaced)
                    {
                        continue;
                    }

                    stroke.ApplyAt(coordinate.X, coordinate.Y);
                    pending.Push(new CavePaintCell(coordinate.X - 1, coordinate.Y));
                    pending.Push(new CavePaintCell(coordinate.X + 1, coordinate.Y));
                    pending.Push(new CavePaintCell(coordinate.X, coordinate.Y - 1));
                    pending.Push(new CavePaintCell(coordinate.X, coordinate.Y + 1));
                }

                stroke.Commit();
            }
        }

        private static bool ContainsCell(Map map, int x, int y)
        {
            return x >= 0 && y >= 0 && x < map.Width && y < map.Height;
        }

        private void FinishStroke()
        {
            if (_stroke == null)
            {
                return;
            }

            _stroke.Commit();
            _stroke.Dispose();
            _stroke = null;
        }

        private CaveData GetCurrentPaintData()
        {
            if (_input.UpdatersShared.Placement.IsPressed())
            {
                return _primaryData;
            }
            if (_input.UpdatersShared.Deletion.IsPressed())
            {
                return _secondaryData;
            }
            return null;
        }

        private void OnCaveSelected(CaveData data)
        {
            if (data == null)
            {
                return;
            }

            if (_primaryTargeted)
            {
                SetPrimary(data);
            }
            else
            {
                SetSecondary(data);
            }
        }

        private void SetPrimary(CaveData data)
        {
            _primaryData = data;
            _view.SetPrimaryData(data);
        }

        private void SetSecondary(CaveData data)
        {
            _secondaryData = data;
            _view.SetSecondaryData(data);
        }

        private readonly struct CavePaintCell
        {
            public int X { get; }
            public int Y { get; }

            public CavePaintCell(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
