using System;
using Plugins.Warlander.Utils;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Tooltips;
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

        public event Action SelectedBridgeChanged;
        
        public Bridge SelectedBridge { get; private set; }
        
        private Bridge _lastFrameHoveredBridge;

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

            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0)
            {
                OnBridgeClicked(bridge);
            }

            if (_input.UpdatersShared.Deletion.ReadValue<float>() > 0)
            {
                OnBridgeDeselected();
            }

            if (bridge != null)
            {
                _tooltipHandler.ShowTooltipText($"{bridge.Data.Name} bridge");
            }

            if (bridgePart != null)
            {
                string bridgePartRawString = bridgePart.PartType.ToString();
                string bridgePartWithSpaces = StringUtils.AddSpacesToSentence(bridgePartRawString);
                string bridgePartLowercase = bridgePartWithSpaces.ToLower();
                _tooltipHandler.ShowTooltipText(bridgePartLowercase);
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

            SelectedBridge = bridge;
            SelectedBridge.EnableHighlighting(OutlineType.Positive);
            SelectedBridgeChanged?.Invoke();
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

                SelectedBridge = null;
                SelectedBridgeChanged?.Invoke();
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
            SelectedBridgeChanged?.Invoke();
        }
    }
}
