using Warlander.Deedplanner.Persistence;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Rendering.Projectors;
using Warlander.Deedplanner.Ui.Tooltips;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Caves;

namespace Warlander.Deedplanner.Editing
{
    public class HeightUpdater : IUpdater
    {
        private static readonly Color NeutralColor = Color.white;
        private static readonly Color HoveredColor = new Color(0.7f, 0.7f, 0, 1);
        private static readonly Color SelectedColor = new Color(0, 1, 0, 1);
        private static readonly Color SelectedHoveredColor = new Color(0.7f, 0.39f, 0f);
        private static readonly Color ActiveColor = new Color(1, 0, 0, 1);
        private static readonly Color AnchorColor = new Color(0, 1, 1, 1);

        private readonly IHeightUpdaterView _view;
        private readonly TooltipHandler _tooltipHandler;
        private readonly EditingSettings _settings;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly DPInput _input;
        private readonly MapHandler _mapHandler;
        private readonly IMapProjectorFacade _mapProjectorFacade;
        private readonly TabContext _tabContext;

        public Tab TargetTab => Tab.Height;

        private List<HeightmapHandle> currentFrameHoveredHandles = new List<HeightmapHandle>();
        private List<HeightmapHandle> lastFrameHoveredHandles = new List<HeightmapHandle>();
        private List<HeightmapHandle> selectedHandles = new List<HeightmapHandle>();
        private List<HeightmapHandle> deselectedHandles = new List<HeightmapHandle>();
        private HeightmapHandle activeHandle;
        private HeightmapHandle anchorHandle;
        private IMapProjector _anchorProjector;
        private PlaneAlignment anchorAlignment;
        private readonly Dictionary<HeightmapHandle, int> _originalCaveValues =
            new Dictionary<HeightmapHandle, int>();
        private ICaveHeightEdit _caveHeightEdit;
        private CaveHeightMode _caveHeightMode = CaveHeightMode.Floor;
        private bool? _lastCaveRealm;
        private Map _observedMap;

        private HeightMode mode = HeightMode.SelectAndDrag;
        private HeightUpdaterState state = HeightUpdaterState.Idle;
        private Vector2 dragStartPos;
        private Vector2 dragEndPos;

        private string _dragSensitivity;
        private bool _respectOriginalSlopes;
        private string _surfaceTargetHeight = "0";
        private string _caveFloorTargetHeight = "-40";
        private string _caveClearanceTargetHeight = "30";

        private SlopeGridView _slopeGrid;
        private readonly int[] _heightsBuffer = new int[9];

        private bool ComplexSelectionEnabled => mode != HeightMode.PaintTerrain;

        public HeightUpdater(IHeightUpdaterView view, TooltipHandler tooltipHandler, EditingSettings settings,
            CameraCoordinator cameraCoordinator, DPInput input, MapHandler mapHandler,
            IMapProjectorFacade mapProjectorFacade, TabContext tabContext)
        {
            _view = view;
            _tooltipHandler = tooltipHandler;
            _settings = settings;
            _cameraCoordinator = cameraCoordinator;
            _input = input;
            _mapHandler = mapHandler;
            _mapProjectorFacade = mapProjectorFacade;
            _tabContext = tabContext;
        }

        public void Initialize()
        {
            _view.ModeChanged += OnModeChanged;
            _view.DragSensitivityChanged += OnDragSensitivityChanged;
            _view.RespectOriginalSlopesChanged += OnRespectOriginalSlopesChanged;
            _view.TargetHeightChanged += OnTargetHeightChanged;
            _view.CaveHeightModeChanged += OnCaveHeightModeChanged;

            _dragSensitivity = _settings.HeightDragSensitivity.ToString(CultureInfo.InvariantCulture);
            _respectOriginalSlopes = _settings.HeightRespectOriginalSlopes;

            _view.SetDragSensitivity(_dragSensitivity);
            _view.SetRespectOriginalSlopes(_respectOriginalSlopes);
            _view.SetTargetHeight(_surfaceTargetHeight);
        }

        public void Enable()
        {
            ObserveMap(_mapHandler.Map);
            RefreshTileSelectionMode();
        }

