using System;
using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Decorations;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Deedplanner.Rendering.Outline;
using Warlander.Deedplanner.Rendering.Water;
using Warlander.Deedplanner.Rendering.Projectors;
using Warlander.Deedplanner.Ui;
using Warlander.Deedplanner.Ui.Tooltips;
using Warlander.Deedplanner.Ui.Widgets;
using Warlander.Deedplanner.Inputs;
using VContainer;
using Warlander.Deedplanner.Caves;

namespace Warlander.Deedplanner.Cameras
{
    [RequireComponent(typeof(Camera))]
    public class MultiCamera : MonoBehaviour
    {
        [Inject] private IReadOnlyList<ICameraController> _cameraControllers;
        [Inject] private TooltipHandler _tooltipHandler;
        [Inject] private CameraCoordinator _cameraCoordinator;
        [Inject] private DPInput _input;
        [Inject] private MapHandler _mapHandler;
        [Inject] private IMapProjectorFacade _mapProjectorFacade;
        [Inject] private IOutlineCoordinator _outlineCoordinator;
        [Inject] private ISharedMaterials _sharedMaterials;
        [Inject] private IWaterFacade _waterFacade;
        [Inject] private TabContext _tabContext;
        [Inject] private ICaveHitResolver _caveHitResolver;

        public event Action LevelChanged;
        public event Action ModeChanged;
        public event Action<MultiCamera> PointerDown;
        
        public Camera AttachedCamera { get; private set; }
        public Vector2 MousePosition { get; private set; }

        [SerializeField] private int screenId = 0;
        [SerializeField] private GameObject screen = null;

        [SerializeField] private RectTransform selectionBox = null;

        [SerializeField] private Color pickerColor = new Color(1f, 1f, 0, 0.3f);

        public bool MouseOver { get; private set; } = false;

        public RaycastHit CurrentRaycast { get; private set; }
        public CaveHit CurrentCaveHit { get; private set; }
        public bool HasCurrentCaveHit { get; private set; }

        public ICameraController CameraController
        {
            get
            {
                foreach (ICameraController controller in _cameraControllers)
                {
                    if (controller.SupportsMode(CameraMode))
                    {
                        return controller;
                    }
                }

                return null;
            }
        }

        public CameraMode CameraMode {
            get => cameraMode;
            set {
                cameraMode = value;
                ModeChanged?.Invoke();
                UpdateState();
            }
        }

        public int Level {
            get => _level;
            set {
                _level = value;
                LevelChanged?.Invoke();
                UpdateState();
            }
        }

        public int ScreenId => screenId;
        public bool RenderEntireMap => CameraMode == CameraMode.Perspective || CameraMode == CameraMode.Wurmian;

        public GameObject Screen => screen;

        public bool RenderSelectionBox
        {
            get => selectionBox.gameObject.activeSelf;
            set
            {
                if (selectionBox)
                {
                    selectionBox.gameObject.SetActive(value);
                }
            }
        }

        public Vector2 SelectionBoxPosition {
            get => selectionBox.anchoredPosition;
            set => selectionBox.anchoredPosition = value;
        }

        public Vector2 SelectionBoxSize {
            get => selectionBox.sizeDelta;
            set => selectionBox.sizeDelta = value;
        }
        
        private IMapProjector _attachedProjector;
        private DynamicModelBehaviour _outlinedModel;
        private SlopeGridView _slopeGrid;
        private readonly int[] _heightsBuffer = new int[9];
        private IDisposable _mapRenderScope;
        private IDisposable _gridRenderScope;

        private CameraMode cameraMode = CameraMode.Top;
        private int _level = 0;

        private void Awake()
        {
            AttachedCamera = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += RenderPipelineManagerOnbeginCameraRendering;
            RenderPipelineManager.endCameraRendering += RenderPipelineManagerOnEndCameraRendering;
        }
        
