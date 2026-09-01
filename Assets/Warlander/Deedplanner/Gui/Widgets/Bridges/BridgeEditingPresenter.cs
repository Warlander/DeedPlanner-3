using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer.Unity;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Bridges;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeEditingPresenter : IInitializable, IDisposable
    {
        private readonly IBridgeEditingView _view;
        private readonly BridgesUpdater _bridgesUpdater;
        private readonly MapHandler _mapHandler;
        private readonly IDataCatalog _dataCatalog;

        public BridgeEditingPresenter(IBridgeEditingView view, BridgesUpdater bridgesUpdater, MapHandler mapHandler,
            IDataCatalog dataCatalog)
        {
            _view = view;
            _bridgesUpdater = bridgesUpdater;
            _mapHandler = mapHandler;
            _dataCatalog = dataCatalog;
        }

        public void Initialize()
        {
            _view.DeleteClicked += OnDeleteClicked;
            _view.CancelClicked += OnCancelClicked;
            _view.BecameActive += OnViewBecameActive;
            _view.BecameInactive += OnViewBecameInactive;
            _view.SelectedMaterialChanged += OnSelectedMaterialChanged;
            _view.SelectedExtraArgumentChanged += OnSelectedExtraArgumentChanged;
            _bridgesUpdater.SelectedBridgeChanged += OnSelectedBridgeChanged;

            RefreshActionVisibility();
            RefreshMaterials();
            RefreshExtraArguments();
        }

        public void Dispose()
        {
            _view.DeleteClicked -= OnDeleteClicked;
            _view.CancelClicked -= OnCancelClicked;
            _view.BecameActive -= OnViewBecameActive;
            _view.BecameInactive -= OnViewBecameInactive;
            _view.SelectedMaterialChanged -= OnSelectedMaterialChanged;
            _view.SelectedExtraArgumentChanged -= OnSelectedExtraArgumentChanged;
            _bridgesUpdater.SelectedBridgeChanged -= OnSelectedBridgeChanged;
        }

        private void OnSelectedBridgeChanged()
        {
            RefreshActionVisibility();
            RefreshMaterials();
            RefreshExtraArguments();
        }

        private void OnDeleteClicked()
        {
            Bridge selectedBridge = _bridgesUpdater.SelectedBridge;
            if (selectedBridge == null)
            {
                return;
            }

            Map map = _mapHandler.Map;
            map.CommandManager.AddToActionAndExecute(new BridgeRemovalCommand(map, selectedBridge));
            map.CommandManager.FinishAction();
            _bridgesUpdater.ClearBridgeSelection();
        }

        private void OnCancelClicked()
        {
            _bridgesUpdater.ClearBridgeSelection();
        }

        private void OnSelectedMaterialChanged(BridgeData material)
        {
            Bridge bridge = _bridgesUpdater.SelectedBridge;
            if (bridge == null || material == null || material == bridge.Data)
            {
                return;
            }

            string oldSegments = bridge.GetSegmentsString();
            string newSegments = BuildSegmentsForMaterial(bridge, material);

            Map map = _mapHandler.Map;
            map.CommandManager.AddToActionAndExecute(new BridgeMaterialChangeCommand(
                map, bridge, bridge.Data, material, oldSegments, newSegments));
            map.CommandManager.FinishAction();
        }

        private string BuildSegmentsForMaterial(Bridge bridge, BridgeData material)
        {
            if (bridge.Type == BridgeType.Flat)
            {
                BridgeStructureUtils.ConstructFlatBridge(bridge.GetSupportPositions(), out BridgePartType?[] segments);
                if (material.Name == "wood")
                {
                    segments = BridgeStructureUtils.SubstituteWoodParts(segments);
                }

                return BridgePartTypeUtils.EncodeSegments(segments.Select(segment => segment.Value).ToArray());
            }

            int length = bridge.GetSegmentsString().Length;
            return BridgeDefaults.GetDefaultSegments(bridge.Type, material, length);
        }

        private void OnViewBecameActive()
        {
            RefreshActionVisibility();
            RefreshMaterials();
            RefreshExtraArguments();
        }

        private void OnViewBecameInactive()
        {
            _view.SetDeleteButtonVisible(false);
            _view.SetCancelButtonVisible(false);
            _view.SetMaterialsVisible(false);
            _view.SetExtraArgumentsVisible(false);
        }

        private void RefreshActionVisibility()
        {
            if (!_view.IsActive)
            {
                _view.SetDeleteButtonVisible(false);
                _view.SetCancelButtonVisible(false);
                return;
            }

            bool hasSelection = _bridgesUpdater.SelectedBridge != null;
            _view.SetDeleteButtonVisible(hasSelection);
            _view.SetCancelButtonVisible(true);
        }

        private void RefreshMaterials()
        {
            Bridge bridge = _bridgesUpdater.SelectedBridge;
            if (!_view.IsActive || bridge == null)
            {
                _view.SetMaterialsVisible(false);
                return;
            }

            _view.SetTypeLabel($"Type: {bridge.Type}");

            int width = Mathf.Min(
                Mathf.Abs(bridge.SecondTile.x - bridge.FirstTile.x),
                Mathf.Abs(bridge.SecondTile.y - bridge.FirstTile.y)) + 1;

            List<BridgeData> materials = _dataCatalog.GetAllBridges()
                .Where(data => data.MaxWidth >= width && data.IsTypeAllowed(bridge.Type))
                .ToList();

            if (materials.Count <= 1)
            {
                _view.SetMaterialsVisible(false);
                return;
            }

            _view.SetMaterialsVisible(true);
            int currentIndex = materials.IndexOf(bridge.Data);
            _view.SetMaterials(materials, Mathf.Max(currentIndex, 0));
        }

        private void OnSelectedExtraArgumentChanged(int value)
        {
            Bridge bridge = _bridgesUpdater.SelectedBridge;
            if (bridge == null || value == bridge.AdditionalData)
            {
                return;
            }

            Map map = _mapHandler.Map;
            map.CommandManager.AddToActionAndExecute(new BridgeExtraArgumentChangeCommand(
                map, bridge, bridge.AdditionalData, value));
            map.CommandManager.FinishAction();
        }

        private void RefreshExtraArguments()
        {
            Bridge bridge = _bridgesUpdater.SelectedBridge;
            if (!_view.IsActive || bridge == null)
            {
                _view.SetExtraArgumentsVisible(false);
                return;
            }

            int[] extraArguments = Bridge.GetTypeForBridge(bridge.Type).ExtraArguments;
            if (extraArguments.Length <= 1)
            {
                _view.SetExtraArgumentsVisible(false);
                return;
            }

            _view.SetExtraArgumentsVisible(true);
            int currentIndex = System.Array.IndexOf(extraArguments, bridge.AdditionalData);
            _view.SetExtraArguments(extraArguments, Mathf.Max(currentIndex, 0));
        }
    }
}
