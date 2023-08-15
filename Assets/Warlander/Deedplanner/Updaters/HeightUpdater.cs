using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Warlander.Deedplanner.Logic.Projectors;
using Warlander.Deedplanner.Settings;
using Zenject;

namespace Warlander.Deedplanner.Updaters
{
    public class HeightUpdater : AbstractUpdater
    {
        [Inject] private TooltipHandler _tooltipHandler;
        [Inject] private DPSettings _settings;
        [Inject] private CameraCoordinator _cameraCoordinator;
        [Inject] private DPInput _input;
        [Inject] private GameManager _gameManager;
        [Inject] private MapProjectorManager _mapProjectorManager;
        
        [SerializeField] private Toggle selectAndDragToggle = null;
        [SerializeField] private Toggle createRampsToggle = null;
        [SerializeField] private Toggle levelAreaToggle = null;
        [SerializeField] private Toggle paintTerrainToggle = null;

        [SerializeField] private RectTransform handlesSettingsTransform = null;
        [SerializeField] private RectTransform paintingSettingsTransform = null;

        [SerializeField] private RectTransform selectAndDragInstructionsTransform = null;
        [SerializeField] private RectTransform createRampsInstructionsTransform = null;
        [SerializeField] private RectTransform levelAreaInstructionsTransform = null;
        [SerializeField] private RectTransform paintTerrainInstructionsTransform = null;
        
        [SerializeField] private TMP_InputField dragSensitivityInput = null;
        [SerializeField] private Toggle respectOriginalSlopesToggle = null;

        [SerializeField] private TMP_InputField targetHeightInput = null;

        [SerializeField] private Color neutralColor = Color.white;
        [SerializeField] private Color hoveredColor = new Color(0.7f, 0.7f, 0, 1);
        [SerializeField] private Color selectedColor = new Color(0, 1, 0, 1);
        [SerializeField] private Color selectedHoveredColor = new Color(0.7f, 0.39f, 0f);
        [SerializeField] private Color activeColor = new Color(1, 0, 0, 1);
        [SerializeField] private Color anchorColor = new Color(0, 1, 1, 1);

        private List<HeightmapHandle> currentFrameHoveredHandles = new List<HeightmapHandle>();
        private List<HeightmapHandle> lastFrameHoveredHandles = new List<HeightmapHandle>();
        private List<HeightmapHandle> selectedHandles = new List<HeightmapHandle>();
        private List<HeightmapHandle> deselectedHandles = new List<HeightmapHandle>();
        private HeightmapHandle activeHandle;
        private HeightmapHandle anchorHandle;
        private MapProjector anchorProjector;
        private PlaneAlignment anchorAlignment;

        private HeightUpdaterMode mode = HeightUpdaterMode.SelectAndDrag;
        private HeightUpdaterState state = HeightUpdaterState.Idle;
        private Vector2 dragStartPos;
        private Vector2 dragEndPos;

        private bool ComplexSelectionEnabled => mode != HeightUpdaterMode.PaintTerrain;
        
        private void Start()
        {
            dragSensitivityInput.text = _settings.HeightDragSensitivity.ToString(CultureInfo.InvariantCulture);
            respectOriginalSlopesToggle.isOn = _settings.HeightRespectOriginalSlopes;
            
            dragSensitivityInput.onValueChanged.AddListener(DragSensitivityOnValueChanged);
            respectOriginalSlopesToggle.onValueChanged.AddListener(RespectOriginalSlopesOnValueChanged);
        }
        
        private void OnEnable()
        {
            RefreshTileSelectionMode();
            anchorProjector = _mapProjectorManager.RequestProjector(ProjectorColor.Red);
            anchorProjector.gameObject.SetActive(false);
        }

        private void DragSensitivityOnValueChanged(string value)
        {
            float.TryParse(dragSensitivityInput.text, NumberStyles.Any, CultureInfo.InvariantCulture,
                out float dragSensitivity);
            
            _settings.Modify(settings =>
            {
                settings.HeightDragSensitivity = dragSensitivity;
            });
        }