        private void Start()
        {
            MouseEventCatcher eventCatcher = screen.GetComponent<MouseEventCatcher>();

            eventCatcher.OnDragEvent.AddListener(data =>
            {
                if (data.button != PointerEventData.InputButton.Middle)
                {
                    return;
                }

                CameraController.UpdateDrag(AttachedCamera, data);
            });

            eventCatcher.OnBeginDragEvent.AddListener(data =>
            {
                if (data.button == PointerEventData.InputButton.Middle)
                {
                    Cursor.visible = false;
                }
            });

            eventCatcher.OnEndDragEvent.AddListener(data =>
            {
                if (data.button == PointerEventData.InputButton.Middle)
                {
                    Cursor.visible = true;
                }
            });

            eventCatcher.OnPointerEnterEvent.AddListener(data => MouseOver = true);
            eventCatcher.OnPointerExitEvent.AddListener(data => MouseOver = false);
            eventCatcher.OnPointerDownEvent.AddListener(data => PointerDown?.Invoke(this));

            CameraMode = cameraMode;
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= RenderPipelineManagerOnbeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= RenderPipelineManagerOnEndCameraRendering;
            CompleteCameraRendering();
        }

        private void Update()
        {
            if (_mapHandler.Map == null)
            {
                return;
            }
            
            Map map = _mapHandler.Map;

            GameObject focusedObject = EventSystem.current.currentSelectedGameObject;
            bool shouldUpdateCameras = !focusedObject;

            if (shouldUpdateCameras)
            {
                Vector3 focusedPoint = CurrentRaycast.point;
                bool focusedWindow = _cameraCoordinator.ActiveId == screenId;
                CameraController.UpdateInput(map, cameraMode, focusedPoint, AttachedCamera.aspect, _level, focusedWindow, MouseOver);
            }

            UpdateState();
        }

        private void RenderPipelineManagerOnbeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != AttachedCamera)
            {
                return;
            }
            
            if (_mapHandler.Map == null)
            {
                return;
            }

