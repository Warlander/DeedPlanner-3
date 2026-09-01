using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Persistence;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Updaters;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeCreationPresenter : IInitializable, IDisposable
    {
        private readonly IBridgeCreationView _view;
        private readonly BridgesUpdater _bridgesUpdater;
        private readonly MapHandler _mapHandler;
        private readonly BridgeFactory _bridgeFactory;
        private readonly IDataCatalog _dataCatalog;

        private TileCoords _start;
        private TileCoords _end;
        private string _lastMaterial;
        private string _lastType;
        private int _hiddenMaterials;

        public BridgeCreationPresenter(IBridgeCreationView view, BridgesUpdater bridgesUpdater,
            MapHandler mapHandler, BridgeFactory bridgeFactory, IDataCatalog dataCatalog)
        {
            _view = view;
            _bridgesUpdater = bridgesUpdater;
            _mapHandler = mapHandler;
            _bridgeFactory = bridgeFactory;
            _dataCatalog = dataCatalog;
        }

        public void Initialize()
        {
            _view.SelectedMaterialChanged += OnSelectedMaterialChanged;
            _view.SelectedTypeChanged += OnSelectedTypeChanged;
            _view.PlaceClicked += OnPlaceClicked;
            _view.CancelClicked += OnCancelClicked;
            _view.BecameActive += OnViewBecameActive;
            _view.BecameInactive += OnViewBecameInactive;
            _bridgesUpdater.TileSelectionChanged += OnTileSelectionChanged;

            OnTileSelectionChanged(_bridgesUpdater.FirstClickedTile, _bridgesUpdater.SecondClickedTile);
            RefreshActionVisibility();
        }

        public void Dispose()
        {
            _view.SelectedMaterialChanged -= OnSelectedMaterialChanged;
            _view.SelectedTypeChanged -= OnSelectedTypeChanged;
            _view.PlaceClicked -= OnPlaceClicked;
            _view.CancelClicked -= OnCancelClicked;
            _view.BecameActive -= OnViewBecameActive;
            _view.BecameInactive -= OnViewBecameInactive;
            _bridgesUpdater.TileSelectionChanged -= OnTileSelectionChanged;
        }

        private void OnTileSelectionChanged(TileCoords first, TileCoords second)
        {
            _start = first;
            _end = second;

            if (first == null || second == null)
            {
                _view.SetMaterials(new List<BridgeData>());
                _view.SetTypes(new List<BridgeType>(), false);
                _view.SetExtraArguments(new List<int>(), false);
                _view.SetMessage(string.Empty);
                RefreshActionVisibility();
                return;
            }

            IReadOnlyList<BridgeData> materials = BuildMaterials(first, second);
            int materialIndex = GetMaterialIndex(materials, _lastMaterial);
            _view.SetMaterials(materials, materialIndex);

            string error = string.Empty;
            bool spanValid = ValidateSpan(first, second, out error);
            if (materials.Count == 0)
            {
                _view.SetMessage(BuildMessage("No bridge material supports this width.", false));
            }
            else if (!spanValid)
            {
                _view.SetMessage(BuildMessage(error));
            }
            else
            {
                _view.SetMessage(BuildMessage("Select a bridge material and type, then click Place."));
            }

            RefreshActionVisibility();
        }

        private void OnSelectedMaterialChanged(BridgeData material)
        {
            if (material != null)
            {
                _lastMaterial = material.Name;
            }

            if (_start == null || _end == null)
            {
                _view.SetTypes(new List<BridgeType>(), false);
                return;
            }

            IReadOnlyList<BridgeType> types = BuildTypes(material, _start, _end);
            int typeIndex = GetTypeIndex(types, _lastType);
            _view.SetTypes(types, types.Count > 1, typeIndex);

            RefreshMessage();
            RefreshActionVisibility();
        }

        private void OnSelectedTypeChanged(BridgeType? type)
        {
            if (type.HasValue)
            {
                _lastType = type.Value.ToString();
                int[] extraArguments = Bridge.GetTypeForBridge(type.Value).ExtraArguments;
                _view.SetExtraArguments(extraArguments, extraArguments.Length > 1);
            }
            else
            {
                _view.SetExtraArguments(new List<int>(), false);
            }

            RefreshMessage();
            RefreshActionVisibility();
        }

        private void OnPlaceClicked()
        {
            BridgeData material = _view.SelectedMaterial;
            BridgeType? type = _view.SelectedType;

            if (material == null || type == null)
            {
                return;
            }

            if (!ValidateSpan(_start, _end, out string error))
            {
                _view.SetMessage(error);
                return;
            }

            int extraArgument = _view.SelectedExtraArgument;
            string segments = BuildDefaultSegments(_start, _end, material, type.Value);
            Map map = _mapHandler.Map;
            Bridge bridge = _bridgeFactory.CreateBridge(map, _start, _end, material,
                type.Value, extraArgument, segments);

            map.CommandManager.AddToActionAndExecute(new BridgePlacementCommand(map, bridge));
            map.CommandManager.FinishAction();
            _bridgesUpdater.ClearTileSelection();
            _bridgesUpdater.SelectBridge(bridge);
        }

        private void OnCancelClicked()
        {
            _bridgesUpdater.ClearTileSelection();
        }

        private void OnViewBecameActive()
        {
            RefreshActionVisibility();
        }

        private void OnViewBecameInactive()
        {
            _view.SetPlaceButtonVisible(false);
            _view.SetCancelButtonVisible(false);
        }

        private void RefreshMessage()
        {
            if (_view.SelectedMaterial == null || _view.SelectedType == null)
            {
                return;
            }

            string error = string.Empty;
            bool valid = ValidateSpan(_start, _end, out error);
            _view.SetMessage(BuildMessage(valid ? "Select a bridge material and type, then click Place." : error));
        }

        private string BuildMessage(string baseMessage, bool includeHiddenMaterialsHint = true)
        {
            if (_start == null || _end == null)
            {
                return baseMessage;
            }

            int spanX = Mathf.Abs(_end.X - _start.X);
            int spanY = Mathf.Abs(_end.Y - _start.Y);
            int length = Mathf.Max(spanX, spanY) - 1;
            int width = Mathf.Min(spanX, spanY) + 1;
            string message = $"Span: {length} long, {width} wide. {baseMessage}";

            if (includeHiddenMaterialsHint && _hiddenMaterials > 0)
            {
                message += " Some materials are hidden - they cannot span a bridge this wide.";
            }

            return message;
        }

        private void RefreshActionVisibility()
        {
            if (!_view.IsActive)
            {
                _view.SetPlaceButtonVisible(false);
                _view.SetCancelButtonVisible(false);
                return;
            }

            _view.SetCancelButtonVisible(true);

            string error = string.Empty;
            bool placeVisible = _start != null && _end != null
                && _view.SelectedMaterial != null
                && _view.SelectedType != null
                && ValidateSpan(_start, _end, out error);
            _view.SetPlaceButtonVisible(placeVisible);
        }

        private IReadOnlyList<BridgeData> BuildMaterials(TileCoords start, TileCoords end)
        {
            List<BridgeData> materials = new List<BridgeData>();
            int bridgeWidth = Mathf.Min(Mathf.Abs(end.X - start.X), Mathf.Abs(end.Y - start.Y)) + 1;

            foreach (BridgeData data in _dataCatalog.GetAllBridges())
            {
                if (data.MaxWidth >= bridgeWidth)
                {
                    materials.Add(data);
                }
            }

            _hiddenMaterials = _dataCatalog.GetAllBridges().Count - materials.Count;
            return materials;
        }

        private IReadOnlyList<BridgeType> BuildTypes(BridgeData material, TileCoords start, TileCoords end)
        {
            List<BridgeType> types = new List<BridgeType>();
            int bridgeLength = Mathf.Max(Mathf.Abs(end.X - start.X), Mathf.Abs(end.Y - start.Y)) - 1;

            foreach (BridgeType type in Enum.GetValues(typeof(BridgeType)))
            {
                if (!material.IsTypeAllowed(type))
                {
                    continue;
                }

                if (type == BridgeType.Arched && bridgeLength < 2)
                {
                    continue;
                }

                types.Add(type);
            }

            return types;
        }

        private int GetMaterialIndex(IReadOnlyList<BridgeData> materials, string lastMaterial)
        {
            if (string.IsNullOrEmpty(lastMaterial))
            {
                return 0;
            }

            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i].Name == lastMaterial)
                {
                    return i;
                }
            }

            return 0;
        }

        private int GetTypeIndex(IReadOnlyList<BridgeType> types, string lastType)
        {
            if (string.IsNullOrEmpty(lastType) || !Enum.TryParse(lastType, out BridgeType type))
            {
                return 0;
            }

            for (int i = 0; i < types.Count; i++)
            {
                if (types[i] == type)
                {
                    return i;
                }
            }

            return 0;
        }

        private bool ValidateSpan(TileCoords start, TileCoords end, out string error)
        {
            error = string.Empty;

            if (start == null || end == null)
            {
                error = "Select start and end tiles.";
                return false;
            }

            Map map = _mapHandler.Map;
            if (map == null)
            {
                error = "No map loaded.";
                return false;
            }

            if ((start.Level >= 0) != (end.Level >= 0))
            {
                error = "Bridge cannot go from surface to cave.";
                return false;
            }

            int minX = Mathf.Min(start.X, end.X);
            int maxX = Mathf.Max(start.X, end.X);
            int minY = Mathf.Min(start.Y, end.Y);
            int maxY = Mathf.Max(start.Y, end.Y);
            int bridgeLength = Mathf.Max(maxX - minX, maxY - minY) - 1;

            if (bridgeLength < 1)
            {
                error = "Bridge must span at least one tile.";
                return false;
            }

            if (bridgeLength > BridgeDefaults.MaxLength)
            {
                error = $"Bridge cannot be longer than {BridgeDefaults.MaxLength} tiles.";
                return false;
            }

            if (minX < 0 || maxX >= map.Width - 1 || minY < 0 || maxY >= map.Height - 1)
            {
                error = "Too close to the map edge - each end of a bridge needs an anchor tile.";
                return false;
            }

            bool vertical = (maxY - minY) > (maxX - minX);

            bool startOnTerrain = start.Level == 0 || start.Level == -1;
            bool endOnTerrain = end.Level == 0 || end.Level == -1;
            if ((startOnTerrain && !AnchorBorderEven(map, start.Level, minX, maxX, minY, maxY, vertical, true))
                || (endOnTerrain && !AnchorBorderEven(map, end.Level, minX, maxX, minY, maxY, vertical, false)))
            {
                error = "Bridge cannot start or end on uneven ground - all tiles at each end must have equal height.";
                return false;
            }

            int spanMinX = minX;
            int spanMaxX = maxX;
            int spanMinY = minY;
            int spanMaxY = maxY;
            if (vertical)
            {
                spanMinY++;
                spanMaxY--;
            }
            else
            {
                spanMinX++;
                spanMaxX--;
            }

            for (int x = spanMinX; x <= spanMaxX; x++)
            {
                for (int y = spanMinY; y <= spanMaxY; y++)
                {
                    if (map[x, y].BridgePart != null)
                    {
                        error = "Bridge would intersect an existing bridge.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        // Heightmap is vertex-based: a tile corner at (x, y) takes its height from map[x, y].
        // The deck border at each end of the bridge is a line of such vertices - all must match.
        private static bool AnchorBorderEven(Map map, int level, int minX, int maxX, int minY, int maxY,
            bool vertical, bool startEdge)
        {
            int from;
            int to;
            int fixedCoord;
            if (vertical)
            {
                from = minX;
                to = maxX + 1;
                fixedCoord = startEdge ? minY + 1 : maxY;
            }
            else
            {
                from = minY;
                to = maxY + 1;
                fixedCoord = startEdge ? minX + 1 : maxX;
            }

            int? borderHeight = null;
            for (int i = from; i <= to; i++)
            {
                Tile tile = vertical ? map[i, fixedCoord] : map[fixedCoord, i];
                int height = level < 0 ? tile.CaveHeight : tile.SurfaceHeight;

                if (borderHeight == null)
                {
                    borderHeight = height;
                }
                else if (height != borderHeight.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private string BuildDefaultSegments(TileCoords start, TileCoords end, BridgeData material, BridgeType type)
        {
            int bridgeLength = Mathf.Max(Mathf.Abs(end.X - start.X), Mathf.Abs(end.Y - start.Y)) - 1;
            return BridgeDefaults.GetDefaultSegments(type, material, bridgeLength);
        }
    }
}
