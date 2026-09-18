using Warlander.Deedplanner.Ui.Widgets;
using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Persistence;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Bridges;

namespace Warlander.Deedplanner.Bridges.Widgets
{
    public class BridgeCreationPresenter : IInitializable, IDisposable
    {
        private readonly IBridgeCreationView _view;
        private readonly BridgesUpdater _bridgesUpdater;
        private readonly MapHandler _mapHandler;
        private readonly IDataCatalog _dataCatalog;
        private readonly IMapEditFacade _mapEditFacade;

        private TileCoords _start;
        private TileCoords _end;
        private string _lastMaterial;
        private string _lastType;
        private int _hiddenMaterials;

        public BridgeCreationPresenter(IBridgeCreationView view, BridgesUpdater bridgesUpdater,
            MapHandler mapHandler, IDataCatalog dataCatalog, IMapEditFacade mapEditFacade)
        {
            _view = view;
            _bridgesUpdater = bridgesUpdater;
            _mapHandler = mapHandler;
            _dataCatalog = dataCatalog;
            _mapEditFacade = mapEditFacade;
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
            var request = new BridgePlacementRequest(_start, _end, material, type.Value, extraArgument, segments);
            Bridge bridge = _mapEditFacade.PlaceBridge(request);
            _bridgesUpdater.ClearTileSelection();
            if (bridge != null)
            {
                _bridgesUpdater.SelectBridge(bridge);
            }
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
            return BridgeSpanValidator.Validate(_mapHandler.Map, start, end, out error);
        }

        private string BuildDefaultSegments(TileCoords start, TileCoords end, BridgeData material, BridgeType type)
        {
            int bridgeLength = Mathf.Max(Mathf.Abs(end.X - start.X), Mathf.Abs(end.Y - start.Y)) - 1;
            return BridgeDefaults.GetDefaultSegments(type, material, bridgeLength);
        }
    }
}