            Map map = _mapHandler.Map;
            CompleteCameraRendering();
            if (this != _cameraCoordinator.Current)
            {
                _mapRenderScope = map.PrepareForCamera(new MapRenderView(Level, RenderEntireMap, map.RenderGrid));
            }
            bool renderWater = RenderEntireMap || Level == 0 || Level == -1;
            _waterFacade.PrepareForCamera(AttachedCamera, CameraController, renderWater);
            PrepareGridState();
            UpdateRaycast();
            UpdateHoverOutline();
            PrepareProjector();
        }

        private void RenderPipelineManagerOnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == AttachedCamera)
            {
                CompleteCameraRendering();
            }
        }

        private void CompleteCameraRendering()
        {
            _gridRenderScope?.Dispose();
            _gridRenderScope = null;
            _mapRenderScope?.Dispose();
            _mapRenderScope = null;
        }

        private void UpdateHoverOutline()
        {
            DynamicModelBehaviour newTarget = CurrentRaycast.collider != null
                ? CurrentRaycast.collider.GetComponent<DynamicModelBehaviour>()
                : null;

            if (newTarget == _outlinedModel) return;

            if (_outlinedModel != null)
                _outlineCoordinator.RemoveObject(_outlinedModel, 0);
            if (newTarget != null)
                _outlineCoordinator.AddObject(newTarget, OutlineType.Neutral, 0);

            _outlinedModel = newTarget;
        }

        private void UpdateRaycast()
        {
            CurrentRaycast = default;
            CurrentCaveHit = default;
            HasCurrentCaveHit = false;

            if (MouseOver)
            {
                Ray ray = CreateMouseRay();
                RaycastHit raycastHit;
                int mask = LayerMasks.GetMaskForTab(_tabContext.CurrentTab, Level);
                bool hit = Physics.Raycast(ray, out raycastHit, 20000, mask);
                StringBuilder tooltipBuild = new StringBuilder();

                if (hit && Cursor.visible)
                {
                    CurrentRaycast = raycastHit;
                    CaveChunk caveChunk = raycastHit.collider.GetComponent<CaveChunk>();
                    HasCurrentCaveHit = _caveHitResolver.TryResolve(caveChunk, raycastHit.triangleIndex,
                        out CaveHit caveHit);
                    CurrentCaveHit = caveHit;

                    bool isHeightEditing = _tabContext.CurrentTab == Tab.Height;

                    GameObject hitObject = raycastHit.transform.gameObject;
                    TileEntity tileEntity = hitObject.GetComponent<TileEntity>();
                    GroundMesh groundMesh = hitObject.GetComponent<GroundMesh>();
                    OverlayMesh overlayMesh = hitObject.GetComponent<OverlayMesh>();
                    GridMesh activeGrid = Level < 0
                        ? _mapHandler.Map.CaveGridMesh
                        : _mapHandler.Map.SurfaceGridMesh;
                    HeightmapHandle heightmapHandle = isHeightEditing
                        ? activeGrid.RaycastHandles(CreateMouseRay())
                        : null;

                    if (tileEntity)
                    {
                        tooltipBuild.Append(tileEntity.ToString());
                    }
                    else if (groundMesh)
                    {
                        int x = Mathf.FloorToInt(raycastHit.point.x / 4f);
                        int y = Mathf.FloorToInt(raycastHit.point.z / 4f);

                        if (heightmapHandle != null)
                        {
                            ShowSlopeGridTooltip(heightmapHandle, tooltipBuild);
                        }
                        else if (isHeightEditing)
                        {
                            tooltipBuild.Append("X: " + x + " Y: " + y).AppendLine();
                            _tooltipHandler.ShowTooltipText(tooltipBuild.ToString());
                            tooltipBuild.Clear();

                            Map map = _mapHandler.Map;
                            Vector3 raycastPoint = raycastHit.point;
                            Vector2Int tileCoords = new Vector2Int(Mathf.FloorToInt(raycastPoint.x / 4), Mathf.FloorToInt(raycastPoint.z / 4));
                            int clampedX = Mathf.Clamp(tileCoords.x, 0, map.Width);
                            int clampedY = Mathf.Clamp(tileCoords.y, 0, map.Height);
                            tileCoords = new Vector2Int(clampedX, clampedY);

                            _heightsBuffer[0] = map[tileCoords.x, tileCoords.y + 1].GetHeightForLevel(_level);
                            _heightsBuffer[1] = map[tileCoords.x + 1, tileCoords.y + 1].GetHeightForLevel(_level);
                            _heightsBuffer[2] = map[tileCoords.x, tileCoords.y].GetHeightForLevel(_level);
                            _heightsBuffer[3] = map[tileCoords.x + 1, tileCoords.y].GetHeightForLevel(_level);
                            SlopeGrid.SetData(new SlopeGridData(2, _heightsBuffer));
                            _tooltipHandler.ShowTooltipContent(SlopeGrid);
                        }
                        else
                        {
                            tooltipBuild.Append("X: " + x + " Y: " + y).AppendLine();
                            tooltipBuild.Append(_mapHandler.Map[x, y].Ground.Data.Name);
                        }
                    }
                    else if (overlayMesh)
                    {
                        int x = Mathf.FloorToInt(raycastHit.point.x / 4f);
                        int y = Mathf.FloorToInt(raycastHit.point.z / 4f);
                        tooltipBuild.Append("X: " + x + " Y: " + y);
                    }
                    else if (heightmapHandle != null)
                    {
                        ShowSlopeGridTooltip(heightmapHandle, tooltipBuild);
                    }
                    else if (HasCurrentCaveHit && !isHeightEditing)
                    {
                        tooltipBuild.Append("X: ").Append(caveHit.CellX)
                            .Append(" Y: ").Append(caveHit.CellY).AppendLine();
                        tooltipBuild.Append(_mapHandler.Map[caveHit.CellX, caveHit.CellY].Cave.Terrain.Name);
                    }

                    Decoration hoveredDecoration = FindClosestDecorationToCursor(ray, mask);
                    if (hoveredDecoration != null)
                    {
                        if (tooltipBuild.Length > 0)
                        {
                            tooltipBuild.AppendLine();
                        }
                        tooltipBuild.Append(hoveredDecoration.Data.Name);
                    }
                }

                string tooltip = tooltipBuild.ToString();
                if (string.IsNullOrEmpty(tooltip) == false)
                {
                    _tooltipHandler.ShowTooltipText(tooltip);
                }
                
            }
        }

        private SlopeGridView SlopeGrid => _slopeGrid != null ? _slopeGrid : (_slopeGrid = _tooltipHandler.GetContent<SlopeGridView>());

        private void ShowSlopeGridTooltip(HeightmapHandle handle, StringBuilder tooltipBuild)
        {
            tooltipBuild.Append("X: " + handle.TileCoords.x + " Y: " + handle.TileCoords.y).AppendLine();
            _tooltipHandler.ShowTooltipText(tooltipBuild.ToString());
            tooltipBuild.Clear();
            GridMesh activeGrid = Level < 0
                ? _mapHandler.Map.CaveGridMesh
                : _mapHandler.Map.SurfaceGridMesh;
            activeGrid.WriteSlopeGridData(handle.TileCoords, _heightsBuffer);
            SlopeGrid.SetData(new SlopeGridData(3, _heightsBuffer));
            _tooltipHandler.ShowTooltipContent(SlopeGrid);
        }

        private Decoration FindClosestDecorationToCursor(Ray ray, int mask)        {
            if ((mask & (LayerMasks.DecorationMask | LayerMasks.CaveDecorationMask)) == 0)
            {
                return null;
            }

            Map map = _mapHandler.Map;
            int tileX = Mathf.FloorToInt(CurrentRaycast.point.x / 4f);
            int tileY = Mathf.FloorToInt(CurrentRaycast.point.z / 4f);
            Tile centerTile = map[tileX, tileY];
            if (centerTile == null)
            {
                return null;
            }

            Decoration closestDecoration = null;
            float closestDistance = float.MaxValue;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Tile tile = map.GetRelativeTile(centerTile, x, y);
                    if (tile == null)
                    {
                        continue;
                    }

                    foreach (Decoration decoration in tile.GetDecorations())
                    {
                        if (decoration.WorldBounds.IntersectRay(ray, out float distance) && distance < closestDistance)
                        {
                            closestDecoration = decoration;
                            closestDistance = distance;
                        }
                    }
                }
            }

            return closestDecoration;
        }

        public Ray CreateMouseRay()
        {
            Vector2 local;
            Vector2 focusPos = _input.MapInputShared.FocusPosition.ReadValue<Vector2>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(screen.GetComponent<RectTransform>(), focusPos, null, out local);
            MousePosition = local + (screen.GetComponent<RectTransform>().sizeDelta / 2);
            local /= screen.GetComponent<RectTransform>().sizeDelta;
            local += new Vector2(0.5f, 0.5f);
            Ray ray = AttachedCamera.ViewportPointToRay(local);
            return ray;
        }

        private void PrepareGridState()
        {
            Tab tab = _tabContext.CurrentTab;
            Map map = _mapHandler.Map;
            bool renderHeights = tab == Tab.Height;
            GridMesh gridMeshToUse = Level < 0 ? map.CaveGridMesh : map.SurfaceGridMesh;
            _gridRenderScope = gridMeshToUse.PrepareForCamera(renderHeights,
                CameraController.CalculateGridAlphaMultiplier(),
                GetMaterialForGridMaterialType(CameraController.GridMaterialToUse));
            if (renderHeights && this == _cameraCoordinator.Current)
            {
                gridMeshToUse.RenderHandles(AttachedCamera);
            }
        }

        private Material GetMaterialForGridMaterialType(GridMaterialType gridMaterialType)
        {
            switch (gridMaterialType)
            {
                case GridMaterialType.Uniform:
                    return _sharedMaterials.SimpleDrawingMaterial;
                case GridMaterialType.ProximityBased:
                    return _sharedMaterials.SimpleSubtleDrawingMaterial;
                default:
                    return _sharedMaterials.SimpleDrawingMaterial;
            }
        }
        
        private void PrepareProjector()
        {
            if (_attachedProjector != null)
            {
                _mapProjectorFacade.FreeProjector(_attachedProjector);
                _attachedProjector = null;
            }

            if (!CurrentRaycast.collider)
                return;

            GameObject hitObject = CurrentRaycast.collider.gameObject;
            if (HasCurrentCaveHit)
            {
                TileSelectionHit caveSelection = GetCaveSelectionHit(CurrentCaveHit);
                _attachedProjector = _mapProjectorFacade.RequestProjector(ProjectorColor.Yellow);
                _attachedProjector.SetRenderCameraId(screenId);
                _attachedProjector.ProjectTile(new Vector2Int(caveSelection.X, caveSelection.Y), caveSelection.Target);
                return;
            }

            bool gridOrGroundHit = hitObject.GetComponent<GroundMesh>() || hitObject.GetComponent<OverlayMesh>();
            if (!gridOrGroundHit)
                return;

            TileSelectionMode tileSelectionMode = _tabContext.TileSelectionMode;
            Vector3 raycastPosition = CurrentRaycast.point;
            TileSelectionHit tileSelectionHit = TileSelection.PositionToTileSelectionHit(raycastPosition, tileSelectionMode);
            TileSelectionTarget target = tileSelectionHit.Target;

            if (target == TileSelectionTarget.Nothing)
                return;

            _attachedProjector = _mapProjectorFacade.RequestProjector(ProjectorColor.Yellow);
            _attachedProjector.SetRenderCameraId(screenId);
            Vector2Int tileCoords = new Vector2Int(tileSelectionHit.X, tileSelectionHit.Y);
            _attachedProjector.ProjectTile(tileCoords, target);
        }

        private static TileSelectionHit GetCaveSelectionHit(CaveHit hit)
        {
            if (!hit.HasEdge)
            {
                return new TileSelectionHit(TileSelectionTarget.Tile, hit.CellX, hit.CellY);
            }

            switch (hit.Edge)
            {
                case CaveEdge.South:
                    return new TileSelectionHit(TileSelectionTarget.BottomBorder, hit.OpenCellX, hit.OpenCellY);
                case CaveEdge.East:
                    return new TileSelectionHit(TileSelectionTarget.LeftBorder, hit.OpenCellX + 1, hit.OpenCellY);
                case CaveEdge.North:
                    return new TileSelectionHit(TileSelectionTarget.BottomBorder, hit.OpenCellX, hit.OpenCellY + 1);
                case CaveEdge.West:
                    return new TileSelectionHit(TileSelectionTarget.LeftBorder, hit.OpenCellX, hit.OpenCellY);
                default:
                    return default;
            }
        }

        private void UpdateState()
        {
            CameraController.UpdateState(this, AttachedCamera.transform);
        }

        private void OnRenderObject()
        {
            if (Camera.current != AttachedCamera || !CurrentRaycast.collider)
            {
                return;
            }

            GameObject hitObject = CurrentRaycast.collider.gameObject;
            GroundMesh groundMesh = hitObject.GetComponent<GroundMesh>();
            OverlayMesh overlayMesh = hitObject.GetComponent<OverlayMesh>();
            GridMesh activeGrid = Level < 0 ? _mapHandler.Map.CaveGridMesh : _mapHandler.Map.SurfaceGridMesh;
            HeightmapHandle heightmapHandle = _tabContext.CurrentTab == Tab.Height
                ? activeGrid.RaycastHandles(CreateMouseRay())
                : null;

            bool gridOrGroundHit = groundMesh || overlayMesh || heightmapHandle != null;

            if (!gridOrGroundHit)
            {
                GL.PushMatrix();
                _sharedMaterials.SimpleDrawingMaterial.SetPass(0);
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(hitObject.transform.position, hitObject.transform.rotation, hitObject.transform.lossyScale);
                GL.MultMatrix(rotationMatrix);
                RenderRaytrace();
                GL.PopMatrix();
            }
        }

        private void RenderRaytrace()
        {
            Collider hitCollider = CurrentRaycast.collider;
            if (hitCollider == null)
            {
                return;
            }

            if (hitCollider.GetComponent<DynamicModelBehaviour>() == null
                && hitCollider.GetType() == typeof(MeshCollider))
            {
                MeshCollider meshCollider = (MeshCollider)hitCollider;
                Mesh mesh = meshCollider.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                if (normals == null || normals.Length == 0)
                {
                    normals = new Vector3[vertices.Length];
                }
                int[] triangles = mesh.triangles;
                int firstTriangle = 0;
                int triangleCount = triangles.Length / 3;
                CaveChunk caveChunk = hitCollider.GetComponent<CaveChunk>();
                if (caveChunk != null && caveChunk.TryGetLogicalFaceTriangleRange(
                        CurrentRaycast.triangleIndex, out int logicalFirst, out int logicalCount))
                {
                    firstTriangle = logicalFirst;
                    triangleCount = logicalCount;
                }
                GL.Begin(GL.TRIANGLES);
                GL.Color(pickerColor);
                int firstIndex = firstTriangle * 3;
                int lastIndex = firstIndex + triangleCount * 3;
                for (int i = firstIndex; i < lastIndex; i += 3)
                {
                    GL.Vertex(vertices[triangles[i]] + normals[triangles[i]] * 0.05f);
                    GL.Vertex(vertices[triangles[i + 1]] + normals[triangles[i + 1]] * 0.05f);
                    GL.Vertex(vertices[triangles[i + 2]] + normals[triangles[i + 2]] * 0.05f);
                }
                GL.End();
            }
            else if (hitCollider.GetType() == typeof(BoxCollider))
            {
                BoxCollider boxCollider = (BoxCollider)hitCollider;
                Vector3 size = boxCollider.size * 1.01f;
                Vector3 center = boxCollider.center;

                Vector3 v000 = center + new Vector3(-size.x, -size.y, -size.z) / 2f;
                Vector3 v001 = center + new Vector3(-size.x, -size.y, size.z) / 2f;
                Vector3 v010 = center + new Vector3(-size.x, size.y, -size.z) / 2f;
                Vector3 v011 = center + new Vector3(-size.x, size.y, size.z) / 2f;
                Vector3 v100 = center + new Vector3(size.x, -size.y, -size.z) / 2f;
                Vector3 v101 = center + new Vector3(size.x, -size.y, size.z) / 2f;
                Vector3 v110 = center + new Vector3(size.x, size.y, -size.z) / 2f;
                Vector3 v111 = center + new Vector3(size.x, size.y, size.z) / 2f;

                GL.Begin(GL.QUADS);
                GL.Color(pickerColor);
                //bottom
                GL.Vertex(v000);
                GL.Vertex(v100);
                GL.Vertex(v101);
                GL.Vertex(v001);
                //top
                GL.Vertex(v111);
                GL.Vertex(v110);
                GL.Vertex(v010);
                GL.Vertex(v011);
                //down
                GL.Vertex(v110);
                GL.Vertex(v100);
                GL.Vertex(v000);
                GL.Vertex(v010);
                //up
                GL.Vertex(v001);
                GL.Vertex(v101);
                GL.Vertex(v111);
                GL.Vertex(v011);
                //left
                GL.Vertex(v000);
                GL.Vertex(v001);
                GL.Vertex(v011);
                GL.Vertex(v010);
                //right
                GL.Vertex(v111);
                GL.Vertex(v101);
                GL.Vertex(v100);
                GL.Vertex(v110);
                GL.End();
            }
        }

    }
}