        private void RespectOriginalSlopesOnValueChanged(bool value)
        {
            _settings.Modify(settings =>
            {
                settings.HeightRespectOriginalSlopes = respectOriginalSlopesToggle.isOn;
            });
        }

        public void OnModeChange(bool toggledOn)
        {
            if (!toggledOn)
            {
                return;
            }

            RefreshMode();
            RefreshGui();
            RefreshTileSelectionMode();
            ResetState();
        }

        private void RefreshMode()
        {
            if (selectAndDragToggle.isOn)
            {
                mode = HeightUpdaterMode.SelectAndDrag;
            }
            else if (createRampsToggle.isOn)
            {
                mode = HeightUpdaterMode.CreateRamps;
            }
            else if (levelAreaToggle.isOn)
            {
                mode = HeightUpdaterMode.LevelArea;
            }
            else if (paintTerrainToggle.isOn)
            {
                mode = HeightUpdaterMode.PaintTerrain;
            }
        }

        private void RefreshTileSelectionMode()
        {
            if (ComplexSelectionEnabled)
            {
                LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Tiles;
            }
            else
            {
                LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Everything;
            }
        }

        private void RefreshGui()
        {
            handlesSettingsTransform.gameObject.SetActive(mode == HeightUpdaterMode.SelectAndDrag || mode == HeightUpdaterMode.CreateRamps);
            paintingSettingsTransform.gameObject.SetActive(mode == HeightUpdaterMode.LevelArea || mode == HeightUpdaterMode.PaintTerrain);
            
            selectAndDragInstructionsTransform.gameObject.SetActive(mode == HeightUpdaterMode.SelectAndDrag);
            createRampsInstructionsTransform.gameObject.SetActive(mode == HeightUpdaterMode.CreateRamps);
            levelAreaInstructionsTransform.gameObject.SetActive(mode == HeightUpdaterMode.LevelArea);
            paintTerrainInstructionsTransform.gameObject.SetActive(mode == HeightUpdaterMode.PaintTerrain);
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
            if (anchorProjector)
            {
                anchorProjector.gameObject.SetActive(false);
            }
            state = HeightUpdaterState.Idle;
            _gameManager.Map.CommandManager.UndoAction();
            UpdateHandlesColors();
            _cameraCoordinator.Current.RenderSelectionBox = false;
        }

        private void Update()
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
                case HeightUpdaterMode.SelectAndDrag:
                    UpdateSelectAndDrag();
                    break;
                case HeightUpdaterMode.CreateRamps:
                    UpdateCreateRamps();
                    break;
                case HeightUpdaterMode.LevelArea:
                    UpdateLevelArea();
                    break;
                case HeightUpdaterMode.PaintTerrain:
                    UpdatePaintTerrain();
                    break;
            }

            UpdateHandlesColors();
            deselectedHandles = new List<HeightmapHandle>();
            lastFrameHoveredHandles = currentFrameHoveredHandles;

