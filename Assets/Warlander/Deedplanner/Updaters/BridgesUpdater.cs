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

        private Bridge _lastFrameBridge;
        
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
            
            Map map = _gameManager.Map;
            int tileX = Mathf.FloorToInt(raycast.point.x / 4f);
            int tileZ = Mathf.FloorToInt(raycast.point.z / 4f);
            Tile tile = map[tileX, tileZ];
            BridgePart bridgePart = tile.BridgePart;
            Bridge bridge = bridgePart != null ? bridgePart.ParentBridge : null;

            if (_lastFrameBridge != bridge)
            {
                if (_lastFrameBridge != null)
                {
                    _lastFrameBridge.DisableHighlighting();
                }

                _lastFrameBridge = bridge;
                if (bridge != null)
                {
                    bridge.EnableHighlighting(OutlineType.Neutral);
                }
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
    }
}
