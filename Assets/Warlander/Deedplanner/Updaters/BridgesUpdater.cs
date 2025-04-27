using System;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Gui.Widgets.Bridges;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Updaters
{
    public class BridgesUpdater : AbstractUpdater
    {
        [Inject] private GameManager _gameManager;
        [Inject] private CameraCoordinator _cameraCoordinator;
        [Inject] private DPInput _input;
        [Inject] private TooltipHandler _tooltipHandler;
        [Inject] private BridgeTabSwapper _bridgeTabSwapper;

        public event Action SelectedBridgeChanged;
        
        public Bridge SelectedBridge { get; private set; }
        
        private Bridge _lastFrameHoveredBridge;
        private TileCoords _firstClickedTile;
        private TileCoords _secondClickedTile;

        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Nothing;
        }

        private void Update()
        {
            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }
            
            BridgePart bridgePart = raycast.transform.GetComponent<BridgePart>();
            Bridge bridge = bridgePart != null ? bridgePart.ParentBridge : null;

            UpdateBridgeHover(bridge);

            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            
            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                OnBridgeClicked(bridge);
                if (overlayMesh != null)
                {
                    int floor = _cameraCoordinator.Current.Level;
                    int x = Mathf.FloorToInt(raycast.point.x / 4f);
                    int y = Mathf.FloorToInt(raycast.point.z / 4f);
                    
                    OnMapClicked(x, y, floor);
                }
            }

            if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
            {
                OnBridgeDeselected();
                OnMapDeselect();
            }

            if (bridge != null)
            {
                _tooltipHandler.ShowTooltipText($"{bridge.Data.Name} bridge");
            }
        }

        private void UpdateBridgeHover(Bridge bridge)
        {
            if (_lastFrameHoveredBridge == bridge)
            {
                return;
            }
            
            if (_lastFrameHoveredBridge != null && IsSelectedBridge(_lastFrameHoveredBridge) == false)
            {
                _lastFrameHoveredBridge.DisableHighlighting();
            }

            _lastFrameHoveredBridge = bridge;

            if (bridge != null && IsSelectedBridge(bridge) == false)
            {
                bridge.EnableHighlighting(OutlineType.Neutral);
            }
        }

        private bool IsSelectedBridge(Bridge bridge)
        {
            return bridge != null && bridge == SelectedBridge;
        }

        private void OnBridgeClicked(Bridge bridge)
        {
            if (bridge == null)
            {
                OnBridgeDeselected();
                return;
            }
            
            if (SelectedBridge != null)
            {
                SelectedBridge.DisableHighlighting();
            }

            bool bridgeChanged = SelectedBridge != bridge;
            SelectedBridge = bridge;
            SelectedBridge.EnableHighlighting(OutlineType.Positive);

            _firstClickedTile = null;
            _secondClickedTile = null;

            RefreshUIState();
            
            if (bridgeChanged)
            {
                SelectedBridgeChanged?.Invoke();
            }
        }

        private void OnBridgeDeselected()
        {
            if (SelectedBridge != null)
            {
                if (SelectedBridge == _lastFrameHoveredBridge)
                {
                    SelectedBridge.EnableHighlighting(OutlineType.Neutral);
                }
                else
                {
                    SelectedBridge.DisableHighlighting();
                }

                bool bridgeChanged = SelectedBridge != null;
                SelectedBridge = null;

                RefreshUIState();
                
                if (bridgeChanged)
                {
                    SelectedBridgeChanged?.Invoke();
                }
            }
        }

        private void OnMapClicked(int x, int y, int floor)
        {
            if (_firstClickedTile != null && _secondClickedTile != null)
            {
                _firstClickedTile = new TileCoords(x, y, floor);
                _secondClickedTile = null;
            }
            else if (_firstClickedTile != null)
            {
                _secondClickedTile = new TileCoords(x, y, floor);
            }
            else
            {
                _firstClickedTile = new TileCoords(x, y, floor);
            }
            
            RefreshUIState();
        }

        private void OnMapDeselect()
        {
            _firstClickedTile = null;
            _secondClickedTile = null;
            
            RefreshUIState();
        }

        private void RefreshUIState()
        {
            if (_firstClickedTile != null && _secondClickedTile != null)
            {
                _bridgeTabSwapper.SwapToTab(BridgeTab.TwoTilesSelected);
            }
            else if (_firstClickedTile != null)
            {
                _bridgeTabSwapper.SwapToTab(BridgeTab.OneTileSelected);
            }
            else if (SelectedBridge != null)
            {
                _bridgeTabSwapper.SwapToTab(BridgeTab.BridgeSelected);
            }
            else
            {
                _bridgeTabSwapper.SwapToTab(BridgeTab.NothingSelected);
            }
        }
        
        private void OnDisable()
        {
            if (_lastFrameHoveredBridge != null)
            {
                _lastFrameHoveredBridge.DisableHighlighting();
            }
            _lastFrameHoveredBridge = null;

            if (SelectedBridge != null)
            {
                SelectedBridge.DisableHighlighting();
            }
            SelectedBridge = null;
            _firstClickedTile = null;
            _secondClickedTile = null;
            RefreshUIState();
            SelectedBridgeChanged?.Invoke();
        }
    }
}
