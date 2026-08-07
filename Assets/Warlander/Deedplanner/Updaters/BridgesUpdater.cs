using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Graphics.Projectors;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Gui.Widgets.Bridges;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Graphics.Outline;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;

namespace Warlander.Deedplanner.Updaters
{
    public class BridgesUpdater : IUpdater
    {
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly DPInput _input;
        private readonly TooltipHandler _tooltipHandler;
        private readonly BridgeTabSwapper _bridgeTabSwapper;
        private readonly TabContext _tabContext;
        private readonly IMapProjectorFacade _mapProjectorFacade;
        private readonly MapHandler _mapHandler;

        public Tab TargetTab => Tab.Bridges;

        public BridgesUpdater(CameraCoordinator cameraCoordinator, DPInput input, TooltipHandler tooltipHandler,
            BridgeTabSwapper bridgeTabSwapper, TabContext tabContext, IMapProjectorFacade mapProjectorFacade, MapHandler mapHandler)
        {
            _cameraCoordinator = cameraCoordinator;
            _input = input;
            _tooltipHandler = tooltipHandler;
            _bridgeTabSwapper = bridgeTabSwapper;
            _tabContext = tabContext;
            _mapProjectorFacade = mapProjectorFacade;
            _mapHandler = mapHandler;
        }

        public event Action SelectedBridgeChanged;
        public event Action<TileCoords, TileCoords> TileSelectionChanged;

        public Bridge SelectedBridge { get; private set; }
        public TileCoords FirstClickedTile => _firstClickedTile;
        public TileCoords SecondClickedTile => _secondClickedTile;

        private Bridge _lastFrameHoveredBridge;
        private TileCoords _firstClickedTile;
        private TileCoords _secondClickedTile;

        private IMapProjector _firstTileProjector;
        private IMapProjector _secondTileProjector;
        private readonly List<IMapProjector> _spanProjectors = new List<IMapProjector>();
        private Map _subscribedMap;

        public void Initialize()
        {
            _mapHandler.MapInitialized += OnMapInitialized;
            SubscribeToMap(_mapHandler.Map);
        }

        private void OnMapInitialized()
        {
            SubscribeToMap(_mapHandler.Map);
        }

        private void SubscribeToMap(Map map)
        {
            if (_subscribedMap == map)
            {
                return;
            }

            if (_subscribedMap != null)
            {
                _subscribedMap.BridgesChanged -= OnBridgesChanged;
            }

            _subscribedMap = map;

            if (_subscribedMap != null)
            {
                _subscribedMap.BridgesChanged += OnBridgesChanged;
            }
        }

        private void OnBridgesChanged()
        {
            IReadOnlyList<Bridge> bridges = _mapHandler.Map.Bridges;

            if (SelectedBridge != null && !bridges.Contains(SelectedBridge))
            {
                ClearBridgeSelection();
            }

            if (_lastFrameHoveredBridge != null && !bridges.Contains(_lastFrameHoveredBridge))
            {
                _lastFrameHoveredBridge.DisableHighlighting();
                _lastFrameHoveredBridge = null;
            }
        }

        public void Enable()
        {
            _tabContext.TileSelectionMode = TileSelectionMode.Nothing;
        }

        public void Tick()
        {
            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }
            
            BridgePart bridgePart = raycast.transform.GetComponentInParent<BridgePart>();
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
            SelectedBridge.Rebuilt += OnSelectedBridgeRebuilt;

            _firstClickedTile = null;
            _secondClickedTile = null;

            ClearProjectors();
            RefreshUIState();

            if (bridgeChanged)
            {
                SelectedBridgeChanged?.Invoke();
            }
        }

        private void OnSelectedBridgeRebuilt()
        {
            if (SelectedBridge != null)
            {
                SelectedBridge.EnableHighlighting(OutlineType.Positive);
            }

            SelectedBridgeChanged?.Invoke();
        }

