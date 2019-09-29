using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public class HeightUpdater : AbstractUpdater
    {

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
        private PlaneLine anchorPlaneLine;

        private HeightUpdaterMode mode = HeightUpdaterMode.SelectAndDrag;
        private HeightUpdaterState state = HeightUpdaterState.Idle;
        private Vector2 dragStartPos;
        private Vector2 dragEndPos;

        private bool ComplexSelectionEnabled => mode != HeightUpdaterMode.PaintTerrain;

        private void Awake()
        {
            anchorPlaneLine = Instantiate(GameManager.Instance.PlaneLinePrefab, transform);
            anchorPlaneLine.gameObject.SetActive(false);
        }

        private void Start()
        {
            dragSensitivityInput.text = Properties.Instance.HeightDragSensitivity.ToString(CultureInfo.InvariantCulture);
            respectOriginalSlopesToggle.isOn = Properties.Instance.HeightRespectOriginalSlopes;
        }
        
        private void OnEnable()
        {
            RefreshTileSelectionMode();
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
            if (anchorPlaneLine)
            {
                anchorPlaneLine.gameObject.SetActive(false);
            }
            state = HeightUpdaterState.Idle;
            GameManager.Instance.Map.CommandManager.UndoAction();
            UpdateHandlesColors();
            LayoutManager.Instance.CurrentCamera.RenderSelectionBox = false;
        }

        private void Update()
        {
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;
            bool cameraOnScreen = LayoutManager.Instance.CurrentCamera.MouseOver;

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

            if (activeHandle)
            {
                LayoutManager.Instance.TooltipText = activeHandle.ToRichString();
            }
        }

        private void UpdateSelectAndDrag()
        {
            Map map = GameManager.Instance.Map;
            float dragSensitivity = 0;
            float.TryParse(dragSensitivityInput.text, NumberStyles.Any, CultureInfo.InvariantCulture, out dragSensitivity);
            bool respectSlopes = respectOriginalSlopesToggle.isOn;

            bool propertiesNeedSaving = false;
            
            if (Math.Abs(dragSensitivity - Properties.Instance.HeightDragSensitivity) > float.Epsilon)
            {
                Properties.Instance.HeightDragSensitivity = dragSensitivity;
                propertiesNeedSaving = true;
            }
            if (respectSlopes != Properties.Instance.HeightRespectOriginalSlopes)
            {
                Properties.Instance.HeightRespectOriginalSlopes = respectSlopes;
                propertiesNeedSaving = true;
            }

            if (propertiesNeedSaving)
            {
                Properties.Instance.SaveProperties();
            }
            
            if (Input.GetMouseButtonDown(0))
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

            if (Input.GetMouseButton(0))
            {
                if (state == HeightUpdaterState.Manipulating)
                {
                    map.CommandManager.UndoAction();
                    int originalHeight = map[activeHandle.TileCoords].SurfaceHeight;
                    int heightDelta = (int) ((dragEndPos.y - dragStartPos.y) * dragSensitivity);
                    foreach (HeightmapHandle heightmapHandle in selectedHandles)
                    {
                        Vector2Int tileCoords = heightmapHandle.TileCoords;
                        if (respectSlopes)
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
            
            if (Input.GetMouseButtonUp(0))
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

            if (Input.GetMouseButtonDown(1))
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
                
                LayoutManager.Instance.CurrentCamera.RenderSelectionBox = false;
            }
        }

        private void UpdateCreateRamps()
        {
            Map map = GameManager.Instance.Map;
            float dragSensitivity = 0;
            float.TryParse(dragSensitivityInput.text, NumberStyles.Any, CultureInfo.InvariantCulture, out dragSensitivity);
            bool respectSlopes = respectOriginalSlopesToggle.isOn;

            if (state == HeightUpdaterState.Recovering)
            {
                state = HeightUpdaterState.Idle;
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                if (currentFrameHoveredHandles.Count == 1 && selectedHandles.Contains(currentFrameHoveredHandles[0]))
                {
                    if (anchorHandle && anchorHandle != currentFrameHoveredHandles[0])
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
                    anchorPlaneLine.gameObject.SetActive(false);
                    state = HeightUpdaterState.Dragging;
                }
            }

            if (Input.GetMouseButton(0))
            {
                if (state == HeightUpdaterState.Manipulating)
                {
                    if (activeHandle)
                    {
                        map.CommandManager.UndoAction();
                        bool locked = anchorPlaneLine.gameObject.activeSelf;
                        PlaneAlignment lockedAxis = anchorPlaneLine.Alignment;
                        int originalHeight = map[activeHandle.TileCoords].SurfaceHeight;
                        int heightDelta = (int) ((dragEndPos.y - dragStartPos.y) * dragSensitivity);
                        Vector2Int manipulatedTileCoords = activeHandle.TileCoords;
                        Vector2Int manipulatedAnchorCoords = GetAxisCorrectedAnchor(manipulatedTileCoords, anchorHandle.TileCoords, locked, lockedAxis);
                        Vector2Int manipulatedDifference = manipulatedTileCoords - manipulatedAnchorCoords;
                        
                        foreach (HeightmapHandle heightmapHandle in selectedHandles)
                        {
                            Vector2Int tileCoords = heightmapHandle.TileCoords;
                            Vector2Int anchorCoords = GetAxisCorrectedAnchor(tileCoords, anchorHandle.TileCoords, locked, lockedAxis);
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
                    else if (anchorHandle)
                    {
                        float anchorPositionX = anchorHandle.TileCoords.x * 4;
                        float anchorPositionY = anchorHandle.TileCoords.y * 4;
                        Vector2 anchorPosition = new Vector2(anchorPositionX, anchorPositionY);

                        Vector3 raycastPoint = LayoutManager.Instance.CurrentCamera.CurrentRaycast.point;
                        Vector2 raycastPosition = new Vector2(raycastPoint.x, raycastPoint.z);

                        Vector2 positionDelta = raycastPosition - anchorPosition;
                        if (positionDelta.magnitude > 4)
                        {
                            anchorPlaneLine.gameObject.SetActive(true);
                            anchorPlaneLine.TileCoords = anchorHandle.TileCoords;
                            bool horizontal = Mathf.Abs(positionDelta.x) > Mathf.Abs(positionDelta.y);
                            anchorPlaneLine.Alignment = horizontal ? PlaneAlignment.Vertical : PlaneAlignment.Horizontal;
                        }
                        else
                        {
                            anchorPlaneLine.gameObject.SetActive(false);
                        }
                    }
                }
            }
            
            if (Input.GetMouseButtonUp(0))
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

            if (Input.GetMouseButtonDown(1))
            {
                if (state == HeightUpdaterState.Manipulating && activeHandle)
                {
                    map.CommandManager.UndoAction();
                    activeHandle = null;
                    state = HeightUpdaterState.Recovering;
                }
                else if (anchorHandle)
                {
                    anchorHandle = null;
                    anchorPlaneLine.gameObject.SetActive(false);
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
                
                LayoutManager.Instance.CurrentCamera.RenderSelectionBox = false;
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
            Map map = GameManager.Instance.Map;
            int targetHeight = int.Parse(targetHeightInput.text);

            if (Input.GetMouseButtonDown(0))
            {
                state = HeightUpdaterState.Dragging;
            }

            if (Input.GetMouseButtonUp(0) && state == HeightUpdaterState.Dragging)
            {
                foreach (HeightmapHandle handle in currentFrameHoveredHandles)
                {
                    map[handle.TileCoords].SurfaceHeight = targetHeight;
                }
                map.CommandManager.FinishAction();
                state = HeightUpdaterState.Idle;
                LayoutManager.Instance.CurrentCamera.RenderSelectionBox = false;
            }

            if (Input.GetMouseButtonDown(1))
            {
                map.CommandManager.UndoAction();
                state = HeightUpdaterState.Idle;
                LayoutManager.Instance.CurrentCamera.RenderSelectionBox = false;
            }
        }

        private void UpdatePaintTerrain()
        {
            Map map = GameManager.Instance.Map;
            int targetHeight = int.Parse(targetHeightInput.text);

            if (Input.GetMouseButtonDown(0))
            {
                state = HeightUpdaterState.Manipulating;
            }
            
            if (Input.GetMouseButton(0) && state == HeightUpdaterState.Manipulating)
            {
                foreach (HeightmapHandle handle in currentFrameHoveredHandles)
                {
                    map[handle.TileCoords].SurfaceHeight = targetHeight;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                map.CommandManager.FinishAction();
                state = HeightUpdaterState.Idle;
            }

            if (Input.GetMouseButtonDown(1))
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
                MultiCamera hoveredCamera = LayoutManager.Instance.HoveredCamera;
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
                dragStartPos = LayoutManager.Instance.CurrentCamera.MousePosition;
            }
            
            dragEndPos = LayoutManager.Instance.CurrentCamera.MousePosition;
            
            if (state == HeightUpdaterState.Dragging)
            {
                if (Vector2.Distance(dragStartPos, dragEndPos) > 5)
                {
                    LayoutManager.Instance.CurrentCamera.RenderSelectionBox = true;
                }
                
                Vector2 difference = dragEndPos - dragStartPos;
                float clampedDifferenceX = Mathf.Clamp(-difference.x, 0, float.MaxValue);
                float clampedDifferenceY = Mathf.Clamp(-difference.y, 0, float.MaxValue);
                Vector2 clampedDifference = new Vector2(clampedDifferenceX, clampedDifferenceY);

                Vector2 selectionStart = dragStartPos - clampedDifference;
                Vector2 selectionEnd = dragEndPos - dragStartPos + clampedDifference * 2;

                LayoutManager.Instance.CurrentCamera.SelectionBoxPosition = selectionStart;
                LayoutManager.Instance.CurrentCamera.SelectionBoxSize = selectionEnd;

                Vector2 viewportStart = selectionStart / LayoutManager.Instance.CurrentCamera.Screen.GetComponent<RectTransform>().sizeDelta;
                Vector2 viewportEnd = selectionEnd / LayoutManager.Instance.CurrentCamera.Screen.GetComponent<RectTransform>().sizeDelta;
                Rect viewportRect = new Rect(viewportStart, viewportEnd);

                Camera checkedCamera = LayoutManager.Instance.CurrentCamera.AttachedCamera;

                for (int i = 0; i <= GameManager.Instance.Map.Width; i++)
                {
                    for (int i2 = 0; i2 < GameManager.Instance.Map.Height; i2++)
                    {
                        float height = GameManager.Instance.Map[i, i2].GetHeightForFloor(LayoutManager.Instance.CurrentCamera.Floor) * 0.1f;
                        Vector2 viewportLocation = checkedCamera.WorldToViewportPoint(new Vector3(i * 4, height, i2 * 4));
                        if (viewportRect.Contains(viewportLocation))
                        {
                            hoveredHandles.Add(GameManager.Instance.Map.SurfaceGridMesh.GetHandle(i, i2));
                        }
                    }
                }
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                LayoutManager.Instance.CurrentCamera.RenderSelectionBox = false;
            }
            
            if (hoveredHandles.Count == 0)
            {
                HeightmapHandle heightmapHandle = raycast.transform ? raycast.transform.GetComponent<HeightmapHandle>() : null;
                if (heightmapHandle)
                {
                    hoveredHandles.Add(heightmapHandle);
                }
            }
            
            return hoveredHandles;
        }

        private List<HeightmapHandle> UpdateHoveredHandlesSimpleSelection(RaycastHit raycast)
        {
            GridMesh gridMesh = GameManager.Instance.Map.SurfaceGridMesh;
            
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