        private void OnModeChanged(HeightMode newMode)
        {
            mode = newMode;
            _view.ShowModePanels(mode);
            RefreshTileSelectionMode();
            ResetState();
        }

        private void OnDragSensitivityChanged(string value)
        {
            _dragSensitivity = value;
            float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float dragSensitivity);

            _settings.HeightDragSensitivity = dragSensitivity;
        }

        private void OnRespectOriginalSlopesChanged(bool value)
        {
            _respectOriginalSlopes = value;
            _settings.HeightRespectOriginalSlopes = value;
        }

        private void OnTargetHeightChanged(string value)
        {
            if (!IsCaveRealm)
            {
                _surfaceTargetHeight = value;
            }
            else if (_caveHeightMode == CaveHeightMode.Floor)
            {
                _caveFloorTargetHeight = value;
            }
            else
            {
                _caveClearanceTargetHeight = value;
            }
        }

        private void OnCaveHeightModeChanged(CaveHeightMode caveHeightMode)
        {
            _caveHeightMode = caveHeightMode;
            ResetState();
            RefreshCaveGrid();
            _view.SetTargetHeight(CurrentTargetHeight);
        }

        private void RefreshTileSelectionMode()
        {
            if (ComplexSelectionEnabled)
            {
                _tabContext.TileSelectionMode = TileSelectionMode.Tiles;
            }
            else
            {
                _tabContext.TileSelectionMode = TileSelectionMode.Everything;
            }
        }

        private void ResetState()
        {
            deselectedHandles.AddRange(currentFrameHoveredHandles);
            currentFrameHoveredHandles.Clear();
            lastFrameHoveredHandles.Clear();
            deselectedHandles.AddRange(selectedHandles);
            selectedHandles.Clear();
            activeHandle = null;
            anchorHandle = null;
            if (_anchorProjector != null)
            {
                _mapProjectorFacade.FreeProjector(_anchorProjector);
                _anchorProjector = null;
            }
            state = HeightUpdaterState.Idle;
            CancelEdit();
            UpdateHandlesColors();
            _cameraCoordinator.Current.RenderSelectionBox = false;
        }

        public void Tick()
        {
            ObserveMap(_mapHandler.Map);
            MultiCamera camera = _cameraCoordinator.Current;
            bool caveRealm = camera.Level < 0;
            if (_lastCaveRealm != caveRealm)
            {
                ResetState();
                _lastCaveRealm = caveRealm;
                _view.ShowCaveHeightModes(caveRealm);
                _view.SetTargetHeight(CurrentTargetHeight);
                if (!caveRealm)
                {
                    RestoreCaveGridFloor();
                }
                else
                {
                    RefreshCaveGrid();
                }
            }

            RaycastHit raycast = camera.CurrentRaycast;
            bool cameraOnScreen = camera.MouseOver;

            if (!cameraOnScreen)
            {
                if (_input.UpdatersShared.Placement.WasReleasedThisFrame())
                {
                    if (state == HeightUpdaterState.Manipulating)
                    {
                        FinishEdit();
                    }
                    state = HeightUpdaterState.Idle;
                    activeHandle = null;
                    camera.RenderSelectionBox = false;
                }
                if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
                {
                    CancelEdit();
                    state = HeightUpdaterState.Idle;
                    activeHandle = null;
                    camera.RenderSelectionBox = false;
                }
                return;
            }

            currentFrameHoveredHandles = UpdateHoveredHandles(raycast);

            switch (mode)
            {
                case HeightMode.SelectAndDrag:
                    UpdateSelectAndDrag();
                    break;
                case HeightMode.CreateRamps:
                    UpdateCreateRamps();
                    break;
                case HeightMode.LevelArea:
                    UpdateLevelArea();
                    break;
                case HeightMode.PaintTerrain:
                    UpdatePaintTerrain();
                    break;
            }

            if (caveRealm)
            {
                _mapHandler.Map.CaveGridMesh.ApplyAllChanges();
            }

            UpdateHandlesColors();
            deselectedHandles = new List<HeightmapHandle>();
            lastFrameHoveredHandles = currentFrameHoveredHandles;

            if (activeHandle != null)
            {
                if (_slopeGrid == null)
                {
                    _slopeGrid = _tooltipHandler.GetContent<SlopeGridView>();
                }
                string quantity = caveRealm ? _caveHeightMode + " — " : string.Empty;
                _tooltipHandler.ShowTooltipText(quantity + "X: " + activeHandle.TileCoords.x
                    + " Y: " + activeHandle.TileCoords.y);
                GridMesh activeGrid = caveRealm
                    ? _mapHandler.Map.CaveGridMesh
                    : _mapHandler.Map.SurfaceGridMesh;
                activeGrid.WriteSlopeGridData(activeHandle.TileCoords, _heightsBuffer);
                _slopeGrid.SetData(new SlopeGridData(3, _heightsBuffer));
                _tooltipHandler.ShowTooltipContent(_slopeGrid);
            }
        }

