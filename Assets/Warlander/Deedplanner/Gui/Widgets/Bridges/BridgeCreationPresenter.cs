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

        private TileCoords _start;
        private TileCoords _end;
        private string _lastMaterial;
        private string _lastType;

        public BridgeCreationPresenter(IBridgeCreationView view, BridgesUpdater bridgesUpdater,
            MapHandler mapHandler, BridgeFactory bridgeFactory)
        {
            _view = view;
            _bridgesUpdater = bridgesUpdater;
            _mapHandler = mapHandler;
            _bridgeFactory = bridgeFactory;
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
                _view.SetMessage("No bridge material supports this width.");
            }
            else if (!spanValid)
            {
                _view.SetMessage(error);
            }
            else
            {
                _view.SetMessage("Select a bridge material and type, then click Place.");
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

            int extraArgument = GetDefaultExtraArgument(type.Value);
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
            _view.SetMessage(valid ? "Select a bridge material and type, then click Place." : error);
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

            foreach (BridgeData data in Database.Bridges.Values)
            {
                if (data.MaxWidth >= bridgeWidth)
                {
                    materials.Add(data);
                }
            }

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

        private int GetDefaultExtraArgument(BridgeType type)
        {
            switch (type)
            {
                case BridgeType.Flat:
                    return 0;
                case BridgeType.Arched:
                    return 5;
                case BridgeType.Rope:
                    return 3;
                default:
                    return 0;
            }
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
                error = "Bridge endpoints are too close to the map edge.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private string BuildDefaultSegments(TileCoords start, TileCoords end, BridgeData material, BridgeType type)
        {
            int bridgeLength = Mathf.Max(Mathf.Abs(end.X - start.X), Mathf.Abs(end.Y - start.Y)) - 1;
            return BridgeDefaults.GetDefaultSegments(type, material, bridgeLength);
        }
    }
}
