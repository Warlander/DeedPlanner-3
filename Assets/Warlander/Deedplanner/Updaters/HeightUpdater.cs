using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Rendering.Projectors;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Gui.Updaters;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Settings;

namespace Warlander.Deedplanner.Updaters
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
        private readonly DPSettings _settings;
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

        private HeightMode mode = HeightMode.SelectAndDrag;
        private HeightUpdaterState state = HeightUpdaterState.Idle;
        private Vector2 dragStartPos;
        private Vector2 dragEndPos;

        private string _dragSensitivity;
        private bool _respectOriginalSlopes;
        private string _targetHeight = "0";

        private SlopeGridView _slopeGrid;
        private readonly int[] _heightsBuffer = new int[9];

        private bool ComplexSelectionEnabled => mode != HeightMode.PaintTerrain;

        public HeightUpdater(IHeightUpdaterView view, TooltipHandler tooltipHandler, DPSettings settings,
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

            _dragSensitivity = _settings.HeightDragSensitivity.ToString(CultureInfo.InvariantCulture);
            _respectOriginalSlopes = _settings.HeightRespectOriginalSlopes;

            _view.SetDragSensitivity(_dragSensitivity);
            _view.SetRespectOriginalSlopes(_respectOriginalSlopes);
        }

        public void Enable()
        {
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

            _settings.Modify(settings =>
            {
                settings.HeightDragSensitivity = dragSensitivity;
            });
        }

        private void OnRespectOriginalSlopesChanged(bool value)
        {
            _respectOriginalSlopes = value;
            _settings.Modify(settings =>
            {
                settings.HeightRespectOriginalSlopes = value;
            });
        }

        private void OnTargetHeightChanged(string value)
        {
            _targetHeight = value;
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
            _mapHandler.Map.CommandManager.UndoAction();
            UpdateHandlesColors();
            _cameraCoordinator.Current.RenderSelectionBox = false;
        }

        public void Tick()
        {
            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            bool cameraOnScreen = _cameraCoordinator.Current.MouseOver;

            if (!cameraOnScreen)
            {
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

            UpdateHandlesColors();
            deselectedHandles = new List<HeightmapHandle>();
            lastFrameHoveredHandles = currentFrameHoveredHandles;

            if (activeHandle != null)
            {
                if (_slopeGrid == null)
                {
                    _slopeGrid = _tooltipHandler.GetContent<SlopeGridView>();
                }
                _tooltipHandler.ShowTooltipText("X: " + activeHandle.TileCoords.x + " Y: " + activeHandle.TileCoords.y);
                activeHandle.WriteSlopeGridData(_mapHandler.Map, _cameraCoordinator.Current.Level, _heightsBuffer);
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
                    map.CommandManager.UndoAction();
                    int originalHeight = map[activeHandle.TileCoords].SurfaceHeight;
                    int heightDelta = (int) ((dragEndPos.y - dragStartPos.y) * _settings.HeightDragSensitivity);
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
                    map.CommandManager.FinishAction();
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
                    map.CommandManager.UndoAction();
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
                        map.CommandManager.UndoAction();
                        bool locked = _anchorProjector != null;
                        int originalHeight = map[anchorHandle.TileCoords].SurfaceHeight;
                        int heightDelta = (int) ((dragEndPos.y - dragStartPos.y) * dragSensitivity);

                        // instantly make smooth ramp from anchor handle to active handle if original slopes are not respected
                        // turned off if original slopes are respected, because instantly making ramp is impractical in such case
                        if (!respectSlopes)
                        {
                            heightDelta += map[activeHandle.TileCoords].SurfaceHeight - originalHeight;
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
                                map[tileCoords].SurfaceHeight += (int) (heightDelta * delta);
                            }
                            else
                            {
                                map[tileCoords].SurfaceHeight = originalHeight + (int) (heightDelta * delta);
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
                    map.CommandManager.FinishAction();
                    activeHandle = null;
                }
                state = HeightUpdaterState.Idle;
            }

            if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
            {
                if (state == HeightUpdaterState.Manipulating && activeHandle != null)
                {
                    map.CommandManager.UndoAction();
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
            if (int.TryParse(_targetHeight, out targetHeight) == false)
            {
                targetHeight = 0;
            }

            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                state = HeightUpdaterState.Dragging;
            }

            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() && state == HeightUpdaterState.Dragging)
            {
                foreach (HeightmapHandle handle in currentFrameHoveredHandles)
                {
                    map[handle.TileCoords].SurfaceHeight = targetHeight;
                }
                map.CommandManager.FinishAction();
                state = HeightUpdaterState.Idle;
                _cameraCoordinator.Current.RenderSelectionBox = false;
            }

            if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
            {
                map.CommandManager.UndoAction();
                state = HeightUpdaterState.Idle;
                _cameraCoordinator.Current.RenderSelectionBox = false;
            }
        }

        private void UpdatePaintTerrain()
        {
            Map map = _mapHandler.Map;
            int targetHeight = int.Parse(_targetHeight);

            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                state = HeightUpdaterState.Manipulating;
            }

            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0 && state == HeightUpdaterState.Manipulating)
            {
                foreach (HeightmapHandle handle in currentFrameHoveredHandles)
                {
                    map[handle.TileCoords].SurfaceHeight = targetHeight;
                }
            }

            if (_input.UpdatersShared.Placement.WasReleasedThisFrame())
            {
                map.CommandManager.FinishAction();
                state = HeightUpdaterState.Idle;
            }

            if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
            {
                map.CommandManager.UndoAction();
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
                        float height = _mapHandler.Map[i, i2].GetHeightForLevel(_cameraCoordinator.Current.Level) * 0.1f;
                        Vector2 viewportLocation = checkedCamera.WorldToViewportPoint(new Vector3(i * 4, height, i2 * 4));
                        if (viewportRect.Contains(viewportLocation))
                        {
                            hoveredHandles.Add(_mapHandler.Map.SurfaceGridMesh.GetHandle(i, i2));
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
                HeightmapHandle heightmapHandle = raycast.transform ? _mapHandler.Map.SurfaceGridMesh.RaycastHandles() : null;
                if (heightmapHandle != null)
                {
                    hoveredHandles.Add(heightmapHandle);
                }
            }

            return hoveredHandles;
        }

        private List<HeightmapHandle> UpdateHoveredHandlesSimpleSelection(RaycastHit raycast)
        {
            GridMesh gridMesh = _mapHandler.Map.SurfaceGridMesh;

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
        }

        private enum HeightUpdaterState
        {
            Idle, Dragging, Manipulating, Recovering
        }
    }
}