            if (activeHandle != null)
            {
                _tooltipHandler.ShowTooltipText(activeHandle.ToRichString());
            }
        }

        private void UpdateSelectAndDrag()
        {
            Map map = _gameManager.Map;

            if (_input.UpdatersShared.Placement.WasPressedThisFrame())
            {
                if (currentFrameHoveredHandles.Count == 1 && selectedHandles.Contains(currentFrameHoveredHandles[0]))
                {
                    activeHandle = currentFrameHoveredHandles[0];
                    state = HeightUpdaterState.Manipulating;
                }
                else if (Input.GetKey(KeyCode.LeftShift))
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
                        if (_settings.HeightRespectOriginalSlopes)
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
                if (state == HeightUpdaterState.Dragging && Input.GetKey(KeyCode.LeftShift))
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
            Map map = _gameManager.Map;
            float dragSensitivity = 0;
            float.TryParse(dragSensitivityInput.text, NumberStyles.Any, CultureInfo.InvariantCulture, out dragSensitivity);
            bool respectSlopes = respectOriginalSlopesToggle.isOn;

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
                else if (Input.GetKey(KeyCode.LeftShift))
                {
                    state = HeightUpdaterState.Dragging;
                }
                else
                {
                    deselectedHandles = selectedHandles;
                    selectedHandles = new List<HeightmapHandle>();
                    anchorHandle = null;
                    anchorProjector.gameObject.SetActive(false);
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
                        bool locked = anchorProjector.gameObject.activeSelf;
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
                            anchorProjector.gameObject.SetActive(true);
                            bool horizontal = Mathf.Abs(positionDelta.x) > Mathf.Abs(positionDelta.y);
                            anchorAlignment = horizontal ? PlaneAlignment.Vertical : PlaneAlignment.Horizontal;
                            anchorProjector.ProjectLine(anchorHandle.TileCoords, anchorAlignment);
                        }
                        else
                        {
                            anchorProjector.gameObject.SetActive(false);
                        }
                    }
                }
            }
            
            if (_input.UpdatersShared.Placement.WasReleasedThisFrame())
            {
                if (state == HeightUpdaterState.Dragging && Input.GetKey(KeyCode.LeftShift))
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
                    anchorProjector.gameObject.SetActive(false);
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
            Map map = _gameManager.Map;
            int targetHeight = int.Parse(targetHeightInput.text);

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
            Map map = _gameManager.Map;
            int targetHeight = int.Parse(targetHeightInput.text);

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

            if (Input.GetMouseButtonDown(0))
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

                for (int i = 0; i <= _gameManager.Map.Width; i++)
                {
                    for (int i2 = 0; i2 <= _gameManager.Map.Height; i2++)
                    {
                        float height = _gameManager.Map[i, i2].GetHeightForFloor(_cameraCoordinator.Current.Floor) * 0.1f;
                        Vector2 viewportLocation = checkedCamera.WorldToViewportPoint(new Vector3(i * 4, height, i2 * 4));
                        if (viewportRect.Contains(viewportLocation))
                        {
                            hoveredHandles.Add(_gameManager.Map.SurfaceGridMesh.GetHandle(i, i2));
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
                HeightmapHandle heightmapHandle = raycast.transform ? _gameManager.Map.SurfaceGridMesh.RaycastHandles() : null;
                if (heightmapHandle != null)
                {
                    hoveredHandles.Add(heightmapHandle);
                }
            }
            
            return hoveredHandles;
        }

        private List<HeightmapHandle> UpdateHoveredHandlesSimpleSelection(RaycastHit raycast)
        {
            GridMesh gridMesh = _gameManager.Map.SurfaceGridMesh;
            
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
                    handle.Color = hoveredColor;
                }
            }
            
            foreach (HeightmapHandle handle in lastFrameHoveredHandles)
            {
                if (!currentFrameHoveredHandles.Contains(handle) && !selectedHandles.Contains(handle))
                {
                    handle.Color = neutralColor;
                }
            }
            
            foreach (HeightmapHandle handle in selectedHandles)
            {
                if (handle == anchorHandle)
                {
                    handle.Color = anchorColor;
                }
                else if (state == HeightUpdaterState.Manipulating)
                {
                    handle.Color = activeColor;
                }
                else if (currentFrameHoveredHandles.Count == 1 && currentFrameHoveredHandles.Contains(handle) && state != HeightUpdaterState.Dragging)
                {
                    handle.Color = selectedHoveredColor;
                }
                else
                {
                    handle.Color = selectedColor;
                }
            }
            
            foreach (HeightmapHandle handle in deselectedHandles)
            {
                handle.Color = neutralColor;
            }
        }

        private void OnDisable()
        {
            _mapProjectorManager.FreeProjector(anchorProjector);
            anchorProjector = null;
            ResetState();
        }

        private enum HeightUpdaterState
        {
            Idle, Dragging, Manipulating, Recovering
        }

        private enum HeightUpdaterMode
        {
            SelectAndDrag, CreateRamps, LevelArea, PaintTerrain
        }
    }
}