        private void OnBridgeDeselected()
        {
            if (SelectedBridge != null)
            {
                SelectedBridge.Rebuilt -= OnSelectedBridgeRebuilt;

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

            TileSelectionChanged?.Invoke(_firstClickedTile, _secondClickedTile);
            RefreshProjectors();
            RefreshUIState();
        }

        public void ClearTileSelection()
        {
            _firstClickedTile = null;
            _secondClickedTile = null;
            TileSelectionChanged?.Invoke(null, null);
            ClearProjectors();
            RefreshUIState();
        }

        public void SelectBridge(Bridge bridge)
        {
            OnBridgeClicked(bridge);
        }

        public void ClearBridgeSelection()
        {
            _lastFrameHoveredBridge = null;
            OnBridgeDeselected();
        }

        private void OnMapDeselect()
        {
            ClearTileSelection();
        }

        private void RefreshProjectors()
        {
            if (_firstClickedTile != null)
            {
                if (_firstTileProjector == null)
                {
                    _firstTileProjector = _mapProjectorFacade.RequestProjector(ProjectorColor.Red);
                }

                _firstTileProjector.ProjectTile(new Vector2Int(_firstClickedTile.X, _firstClickedTile.Y));
            }
            else
            {
                ReleaseProjector(ref _firstTileProjector);
            }

            if (_secondClickedTile != null)
            {
                if (_secondTileProjector == null)
                {
                    _secondTileProjector = _mapProjectorFacade.RequestProjector(ProjectorColor.Red);
                }

                _secondTileProjector.ProjectTile(new Vector2Int(_secondClickedTile.X, _secondClickedTile.Y));
            }
            else
            {
                ReleaseProjector(ref _secondTileProjector);
            }

            foreach (IMapProjector spanProjector in _spanProjectors)
            {
                _mapProjectorFacade.FreeProjector(spanProjector);
            }

            _spanProjectors.Clear();

            if (_firstClickedTile == null || _secondClickedTile == null)
            {
                return;
            }

            int minX = Mathf.Min(_firstClickedTile.X, _secondClickedTile.X);
            int maxX = Mathf.Max(_firstClickedTile.X, _secondClickedTile.X);
            int minY = Mathf.Min(_firstClickedTile.Y, _secondClickedTile.Y);
            int maxY = Mathf.Max(_firstClickedTile.Y, _secondClickedTile.Y);

            bool vertical;
            if (_firstClickedTile.X == _secondClickedTile.X)
            {
                vertical = true;
            }
            else if (_firstClickedTile.Y == _secondClickedTile.Y)
            {
                vertical = false;
            }
            else
            {
                vertical = Mathf.Abs(maxY - minY) > Mathf.Abs(maxX - minX);
            }

            if (vertical)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int startY = minY;
                    int endY = maxY;

                    if (_firstClickedTile.X == x && _firstClickedTile.Y == minY)
                    {
                        startY++;
                    }
                    else if (_firstClickedTile.X == x && _firstClickedTile.Y == maxY)
                    {
                        endY--;
                    }

                    if (_secondClickedTile.X == x && _secondClickedTile.Y == minY)
                    {
                        startY++;
                    }
                    else if (_secondClickedTile.X == x && _secondClickedTile.Y == maxY)
                    {
                        endY--;
                    }

                    if (startY > endY)
                    {
                        continue;
                    }

                    IMapProjector projector = _mapProjectorFacade.RequestProjector(ProjectorColor.Yellow);
                    projector.ProjectArea(new Vector2Int(x, startY), new Vector2Int(x, endY));
                    _spanProjectors.Add(projector);
                }
            }
            else
            {
                for (int y = minY; y <= maxY; y++)
                {
                    int startX = minX;
                    int endX = maxX;

                    if (_firstClickedTile.Y == y && _firstClickedTile.X == minX)
                    {
                        startX++;
                    }
                    else if (_firstClickedTile.Y == y && _firstClickedTile.X == maxX)
                    {
                        endX--;
                    }

                    if (_secondClickedTile.Y == y && _secondClickedTile.X == minX)
                    {
                        startX++;
                    }
                    else if (_secondClickedTile.Y == y && _secondClickedTile.X == maxX)
                    {
                        endX--;
                    }

                    if (startX > endX)
                    {
                        continue;
                    }

                    IMapProjector projector = _mapProjectorFacade.RequestProjector(ProjectorColor.Yellow);
                    projector.ProjectArea(new Vector2Int(startX, y), new Vector2Int(endX, y));
                    _spanProjectors.Add(projector);
                }
            }
        }

        private void ClearProjectors()
        {
            ReleaseProjector(ref _firstTileProjector);
            ReleaseProjector(ref _secondTileProjector);

            foreach (IMapProjector spanProjector in _spanProjectors)
            {
                _mapProjectorFacade.FreeProjector(spanProjector);
            }

            _spanProjectors.Clear();
        }

        private void ReleaseProjector(ref IMapProjector projector)
        {
            if (projector == null)
            {
                return;
            }

            _mapProjectorFacade.FreeProjector(projector);
            projector = null;
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
        
        public void Disable()
        {
            if (_lastFrameHoveredBridge != null)
            {
                _lastFrameHoveredBridge.DisableHighlighting();
            }
            _lastFrameHoveredBridge = null;

            if (SelectedBridge != null)
            {
                SelectedBridge.Rebuilt -= OnSelectedBridgeRebuilt;
                SelectedBridge.DisableHighlighting();
            }
            SelectedBridge = null;
            ClearTileSelection();
            SelectedBridgeChanged?.Invoke();
        }
    }
}
