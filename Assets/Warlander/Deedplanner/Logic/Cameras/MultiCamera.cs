using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityStandardAssets.Water;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Logic.Cameras
{
    [RequireComponent(typeof(Camera))]
    public class MultiCamera : MonoBehaviour
    {
        private Transform parentTransform;
        private readonly List<ICameraController> cameraControllers = new List<ICameraController>();
        public Camera AttachedCamera { get; private set; }
        public Vector2 MousePosition { get; private set; }
        
        [SerializeField] private int screenId = 0;
        [SerializeField] private GameObject screen = null;
        [SerializeField] private CameraMode cameraMode = CameraMode.Top;
        [SerializeField] private int floor = 0;

        [SerializeField] private Water ultraQualityWater = null;
        [SerializeField] private GameObject highQualityWater = null;
        [SerializeField] private GameObject simpleQualityWater = null;

        [SerializeField] private RectTransform selectionBox = null;
        [SerializeField] private Projector attachedProjector = null;

        [SerializeField] private Color pickerColor = new Color(1f, 1f, 0, 0.3f);

        public bool MouseOver { get; private set; } = false;

        public RaycastHit CurrentRaycast { get; private set; }

        public ICameraController CameraController
        {
            get
            {
                foreach (ICameraController controller in cameraControllers)
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
                UpdateState();
            }
        }

        public int Floor {
            get => floor;
            set {
                floor = value;
                UpdateState();
            }
        }

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

        private void Start()
        {
            cameraControllers.Add(new FppCameraController());
            cameraControllers.Add(new IsoCameraController());
            cameraControllers.Add(new TopCameraController());
            
            parentTransform = transform.parent;
            AttachedCamera = GetComponent<Camera>();

            MouseEventCatcher eventCatcher = screen.GetComponent<MouseEventCatcher>();

            eventCatcher.OnDragEvent.AddListener(data =>
            {
                if (data.button != PointerEventData.InputButton.Middle)
                {
                    return;
                }
                
                CameraController.UpdateDrag(data);
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

            CameraMode = cameraMode;

            Properties.Instance.Saved += ValidateState;
            ValidateState();
        }

        private void ValidateState()
        {
            Gui.WaterQuality waterQuality = Properties.Instance.WaterQuality;
            if (waterQuality != Gui.WaterQuality.Ultra)
            {
                ultraQualityWater.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            Map map = GameManager.Instance.Map;

            GameObject focusedObject = EventSystem.current.currentSelectedGameObject;
            bool shouldUpdateCameras = !focusedObject;

            if (shouldUpdateCameras)
            {
                Vector3 focusedPoint = CurrentRaycast.point;
                bool focusedWindow = LayoutManager.Instance.ActiveWindow == screenId;
                CameraController.UpdateInput(map, cameraMode, focusedPoint, AttachedCamera.aspect, floor, focusedWindow, MouseOver);
            }

            UpdateState();
        }

        private void OnPreCull()
        {
            PrepareWater();
            PrepareMapState();
            UpdateRaycast();
            PrepareProjector();
        }

        private void UpdateRaycast()
        {
            CurrentRaycast = default;
            
            if (MouseOver)
            {
                Ray ray = CreateMouseRay();
                RaycastHit raycastHit;
                int mask = LayerMasks.GetMaskForTab(LayoutManager.Instance.CurrentTab);
                bool hit = Physics.Raycast(ray, out raycastHit, 20000, mask);
                StringBuilder tooltipBuild = new StringBuilder();
                
                if (hit && Cursor.visible)
                {
                    CurrentRaycast = raycastHit;

                    bool isHeightEditing = LayoutManager.Instance.CurrentTab == Tab.Height;
                    
                    GameObject hitObject = raycastHit.transform.gameObject;
                    TileEntity tileEntity = hitObject.GetComponent<TileEntity>();
                    GroundMesh groundMesh = hitObject.GetComponent<GroundMesh>();
                    OverlayMesh overlayMesh = hitObject.GetComponent<OverlayMesh>();
                    HeightmapHandle heightmapHandle = GameManager.Instance.Map.SurfaceGridMesh.RaycastHandles();
                    
                    if (tileEntity)
                    {
                        tooltipBuild.Append(tileEntity.ToString());
                    }
                    else if (groundMesh)
                    {
                        int x = Mathf.FloorToInt(raycastHit.point.x / 4f);
                        int y = Mathf.FloorToInt(raycastHit.point.z / 4f);
                        tooltipBuild.Append("X: " + x + " Y: " + y).AppendLine();
                        
                        if (isHeightEditing)
                        {
                            Map map = GameManager.Instance.Map;
                            Vector3 raycastPoint = raycastHit.point;
                            Vector2Int tileCoords = new Vector2Int(Mathf.FloorToInt(raycastPoint.x / 4), Mathf.FloorToInt(raycastPoint.z / 4));
                            int clampedX = Mathf.Clamp(tileCoords.x, 0, map.Width);
                            int clampedY = Mathf.Clamp(tileCoords.y, 0, map.Height);
                            tileCoords = new Vector2Int(clampedX, clampedY);

                            int h00 = map[tileCoords.x, tileCoords.y].GetHeightForFloor(floor);
                            int h10 = map[tileCoords.x + 1, tileCoords.y].GetHeightForFloor(floor);
                            int h01 = map[tileCoords.x, tileCoords.y + 1].GetHeightForFloor(floor);
                            int h11 = map[tileCoords.x + 1, tileCoords.y + 1].GetHeightForFloor(floor);
                            int h00Digits = StringUtils.DigitsStringCount(h00);
                            int h10Digits = StringUtils.DigitsStringCount(h10);
                            int h01Digits = StringUtils.DigitsStringCount(h01);
                            int h11Digits = StringUtils.DigitsStringCount(h11);
                            int maxDigits = Mathf.Max(h00Digits, h10Digits, h01Digits, h11Digits);
                            
                            tooltipBuild.Append("<mspace=0.5em>");
                            tooltipBuild.Append(StringUtils.PaddedNumberString(h01, maxDigits)).Append("   ").Append(StringUtils.PaddedNumberString(h11, maxDigits)).AppendLine();
                            tooltipBuild.AppendLine();
                            tooltipBuild.Append(StringUtils.PaddedNumberString(h00, maxDigits)).Append("   ").Append(StringUtils.PaddedNumberString(h10, maxDigits)).Append("</mspace>");
                        }
                        else
                        {
                            tooltipBuild.Append(GameManager.Instance.Map[x, y].Ground.Data.Name);
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
                        tooltipBuild.Append(heightmapHandle.ToRichString());
                    }
                }

                LayoutManager.Instance.TooltipText = tooltipBuild.ToString();
            }
        }

        public Ray CreateMouseRay()
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(screen.GetComponent<RectTransform>(), Input.mousePosition, null, out local);
            MousePosition = local + (screen.GetComponent<RectTransform>().sizeDelta / 2);
            local /= screen.GetComponent<RectTransform>().sizeDelta;
            local += new Vector2(0.5f, 0.5f);
            Ray ray = AttachedCamera.ViewportPointToRay(local);
            return ray;
        }

        private void PrepareWater()
        {
            Tab tab = LayoutManager.Instance.CurrentTab;
            bool forceSurfaceEditing = tab == Tab.Ground || tab == Tab.Height;
            int editingFloor = forceSurfaceEditing ? 0 : Floor;
            bool renderWater = RenderEntireMap || editingFloor == 0 || editingFloor == -1;
            Vector2 waterPosition = CameraController.CalculateWaterTablePosition(AttachedCamera.transform.position);
            
            if (Properties.Instance.WaterQuality == Gui.WaterQuality.Ultra)
            {
                ultraQualityWater.gameObject.SetActive(renderWater);
                Vector3 ultraQualityWaterPosition;
                ultraQualityWaterPosition = new Vector3(waterPosition.x, ultraQualityWater.transform.position.y, waterPosition.y);
                ultraQualityWater.transform.position = ultraQualityWaterPosition;
                ultraQualityWater.Update();
            }
            else if (Properties.Instance.WaterQuality == Gui.WaterQuality.High)
            {
                highQualityWater.gameObject.SetActive(renderWater);
                highQualityWater.transform.position = new Vector3(waterPosition.x, highQualityWater.transform.position.y, waterPosition.y);
            }
            else if (Properties.Instance.WaterQuality == Gui.WaterQuality.Simple)
            {
                simpleQualityWater.gameObject.SetActive(renderWater);
            }
        }

        private void PrepareMapState()
        {
            Tab tab = LayoutManager.Instance.CurrentTab;
            bool forceSurfaceEditing = tab == Tab.Ground || tab == Tab.Height;
            int editingFloor = forceSurfaceEditing ? 0 : Floor;
            
            Map map = GameManager.Instance.Map;
            if (map.RenderedFloor != editingFloor)
            {
                map.RenderedFloor = editingFloor;
            }
            if (map.RenderEntireMap != RenderEntireMap)
            {
                map.RenderEntireMap = RenderEntireMap;
            }
            bool renderHeights = tab == Tab.Height;
            if (Floor < 0)
            {
                map.CaveGridMesh.HandlesVisible = renderHeights;
                map.CaveGridMesh.SetRenderHeightColors(renderHeights);
                map.CaveGridMesh.ApplyAllChanges();
            }
            else
            {
                map.SurfaceGridMesh.HandlesVisible = renderHeights;
                map.SurfaceGridMesh.SetRenderHeightColors(renderHeights);
                map.SurfaceGridMesh.ApplyAllChanges();
            }
        }
        
        private void PrepareProjector()
        {
            if (!CurrentRaycast.collider)
            {
                return;
            }
            
            GameObject hitObject = CurrentRaycast.collider.gameObject;
            bool gridOrGroundHit = hitObject.GetComponent<GroundMesh>() || hitObject.GetComponent<OverlayMesh>();
            if (!gridOrGroundHit)
            {
                return;
            }

            TileSelectionMode tileSelectionMode = LayoutManager.Instance.TileSelectionMode;
            Vector3 raycastPosition = CurrentRaycast.point;
            TileSelectionHit tileSelectionHit = TileSelection.PositionToTileSelectionHit(raycastPosition, tileSelectionMode);
            TileSelectionTarget target = tileSelectionHit.Target;

            if (target == TileSelectionTarget.Nothing)
            {
                return;
            }

            attachedProjector.gameObject.SetActive(true);

            int tileX = tileSelectionHit.X;
            int tileY = tileSelectionHit.Y;

            const float borderThickness = TileSelection.BorderThickness;

            switch (target)
            {
                case TileSelectionTarget.Tile:
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4 + 2);
                    attachedProjector.orthographicSize = 2;
                    attachedProjector.aspectRatio = 1;
                    break;
                case TileSelectionTarget.InnerTile:
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4 + 2);
                    attachedProjector.orthographicSize = 2 - borderThickness * 4;
                    attachedProjector.aspectRatio = 1;
                    break;
                case TileSelectionTarget.BottomBorder:
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4);
                    attachedProjector.orthographicSize = borderThickness * 4;
                    attachedProjector.aspectRatio = 2f / (borderThickness * 4) - (borderThickness * 6);
                    break;
                case TileSelectionTarget.LeftBorder:
                    attachedProjector.transform.position = new Vector3(tileX * 4, 10000, tileY * 4 + 2);
                    attachedProjector.orthographicSize = 2 - (borderThickness * 4);
                    attachedProjector.aspectRatio = (borderThickness * 4) / 1.5f;
                    break;
                case TileSelectionTarget.Corner:
                    attachedProjector.transform.position = new Vector3(tileX * 4, 10000, tileY * 4);
                    attachedProjector.orthographicSize = borderThickness * 4;
                    attachedProjector.aspectRatio = 1;
                    break;
            }
        }

        private void UpdateState()
        {
            CameraController.UpdateState(AttachedCamera, AttachedCamera.transform, parentTransform);
        }

        private void OnRenderObject()
        {
            Camera[] waterCameras = ultraQualityWater.GetComponentsInChildren<Camera>();
            bool currentWaterCamera = waterCameras.Contains(Camera.current);
            bool currentAttachedCamera = Camera.current == AttachedCamera;
            if (!currentWaterCamera && !currentAttachedCamera || !CurrentRaycast.collider)
            {
                return;
            }
            
            GameObject hitObject = CurrentRaycast.collider.gameObject;
            GroundMesh groundMesh = hitObject.GetComponent<GroundMesh>();
            OverlayMesh overlayMesh = hitObject.GetComponent<OverlayMesh>();
            HeightmapHandle heightmapHandle = GameManager.Instance.Map.SurfaceGridMesh.RaycastHandles();

            bool gridOrGroundHit = groundMesh || overlayMesh || heightmapHandle != null;

            if (!gridOrGroundHit)
            {
                GL.PushMatrix();
                GraphicsManager.Instance.SimpleDrawingMaterial.SetPass(0);
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

            if (hitCollider.GetType() == typeof(MeshCollider))
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
                GL.Begin(GL.TRIANGLES);
                GL.Color(pickerColor);
                for (int i = 0; i < triangles.Length; i += 3)
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

        private void OnPostRender()
        {
            attachedProjector.gameObject.SetActive(false);
            if (Properties.Instance.WaterQuality == Gui.WaterQuality.Ultra)
            {
                ultraQualityWater.gameObject.SetActive(false);
            }
        }
    }
}