        private void UpdateSelectAndDrag()
        {
            Map map = _mapHandler.Map;

            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                if (currentFrameHoveredHandles.Count == 1 && selectedHandles.Contains(currentFrameHoveredHandles[0]))
                {
                    activeHandle = currentFrameHoveredHandles[0];
                    state = HeightUpdaterState.Manipulating;
                    BeginCaveEdit(map);
                }
                else if (_input.HeightUpdater.DragSelection.IsPressed())
                {
                    state = HeightUpdaterState.Dragging;
                }
                else
                {
                    deselectedHandles = selectedHandles;
                    selectedHandles = new List<HeightmapHandle>();
                    state = HeightUpdaterState.Dragging;
                }
            }

            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0)
            {
                if (state == HeightUpdaterState.Manipulating)
                {
                    int heightDelta = (int) ((dragEndPos.y - dragStartPos.y) * _settings.HeightDragSensitivity);
                    if (IsCaveRealm)
                    {
                        int originalHeight = _originalCaveValues[activeHandle];
                        foreach (HeightmapHandle heightmapHandle in selectedHandles)
                        {
                            int target = _respectOriginalSlopes
                                ? _originalCaveValues[heightmapHandle] + heightDelta
                                : originalHeight + heightDelta;
                            SetCaveValue(heightmapHandle, target);
                        }
                    }
                    else
                    {
                        map.CommandManager.UndoAction();
                        int originalHeight = map[activeHandle.TileCoords].SurfaceHeight;
                        foreach (HeightmapHandle heightmapHandle in selectedHandles)
                        {
                            Vector2Int tileCoords = heightmapHandle.TileCoords;
                            if (_respectOriginalSlopes)
                            {
                                map[tileCoords].SurfaceHeight += heightDelta;
                            }
                            else
                            {
                                map[tileCoords].SurfaceHeight = originalHeight + heightDelta;
                            }
                        }
                    }
                }
            }

            if (_input.UpdatersShared.Placement.WasReleasedThisFrame())
            {
                if (state == HeightUpdaterState.Dragging && _input.HeightUpdater.DragSelection.IsPressed())
                {
                    selectedHandles.AddRange(lastFrameHoveredHandles);
                }
                else if (state != HeightUpdaterState.Manipulating && state != HeightUpdaterState.Recovering)
                {
                    deselectedHandles = selectedHandles;
                    selectedHandles = lastFrameHoveredHandles;
                }
                else if (state == HeightUpdaterState.Manipulating)
                {
                    FinishEdit();
                    activeHandle = null;
                }
                state = HeightUpdaterState.Idle;
            }

            if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
            {
                if (state == HeightUpdaterState.Idle)
                {
                    deselectedHandles = selectedHandles;
                    selectedHandles = new List<HeightmapHandle>();
                }
                else if (state == HeightUpdaterState.Manipulating)
                {
                    CancelEdit();
                    activeHandle = null;
                    state = HeightUpdaterState.Recovering;
                }
                else
                {
                    state = HeightUpdaterState.Recovering;
                }

                _cameraCoordinator.Current.RenderSelectionBox = false;
            }
        }

        private void UpdateCreateRamps()
        {
            Map map = _mapHandler.Map;
            float dragSensitivity = 0;
            float.TryParse(_dragSensitivity, NumberStyles.Any, CultureInfo.InvariantCulture, out dragSensitivity);
            bool respectSlopes = _respectOriginalSlopes;

            if (state == HeightUpdaterState.Recovering)
            {
                state = HeightUpdaterState.Idle;
            }

            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                if (currentFrameHoveredHandles.Count == 1 && selectedHandles.Contains(currentFrameHoveredHandles[0]))
                {
                    if (anchorHandle != null && anchorHandle != currentFrameHoveredHandles[0])
                    {
                        activeHandle = currentFrameHoveredHandles[0];
                        BeginCaveEdit(map);
                    }
                    else
                    {
                        anchorHandle = currentFrameHoveredHandles[0];
                    }
                    state = HeightUpdaterState.Manipulating;
                }
                else if (_input.HeightUpdater.DragSelection.IsPressed())
                {
                    state = HeightUpdaterState.Dragging;
                }
                else
                {
                    deselectedHandles = selectedHandles;
                    selectedHandles = new List<HeightmapHandle>();
                    anchorHandle = null;
                    if (_anchorProjector != null)
                    {
                        _mapProjectorFacade.FreeProjector(_anchorProjector);
                        _anchorProjector = null;
                    }
                    state = HeightUpdaterState.Dragging;
                }
            }

            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0)
            {
                if (state == HeightUpdaterState.Manipulating)
                {
                    if (activeHandle != null && anchorHandle != null)
                    {
                        bool locked = _anchorProjector != null;
                        int originalHeight;
                        int activeOriginalHeight;
                        if (IsCaveRealm)
                        {
                            originalHeight = _originalCaveValues[anchorHandle];
                            activeOriginalHeight = _originalCaveValues[activeHandle];
                        }
                        else
                        {
                            map.CommandManager.UndoAction();
                            originalHeight = map[anchorHandle.TileCoords].SurfaceHeight;
                            activeOriginalHeight = map[activeHandle.TileCoords].SurfaceHeight;
                        }
                        int heightDelta = (int) ((dragEndPos.y - dragStartPos.y) * dragSensitivity);

                        // instantly make smooth ramp from anchor handle to active handle if original slopes are not respected
                        // turned off if original slopes are respected, because instantly making ramp is impractical in such case
                        if (!respectSlopes)
                        {
                            heightDelta += activeOriginalHeight - originalHeight;
                        }

                        Vector2Int manipulatedTileCoords = activeHandle.TileCoords;
                        Vector2Int manipulatedAnchorCoords = GetAxisCorrectedAnchor(manipulatedTileCoords, anchorHandle.TileCoords, locked, anchorAlignment);
                        Vector2Int manipulatedDifference = manipulatedTileCoords - manipulatedAnchorCoords;

                        foreach (HeightmapHandle heightmapHandle in selectedHandles)
                        {
                            Vector2Int tileCoords = heightmapHandle.TileCoords;
                            Vector2Int anchorCoords = GetAxisCorrectedAnchor(tileCoords, anchorHandle.TileCoords, locked, anchorAlignment);
                            Vector2Int difference = tileCoords - anchorCoords;
                            float deltaX = (float) difference.x / manipulatedDifference.x;
                            if (float.IsNaN(deltaX) || float.IsInfinity(deltaX))
                            {
                                deltaX = float.NegativeInfinity;
                            }
                            float deltaY = (float) difference.y / manipulatedDifference.y;
                            if (float.IsNaN(deltaY) || float.IsInfinity(deltaY))
                            {
                                deltaY = float.NegativeInfinity;
                            }

                            float delta = Mathf.Max(deltaX, deltaY);
                            if (float.IsNegativeInfinity(delta))
                            {
                                delta = 0;
                            }

                            if (respectSlopes)
                            {
                                int target = IsCaveRealm
                                    ? _originalCaveValues[heightmapHandle] + (int) (heightDelta * delta)
                                    : map[tileCoords].SurfaceHeight + (int) (heightDelta * delta);
                                SetHeightValue(map, heightmapHandle, target);
                            }
                            else
                            {
                                SetHeightValue(map, heightmapHandle,
                                    originalHeight + (int) (heightDelta * delta));
                            }
                        }
                    }
                    else if (anchorHandle != null)
                    {
                        float anchorPositionX = anchorHandle.TileCoords.x * 4;
                        float anchorPositionY = anchorHandle.TileCoords.y * 4;
                        Vector2 anchorPosition = new Vector2(anchorPositionX, anchorPositionY);

                        Vector3 raycastPoint = _cameraCoordinator.Current.CurrentRaycast.point;
                        Vector2 raycastPosition = new Vector2(raycastPoint.x, raycastPoint.z);

                        Vector2 positionDelta = raycastPosition - anchorPosition;
                        if (positionDelta.magnitude > 4)
                        {
                            if (_anchorProjector == null)
                                _anchorProjector = _mapProjectorFacade.RequestProjector(ProjectorColor.Red);
                            bool horizontal = Mathf.Abs(positionDelta.x) > Mathf.Abs(positionDelta.y);
                            anchorAlignment = horizontal ? PlaneAlignment.Vertical : PlaneAlignment.Horizontal;
                            _anchorProjector.ProjectLine(anchorHandle.TileCoords, anchorAlignment);
                        }
                        else if (_anchorProjector != null)
                        {
                            _mapProjectorFacade.FreeProjector(_anchorProjector);
                            _anchorProjector = null;
                        }
                    }
                }
            }

            if (_input.UpdatersShared.Placement.WasReleasedThisFrame())
            {
                if (state == HeightUpdaterState.Dragging && _input.HeightUpdater.DragSelection.IsPressed())
                {
                    selectedHandles.AddRange(lastFrameHoveredHandles);
                }
                else if (state == HeightUpdaterState.Dragging)
                {
                    deselectedHandles = selectedHandles;
                    selectedHandles = lastFrameHoveredHandles;
                }
                else if (state == HeightUpdaterState.Manipulating)
                {
                    FinishEdit();
                    activeHandle = null;
                }
                state = HeightUpdaterState.Idle;
            }

            if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
            {
                if (state == HeightUpdaterState.Manipulating && activeHandle != null)
                {
                    CancelEdit();
                    activeHandle = null;
                    state = HeightUpdaterState.Recovering;
                }
                else if (anchorHandle != null)
                {
                    anchorHandle = null;
                    if (_anchorProjector != null)
                    {
                        _mapProjectorFacade.FreeProjector(_anchorProjector);
                        _anchorProjector = null;
                    }
                    state = HeightUpdaterState.Recovering;
                }
                else if (state == HeightUpdaterState.Idle)
                {
                    deselectedHandles = selectedHandles;
                    selectedHandles = new List<HeightmapHandle>();
                }
                else
                {
                    state = HeightUpdaterState.Recovering;
                }

                _cameraCoordinator.Current.RenderSelectionBox = false;
            }
        }

        private Vector2Int GetAxisCorrectedAnchor(Vector2Int tileCoords, Vector2Int anchorCoords, bool locked, PlaneAlignment lockedAxis)
        {
            if (locked)
            {
                switch (lockedAxis)
                {
                    case PlaneAlignment.Horizontal:
                        return new Vector2Int(tileCoords.x, anchorCoords.y);
                    case PlaneAlignment.Vertical:
                        return new Vector2Int(anchorCoords.x, tileCoords.y);
                }
            }

            return anchorCoords;
        }

        private void UpdateLevelArea()
        {
            Map map = _mapHandler.Map;
            int targetHeight;
            if (int.TryParse(CurrentTargetHeight, out targetHeight) == false)
            {
                targetHeight = 0;
            }

            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                state = HeightUpdaterState.Dragging;
            }

            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() && state == HeightUpdaterState.Dragging)
            {
                BeginCaveEdit(map, currentFrameHoveredHandles);
                foreach (HeightmapHandle handle in currentFrameHoveredHandles)
                {
                    SetHeightValue(map, handle, targetHeight);
                }
                FinishEdit();
                state = HeightUpdaterState.Idle;
                _cameraCoordinator.Current.RenderSelectionBox = false;
            }

            if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
            {
                CancelEdit();
                state = HeightUpdaterState.Idle;
                _cameraCoordinator.Current.RenderSelectionBox = false;
            }
        }

        private void UpdatePaintTerrain()
        {
            Map map = _mapHandler.Map;
            if (!int.TryParse(CurrentTargetHeight, out int targetHeight))
            {
                return;
            }

            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                state = HeightUpdaterState.Manipulating;
                BeginCaveEdit(map, currentFrameHoveredHandles);
            }

            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0 && state == HeightUpdaterState.Manipulating)
            {
                foreach (HeightmapHandle handle in currentFrameHoveredHandles)
                {
                    SetHeightValue(map, handle, targetHeight);
                }
            }

            if (_input.UpdatersShared.Placement.WasReleasedThisFrame())
            {
                FinishEdit();
                state = HeightUpdaterState.Idle;
            }

            if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
            {
                CancelEdit();
                state = HeightUpdaterState.Idle;
            }

        }

        private List<HeightmapHandle> UpdateHoveredHandles(RaycastHit raycast)
        {
            if (ComplexSelectionEnabled)
            {
                return UpdateHoveredHandlesComplexSelection(raycast);
            }
            else
            {
                MultiCamera hoveredCamera = _cameraCoordinator.Hovered;
                if (!hoveredCamera || hoveredCamera.CameraMode != CameraMode.Top)
                {
                    return new List<HeightmapHandle>();
                }

                return UpdateHoveredHandlesSimpleSelection(raycast);
            }
        }

        private List<HeightmapHandle> UpdateHoveredHandlesComplexSelection(RaycastHit raycast)
        {
            List<HeightmapHandle> hoveredHandles = new List<HeightmapHandle>();
            GridMesh gridMesh = IsCaveRealm
                ? _mapHandler.Map.CaveGridMesh
                : _mapHandler.Map.SurfaceGridMesh;

            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                dragStartPos = _cameraCoordinator.Current.MousePosition;
            }

            dragEndPos = _cameraCoordinator.Current.MousePosition;

            if (state == HeightUpdaterState.Dragging)
            {
                if (Vector2.Distance(dragStartPos, dragEndPos) > 5)
                {
                    _cameraCoordinator.Current.RenderSelectionBox = true;
                }

                Vector2 difference = dragEndPos - dragStartPos;
                float clampedDifferenceX = Mathf.Clamp(-difference.x, 0, float.MaxValue);
                float clampedDifferenceY = Mathf.Clamp(-difference.y, 0, float.MaxValue);
                Vector2 clampedDifference = new Vector2(clampedDifferenceX, clampedDifferenceY);

                Vector2 selectionStart = dragStartPos - clampedDifference;
                Vector2 selectionEnd = dragEndPos - dragStartPos + clampedDifference * 2;

                _cameraCoordinator.Current.SelectionBoxPosition = selectionStart;
                _cameraCoordinator.Current.SelectionBoxSize = selectionEnd;

                Vector2 viewportStart = selectionStart / _cameraCoordinator.Current.Screen.GetComponent<RectTransform>().sizeDelta;
                Vector2 viewportEnd = selectionEnd / _cameraCoordinator.Current.Screen.GetComponent<RectTransform>().sizeDelta;
                Rect viewportRect = new Rect(viewportStart, viewportEnd);

                Camera checkedCamera = _cameraCoordinator.Current.AttachedCamera;
                for (int i = 0; i <= _mapHandler.Map.Width; i++)
                {
                    for (int i2 = 0; i2 <= _mapHandler.Map.Height; i2++)
                    {
                        float height = GetWorldHeight(_mapHandler.Map, i, i2) * 0.1f;
                        Vector2 viewportLocation = checkedCamera.WorldToViewportPoint(new Vector3(i * 4, height, i2 * 4));
                        if (viewportRect.Contains(viewportLocation))
                        {
                            hoveredHandles.Add(gridMesh.GetHandle(i, i2));
                        }
                    }
                }
            }

            if (_input.UpdatersShared.Placement.WasReleasedThisFrame())
            {
                _cameraCoordinator.Current.RenderSelectionBox = false;
            }

            if (hoveredHandles.Count == 0)
            {
                HeightmapHandle heightmapHandle = raycast.transform
                    ? gridMesh.RaycastHandles(_cameraCoordinator.Current.CreateMouseRay())
                    : null;
                if (heightmapHandle != null)
                {
                    hoveredHandles.Add(heightmapHandle);
                }
            }

            return hoveredHandles;
        }

        private List<HeightmapHandle> UpdateHoveredHandlesSimpleSelection(RaycastHit raycast)
        {
            GridMesh gridMesh = IsCaveRealm
                ? _mapHandler.Map.CaveGridMesh
                : _mapHandler.Map.SurfaceGridMesh;

            List<HeightmapHandle> hoveredHandles = new List<HeightmapHandle>();

            TileSelectionHit hit = TileSelection.PositionToTileSelectionHit(raycast.point, TileSelectionMode.Everything);
            switch (hit.Target)
            {
                case TileSelectionTarget.InnerTile:
                    hoveredHandles.Add(gridMesh.GetHandle(hit.X, hit.Y));
                    hoveredHandles.Add(gridMesh.GetHandle(hit.X + 1, hit.Y));
                    hoveredHandles.Add(gridMesh.GetHandle(hit.X, hit.Y + 1));
                    hoveredHandles.Add(gridMesh.GetHandle(hit.X + 1, hit.Y + 1));
                    break;
                case TileSelectionTarget.Corner:
                    hoveredHandles.Add(gridMesh.GetHandle(hit.X, hit.Y));
                    break;
                case TileSelectionTarget.BottomBorder:
                    hoveredHandles.Add(gridMesh.GetHandle(hit.X, hit.Y));
                    hoveredHandles.Add(gridMesh.GetHandle(hit.X + 1, hit.Y));
                    break;
                case TileSelectionTarget.LeftBorder:
                    hoveredHandles.Add(gridMesh.GetHandle(hit.X, hit.Y));
                    hoveredHandles.Add(gridMesh.GetHandle(hit.X, hit.Y + 1));
                    break;
            }

            return hoveredHandles;
        }

        private bool IsCaveRealm => _cameraCoordinator.Current.Level < 0;

        private string CurrentTargetHeight
        {
            get
            {
                if (!IsCaveRealm)
                {
                    return _surfaceTargetHeight;
                }
                return _caveHeightMode == CaveHeightMode.Floor
                    ? _caveFloorTargetHeight
                    : _caveClearanceTargetHeight;
            }
        }

        private void BeginCaveEdit(Map map, IEnumerable<HeightmapHandle> handles = null)
        {
            if (!IsCaveRealm || _caveHeightEdit != null)
            {
                return;
            }

            _caveHeightEdit = _caveHeightMode == CaveHeightMode.Floor
                ? map.CaveEditor.BeginFloorHeightEdit()
                : map.CaveEditor.BeginClearanceEdit();
            _originalCaveValues.Clear();

            foreach (HeightmapHandle handle in handles ?? selectedHandles)
            {
                EnsureCaveOriginalValue(handle);
            }
            EnsureCaveOriginalValue(activeHandle);
            EnsureCaveOriginalValue(anchorHandle);
        }

        private void EnsureCaveOriginalValue(HeightmapHandle handle)
        {
            if (handle == null || _originalCaveValues.ContainsKey(handle))
            {
                return;
            }

            _originalCaveValues.Add(handle, GetCaveValue(_mapHandler.Map, handle.TileCoords));
        }

        private int GetCaveValue(Map map, Vector2Int coordinates)
        {
            return _caveHeightMode == CaveHeightMode.Floor
                ? map[coordinates].CaveHeight
                : map[coordinates].CaveSize;
        }

        private int GetWorldHeight(Map map, int x, int y)
        {
            Tile tile = map[x, y];
            if (!IsCaveRealm || _caveHeightMode == CaveHeightMode.Floor)
            {
                return tile.GetHeightForLevel(_cameraCoordinator.Current.Level);
            }
            return tile.CaveHeight + tile.CaveSize;
        }

        private void SetHeightValue(Map map, HeightmapHandle handle, int value)
        {
            if (IsCaveRealm)
            {
                EnsureCaveOriginalValue(handle);
                SetCaveValue(handle, value);
            }
            else
            {
                map[handle.TileCoords].SurfaceHeight = value;
            }
        }

        private void SetCaveValue(HeightmapHandle handle, int value)
        {
            if (_caveHeightMode == CaveHeightMode.Clearance)
            {
                value = Mathf.Max(0, value);
            }
            if (_caveHeightEdit.SetAt(handle.TileCoords.x, handle.TileCoords.y, value))
            {
                RefreshCaveVertex(_mapHandler.Map, handle.TileCoords.x, handle.TileCoords.y);
            }
        }

        private void FinishEdit()
        {
            if (_caveHeightEdit != null)
            {
                _caveHeightEdit.Commit();
                _caveHeightEdit.Dispose();
                _caveHeightEdit = null;
                _originalCaveValues.Clear();
            }
            else
            {
                _mapHandler.Map?.CommandManager.FinishAction();
            }
        }

        private void CancelEdit()
        {
            if (_caveHeightEdit != null)
            {
                _caveHeightEdit.Cancel();
                _caveHeightEdit.Dispose();
                _caveHeightEdit = null;
                _originalCaveValues.Clear();
            }
            else
            {
                _mapHandler.Map?.CommandManager.UndoAction();
            }
        }

        private void RefreshCaveGrid()
        {
            Map map = _mapHandler.Map;
            if (map == null)
            {
                return;
            }

            for (int x = 0; x <= map.Width; x++)
            {
                for (int y = 0; y <= map.Height; y++)
                {
                    RefreshCaveVertex(map, x, y);
                }
            }
            map.CaveGridMesh.ApplyAllChanges();
        }

        private void RefreshCaveVertex(Map map, int x, int y)
        {
            int displayValue = _caveHeightMode == CaveHeightMode.Floor
                ? map[x, y].CaveHeight
                : map[x, y].CaveSize;
            int worldHeight = _caveHeightMode == CaveHeightMode.Floor
                ? map[x, y].CaveHeight
                : map[x, y].CaveHeight + map[x, y].CaveSize;
            map.CaveGridMesh.SetDisplayHeight(x, y, worldHeight, displayValue);
        }

        private void ObserveMap(Map map)
        {
            if (_observedMap == map)
            {
                return;
            }
            if (_observedMap != null)
            {
                _observedMap.CaveEditor.EditCompleted -= OnCaveEditCompleted;
            }
            _observedMap = map;
            if (_observedMap != null)
            {
                _observedMap.CaveEditor.EditCompleted += OnCaveEditCompleted;
            }
        }

        private void OnCaveEditCompleted(CaveDirtyRegion _)
        {
            if (IsCaveRealm)
            {
                RefreshCaveGrid();
            }
        }

        private void RestoreCaveGridFloor()
        {
            Map map = _mapHandler.Map;
            if (map == null)
            {
                return;
            }

            for (int x = 0; x <= map.Width; x++)
            {
                for (int y = 0; y <= map.Height; y++)
                {
                    map.CaveGridMesh.SetHeight(x, y, map[x, y].CaveHeight);
                }
            }
            map.CaveGridMesh.ApplyAllChanges();
        }

        private void UpdateHandlesColors()
        {
            foreach (HeightmapHandle handle in currentFrameHoveredHandles)
            {
                if (!selectedHandles.Contains(handle))
                {
                    handle.Color = HoveredColor;
                }
            }

            foreach (HeightmapHandle handle in lastFrameHoveredHandles)
            {
                if (!currentFrameHoveredHandles.Contains(handle) && !selectedHandles.Contains(handle))
                {
                    handle.Color = NeutralColor;
                }
            }

            foreach (HeightmapHandle handle in selectedHandles)
            {
                if (handle == anchorHandle)
                {
                    handle.Color = AnchorColor;
                }
                else if (state == HeightUpdaterState.Manipulating)
                {
                    handle.Color = ActiveColor;
                }
                else if (currentFrameHoveredHandles.Count == 1 && currentFrameHoveredHandles.Contains(handle) && state != HeightUpdaterState.Dragging)
                {
                    handle.Color = SelectedHoveredColor;
                }
                else
                {
                    handle.Color = SelectedColor;
                }
            }

            foreach (HeightmapHandle handle in deselectedHandles)
            {
                handle.Color = NeutralColor;
            }
        }

        public void Disable()
        {
            if (_anchorProjector != null)
            {
                _mapProjectorFacade.FreeProjector(_anchorProjector);
                _anchorProjector = null;
            }
            ResetState();
            RestoreCaveGridFloor();
            ObserveMap(null);
            _lastCaveRealm = null;
            _view.ShowCaveHeightModes(false);
        }

        private enum HeightUpdaterState
        {
            Idle, Dragging, Manipulating, Recovering
        }
    }
}
