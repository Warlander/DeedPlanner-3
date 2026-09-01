using System;
using Warlander.Deedplanner.Persistence;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Bridges;
using Warlander.Deedplanner.Rendering.Projectors;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Bridges.Widgets;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Rendering.Outline;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Cameras;

namespace Warlander.Deedplanner.Editing
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
        private int _hoveredSegment = -1;
        private TileCoords _firstClickedTile;
        private TileCoords _secondClickedTile;

        private bool _pavingBrushActive;
        private BridgePavementData _pavingBrush;
        private BridgePart _hoveredPaintPart;
        private List<BridgePart> _strokeParts;
        private List<BridgePavementData> _strokeOldPavements;

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
                if (_pavingBrushActive)
                {
                    UpdatePaintHover(null);
                    EndStrokeIfNeeded();
                }
                return;
            }

            BridgePart bridgePart = raycast.transform.GetComponentInParent<BridgePart>();
            Bridge bridge = bridgePart != null ? bridgePart.ParentBridge : null;

            if (_pavingBrushActive)
            {
                TickPaving(bridgePart);
            }
            else
            {
                UpdateBridgeHover(bridge);
            }

            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                // Paving brush intercepts clicks on paveable parts; anything else behaves as usual.
                bool painted = _pavingBrushActive && BeginStroke(bridgePart);
                if (!painted)
                {
                    OnBridgeClicked(bridge);
                    if (bridge == null)
                    {
                        int x = Mathf.FloorToInt(raycast.point.x / 4f);
                        int y = Mathf.FloorToInt(raycast.point.z / 4f);
                        int floor = ResolveLevel(raycast.point, x, y);

                        OnMapClicked(x, y, floor);
                    }
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
            else
            {
                ShowSelectedEndpointTooltip(raycast);
            }
        }

        private int ResolveLevel(Vector3 point, int x, int y)
        {
            int cameraLevel = _cameraCoordinator.Current.Level;
            if (cameraLevel < 0)
            {
                return cameraLevel;
            }

            Map map = _mapHandler.Map;
            if (map[x, y] == null)
            {
                return cameraLevel;
            }

            // Inverse of Bridge.GetAbsoluteHeight: 3 world units per building level, 0.3 world units floor offset.
            float surfaceHeight = map.GetInterpolatedHeight(point.x, point.z);
            int derivedLevel = Mathf.RoundToInt((point.y - surfaceHeight - 0.3f) / 3f);

            return derivedLevel > 0 ? derivedLevel : cameraLevel;
        }

        private void ShowSelectedEndpointTooltip(RaycastHit raycast)
        {
            int x = Mathf.FloorToInt(raycast.point.x / 4f);
            int y = Mathf.FloorToInt(raycast.point.z / 4f);

            TileCoords endpoint = null;
            string label = null;
            if (_firstClickedTile != null && _firstClickedTile.X == x && _firstClickedTile.Y == y)
            {
                endpoint = _firstClickedTile;
                label = "Bridge start";
            }
            else if (_secondClickedTile != null && _secondClickedTile.X == x && _secondClickedTile.Y == y)
            {
                endpoint = _secondClickedTile;
                label = "Bridge end";
            }

            if (endpoint == null)
            {
                return;
            }

            int difference = endpoint.Level - _cameraCoordinator.Current.Level;
            string floorLine;
            if (difference == 0)
            {
                floorLine = "This floor";
            }
            else
            {
                int count = Mathf.Abs(difference);
                string direction = difference > 0 ? "above" : "below";
                floorLine = $"{count} floor{(count == 1 ? "" : "s")} {direction} active floor";
            }

            _tooltipHandler.ShowTooltipText($"{label}\n{floorLine}");
        }

        // Null pavement with active brush = eraser.
        public void SetPavingBrush(bool active, BridgePavementData pavement)
        {
            _pavingBrushActive = active;
            _pavingBrush = pavement;
            if (!active)
            {
                UpdatePaintHover(null);
                _strokeParts = null;
                _strokeOldPavements = null;
            }
        }

        private void TickPaving(BridgePart bridgePart)
        {
            // Whole-bridge hover is suspended while painting - only the targeted part highlights.
            UpdateBridgeHover(null);

            BridgePart target = bridgePart != null && bridgePart.ParentBridge.Data.CanBePaved
                ? bridgePart : null;
            UpdatePaintHover(target);

            if (_strokeParts != null)
            {
                if (_input.UpdatersShared.Placement.IsPressed())
                {
                    AddToStroke(target);
                }
                else
                {
                    FinishStroke();
                }
            }
        }

        private bool BeginStroke(BridgePart bridgePart)
        {
            BridgePart target = bridgePart != null && bridgePart.ParentBridge.Data.CanBePaved
                ? bridgePart : null;
            if (target == null || target.Pavement == _pavingBrush)
            {
                return false;
            }

            _strokeParts = new List<BridgePart>();
            _strokeOldPavements = new List<BridgePavementData>();
            AddToStroke(target);
            return true;
        }

        private void AddToStroke(BridgePart part)
        {
            // Skipping same-pavement parts keeps the refresh per newly painted part only.
            if (part == null || _strokeParts.Contains(part) || part.Pavement == _pavingBrush)
            {
                return;
            }

            _strokeParts.Add(part);
            _strokeOldPavements.Add(part.Pavement);
            part.SetPavement(_pavingBrush);
        }

        private void EndStrokeIfNeeded()
        {
            if (_strokeParts != null && !_input.UpdatersShared.Placement.IsPressed())
            {
                FinishStroke();
            }
        }

        private void FinishStroke()
        {
            if (_strokeParts.Count > 0)
            {
                BridgePavementData[] newPavements = new BridgePavementData[_strokeParts.Count];
                for (int i = 0; i < newPavements.Length; i++)
                {
                    newPavements[i] = _pavingBrush;
                }

                // Pavements were already applied live during the stroke - record for undo only.
                Map map = _mapHandler.Map;
                map.CommandManager.AddToStack(new BridgePavingChangeCommand(
                    _strokeParts.ToArray(), _strokeOldPavements.ToArray(), newPavements));
            }

            _strokeParts = null;
            _strokeOldPavements = null;
        }

        private void UpdatePaintHover(BridgePart part)
        {
            if (_hoveredPaintPart == part)
            {
                return;
            }

            ClearPaintHover();
            _hoveredPaintPart = part;

            if (part != null)
            {
                part.ParentBridge.HighlightPart(part, OutlineType.Neutral);
            }
        }

        private void ClearPaintHover()
        {
            if (_hoveredPaintPart == null)
            {
                return;
            }

            // Selected bridge keeps its Positive highlight on all parts; restore it instead of clearing.
            if (_hoveredPaintPart.ParentBridge == SelectedBridge)
            {
                _hoveredPaintPart.ParentBridge.HighlightPart(_hoveredPaintPart, OutlineType.Positive);
            }
            else
            {
                _hoveredPaintPart.ParentBridge.UnhighlightPart(_hoveredPaintPart);
            }

            _hoveredPaintPart = null;
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

        // -1 clears the segment highlight. Hovered segment goes Neutral on top of the
        // selection's Positive, same priority so deselection still clears everything.
        public void SetHoveredSegment(int segmentIndex)
        {
            if (SelectedBridge == null || segmentIndex == _hoveredSegment)
            {
                return;
            }

            _hoveredSegment = segmentIndex;
            ApplyHighlighting();
        }

        private void ApplyHighlighting()
        {
            if (SelectedBridge == null)
            {
                return;
            }

            SelectedBridge.EnableHighlighting(OutlineType.Positive);
            if (_hoveredSegment >= 0)
            {
                SelectedBridge.HighlightSegment(_hoveredSegment, OutlineType.Neutral);
            }
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
            _hoveredSegment = -1;
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
                ApplyHighlighting();
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
                _hoveredSegment = -1;

                RefreshUIState();
                
                if (bridgeChanged)
                {
                    SelectedBridgeChanged?.Invoke();
                }
            }
        }

        private void OnMapClicked(int x, int y, int floor)
        {
            if (_firstClickedTile != null && _firstClickedTile.X == x && _firstClickedTile.Y == y)
            {
                _firstClickedTile = _secondClickedTile;
                _secondClickedTile = null;
            }
            else if (_secondClickedTile != null && _secondClickedTile.X == x && _secondClickedTile.Y == y)
            {
                _secondClickedTile = null;
            }
            else if (_firstClickedTile != null && _secondClickedTile != null)
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
            UpdatePaintHover(null);
            if (_strokeParts != null)
            {
                FinishStroke();
            }

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
