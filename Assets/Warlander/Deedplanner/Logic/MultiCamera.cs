using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityStandardAssets.Water;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Logic
{
    [RequireComponent(typeof(Camera))]
    public class MultiCamera : MonoBehaviour
    {

        private Transform parentTransform;
        public Camera AttachedCamera { get; private set; }
        public Vector2 MousePosition { get; private set; }
        [SerializeField]
        private int screenId = 0;
        [SerializeField]
        private GameObject screen = null;
        [SerializeField]
        private CameraMode cameraMode = CameraMode.Top;
        [SerializeField]
        private int floor = 0;

        [SerializeField]
        private Water ultraQualityWater = null;
        [SerializeField]
        private GameObject highQualityWater = null;
        [SerializeField]
        private GameObject simpleQualityWater = null;

        [SerializeField]
        private RectTransform selectionBox = null;
        [SerializeField]
        private Projector attachedProjector = null;

        private Vector3 fppPosition = new Vector3(-3, 4, -3);
        private Vector3 fppRotation = new Vector3(15, 45, 0);
        private const float wurmianHeight = 1.4f;

        private Vector2 topPosition;
        private float topScale = 40;

        private Vector2 isoPosition;
        private float isoScale = 40;

        public bool MouseOver { get; private set; } = false;

        public RaycastHit CurrentRaycast { get; private set; }

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

        public bool RenderEntireLayer => CameraMode == CameraMode.Perspective;

        public GameObject Screen => screen;

        public bool RenderSelectionBox {
            get => selectionBox.gameObject.activeSelf;
            set => selectionBox.gameObject.SetActive(value);
        }

        public Vector2 SelectionBoxPosition {
            get => selectionBox.anchoredPosition;
            set => selectionBox.anchoredPosition = value;
        }

        public Vector2 SelectionBoxSize {
            get => selectionBox.sizeDelta;
            set => selectionBox.sizeDelta = value;
        }

        void Start()
        {
            parentTransform = transform.parent;
            AttachedCamera = GetComponent<Camera>();

            MouseEventCatcher eventCatcher = screen.GetComponent<MouseEventCatcher>();

            eventCatcher.OnDragEvent.AddListener(data =>
            {
                if (CameraMode == CameraMode.Perspective || CameraMode == CameraMode.Wurmian)
                {
                    if (data.button == PointerEventData.InputButton.Middle)
                    {
                        fppRotation += new Vector3(-data.delta.y * Properties.Instance.FppMouseSensitivity, data.delta.x * Properties.Instance.FppMouseSensitivity, 0);
                        fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);
                    }
                }
                else if (CameraMode == CameraMode.Top && Input.GetMouseButton(2))
                {
                    topPosition += new Vector2(-data.delta.x * Properties.Instance.TopMouseSensitivity, -data.delta.y * Properties.Instance.TopMouseSensitivity);
                }
                else if (CameraMode == CameraMode.Isometric && Input.GetMouseButton(2))
                {
                    isoPosition += new Vector2(-data.delta.x * Properties.Instance.IsoMouseSensitivity, -data.delta.y * Properties.Instance.IsoMouseSensitivity);
                }
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

            eventCatcher.OnPointerEnterEvent.AddListener(data =>
            {
                MouseOver = true;
            });

            eventCatcher.OnPointerExitEvent.AddListener(data =>
            {
                MouseOver = false;
            });

            CameraMode = cameraMode;

            Properties.Instance.Saved += ValidateState;
            ValidateState();
        }

        private void ValidateState()
        {
            Gui.WaterQuality waterQuality = Properties.Instance.WaterQuality;
            if (waterQuality != Gui.WaterQuality.ULTRA)
            {
                ultraQualityWater.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            Map map = GameManager.Instance.Map;

            if (CameraMode == CameraMode.Perspective || CameraMode == CameraMode.Wurmian)
            {
                UpdatePerspectiveCamera(map);
            }
            else if (CameraMode == CameraMode.Top)
            {
                UpdateTopCamera(map);
            }
            else if (CameraMode == CameraMode.Isometric)
            {
                UpdateIsometricCamera(map);
            }

            UpdateState();
        }

        void OnPreCull()
        {
            PrepareProjector();
            Vector3 cameraPosition = AttachedCamera.transform.position;
            bool renderWater = RenderEntireLayer ? true : (Floor == 0 || Floor == -1);
            if (Properties.Instance.WaterQuality == Gui.WaterQuality.ULTRA)
            {
                ultraQualityWater.gameObject.SetActive(renderWater);
                if (cameraMode != CameraMode.Isometric)
                {
                    ultraQualityWater.transform.position = new Vector3(cameraPosition.x, ultraQualityWater.transform.position.y, cameraPosition.z);
                }
                else
                {
                    ultraQualityWater.transform.position = new Vector3(isoPosition.x, ultraQualityWater.transform.position.y, isoPosition.y);
                }
                ultraQualityWater.Update();
            }
            else if (Properties.Instance.WaterQuality == Gui.WaterQuality.HIGH)
            {
                highQualityWater.gameObject.SetActive(renderWater);
                if (cameraMode != CameraMode.Isometric)
                {
                    highQualityWater.transform.position = new Vector3(cameraPosition.x, ultraQualityWater.transform.position.y, cameraPosition.z);
                }
                else
                {
                    highQualityWater.transform.position = new Vector3(isoPosition.x, ultraQualityWater.transform.position.y, isoPosition.y);
                }
            }
            else if (Properties.Instance.WaterQuality == Gui.WaterQuality.SIMPLE)
            {
                simpleQualityWater.gameObject.SetActive(renderWater);
            }

            Map map = GameManager.Instance.Map;
            if (map.RenderedFloor != Floor)
            {
                map.RenderedFloor = Floor;
            }
            if (map.RenderEntireLayer != RenderEntireLayer)
            {
                map.RenderEntireLayer = RenderEntireLayer;
            }
            bool renderHeights = LayoutManager.Instance.CurrentTab == Tab.Height;
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

            CurrentRaycast = default;

            if (MouseOver)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(screen.GetComponent<RectTransform>(), Input.mousePosition, null, out local);
                MousePosition = local + (screen.GetComponent<RectTransform>().sizeDelta / 2);
                local /= screen.GetComponent<RectTransform>().sizeDelta;
                local += new Vector2(0.5f, 0.5f);
                Ray ray = AttachedCamera.ViewportPointToRay(local);
                RaycastHit raycastHit;
                int mask = LayerMasks.GetMaskForTab(LayoutManager.Instance.CurrentTab);
                bool hit = Physics.Raycast(ray, out raycastHit, 20000, mask);
                if (hit && Cursor.visible)
                {
                    CurrentRaycast = raycastHit;
                    GameObject hitObject = raycastHit.transform.gameObject;
                    TileEntity tileEntity = hitObject.GetComponent<TileEntity>();
                    GridTile gridTile = hitObject.GetComponent<GridTile>();
                    if (tileEntity)
                    {
                        LayoutManager.Instance.TooltipText = tileEntity.ToString();
                    }
                    else if (gridTile)
                    {
                        LayoutManager.Instance.TooltipText = gridTile.ToString();
                    }
                    else
                    {
                        LayoutManager.Instance.TooltipText = null;
                    }
                }
                else
                {
                    LayoutManager.Instance.TooltipText = null;
                }
            }
        }

        private void PrepareProjector()
        {
            GameObject hitObject = CurrentRaycast.collider?.gameObject;
            bool gridOrGroundHit = hitObject != null && (hitObject.GetComponent<Ground>() || hitObject.GetComponent<GridTile>());

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

            float borderThickness = TileSelection.BorderThickness;

            if (target == TileSelectionTarget.Tile)
            {
                attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4 + 2);
                attachedProjector.orthographicSize = 2;
                attachedProjector.aspectRatio = 1;
            }
            else if (target == TileSelectionTarget.InnerTile)
            {
                attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4 + 2);
                attachedProjector.orthographicSize = 2 - borderThickness * 4;
                attachedProjector.aspectRatio = 1;
            }
            else if (target == TileSelectionTarget.BottomBorder)
            {
                attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4);
                attachedProjector.orthographicSize = borderThickness * 4;
                attachedProjector.aspectRatio = 2f / (borderThickness * 4) - (borderThickness * 6);
            }
            else if (target == TileSelectionTarget.LeftBorder)
            {
                attachedProjector.transform.position = new Vector3(tileX * 4, 10000, tileY * 4 + 2);
                attachedProjector.orthographicSize = 2 - (borderThickness * 4);
                attachedProjector.aspectRatio = (borderThickness * 4) / 1.5f;
            }
            else if (target == TileSelectionTarget.Corner)
            {
                attachedProjector.transform.position = new Vector3(tileX * 4, 10000, tileY * 4);
                attachedProjector.orthographicSize = borderThickness * 4;
                attachedProjector.aspectRatio = 1;
            }
        }

        private void UpdatePerspectiveCamera(Map map)
        {
            int activeWindow = LayoutManager.Instance.ActiveWindow;
            if (activeWindow == screenId)
            {
                Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                movement *= Properties.Instance.FppMovementSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    movement *= Properties.Instance.FppShiftModifier;
                }
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    movement *= Properties.Instance.FppControlModifier;
                }

                if (Input.GetKey(KeyCode.Q))
                {
                    fppRotation += new Vector3(0, -Time.deltaTime * Properties.Instance.FppKeyboardRotationSensitivity, 0);
                    fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);
                }
                if (Input.GetKey(KeyCode.E))
                {
                    fppRotation += new Vector3(0, Time.deltaTime * Properties.Instance.FppKeyboardRotationSensitivity, 0);
                    fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);
                }

                if (Input.GetKey(KeyCode.R))
                {
                    fppPosition += new Vector3(0, Time.deltaTime * Properties.Instance.FppMovementSpeed, 0);
                }
                if (Input.GetKey(KeyCode.F))
                {
                    fppPosition += new Vector3(0, -Time.deltaTime * Properties.Instance.FppMovementSpeed, 0);
                }

                AttachedCamera.transform.localPosition = fppPosition;
                AttachedCamera.transform.Translate(movement, Space.Self);
                fppPosition = AttachedCamera.transform.position;
            }

            if (CameraMode == CameraMode.Wurmian)
            {
                if (fppPosition.x < 0)
                {
                    fppPosition.x = 0;
                }
                if (fppPosition.z < 0)
                {
                    fppPosition.z = 0;
                }
                if (fppPosition.x > map.Width * 4)
                {
                    fppPosition.x = map.Width * 4;
                }
                if (fppPosition.z > map.Height * 4)
                {
                    fppPosition.z = map.Height * 4;
                }

                int currentTileX = (int) (fppPosition.x / 4f);
                int currentTileY = (int) (fppPosition.z / 4f);

                float xPart = (fppPosition.x % 4f) / 4f;
                float yPart = (fppPosition.z % 4f) / 4f;
                float xPartRev = 1f - xPart;
                float yPartRev = 1f - yPart;

                float h00 = map[currentTileX, currentTileY].GetHeightForFloor(floor) * 0.1f;
                float h10 = map[currentTileX + 1, currentTileY].GetHeightForFloor(floor) * 0.1f;
                float h01 = map[currentTileX, currentTileY + 1].GetHeightForFloor(floor) * 0.1f;
                float h11 = map[currentTileX + 1, currentTileY + 1].GetHeightForFloor(floor) * 0.1f;

                float x0 = (h00 * xPartRev + h10 * xPart);
                float x1 = (h01 * xPartRev + h11 * xPart);

                float height = (x0 * yPartRev + x1 * yPart);
                height += wurmianHeight;
                if (height < 0.3f)
                {
                    height = 0.3f;
                }
                fppPosition.y = height;
            }
        }

        private void UpdateTopCamera(Map map)
        {
            int activeWindow = LayoutManager.Instance.ActiveWindow;
            if (activeWindow == screenId)
            {
                if (MouseOver)
                {
                    Vector3 raycastPoint = CurrentRaycast.point;
                    Vector2 topPoint = new Vector2(raycastPoint.x, raycastPoint.z);
                    
                    float scroll = Input.mouseScrollDelta.y;
                    if (scroll > 0 && topScale > 10)
                    {
                        topPosition += (topPoint - topPosition) / topScale * 4;
                        topScale -= 4;
                    }
                    else if (scroll < 0)
                    {
                        topPosition -= (topPoint - topPosition) / topScale * 4;
                        topScale += 4;
                    }
                }

                Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                movement *= Properties.Instance.TopMovementSpeed * Time.deltaTime;
                topPosition += movement;
            }

            if (topPosition.x < topScale * AttachedCamera.aspect)
            {
                topPosition.x = topScale * AttachedCamera.aspect;
            }
            if (topPosition.y < topScale)
            {
                topPosition.y = topScale;
            }

            if (topPosition.x > map.Width * 4 - topScale * AttachedCamera.aspect)
            {
                topPosition.x = map.Width * 4 - topScale * AttachedCamera.aspect;
            }
            if (topPosition.y > map.Height * 4 - topScale)
            {
                topPosition.y = map.Height * 4 - topScale;
            }

            bool fitsHorizontally = map.Width * 2 < topScale * AttachedCamera.aspect;
            bool fitsVertically = map.Height * 2 < topScale;

            if (fitsHorizontally)
            {
                topPosition.x = map.Width * 2;
            }
            if (fitsVertically)
            {
                topPosition.y = map.Height * 2;
            }
        }

        private void UpdateIsometricCamera(Map map)
        {
            int activeWindow = LayoutManager.Instance.ActiveWindow;
            if (activeWindow == screenId)
            {
                if (MouseOver)
                {
                    float scroll = Input.mouseScrollDelta.y;
                    if (scroll > 0 && isoScale > 10)
                    {
                        isoScale -= 4;
                    }
                    else if (scroll < 0)
                    {
                        isoScale += 4;
                    }
                }

                Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                movement *= Properties.Instance.IsoMovementSpeed * Time.deltaTime;
                isoPosition += movement;
            }

            if (isoPosition.x < -(map.Width * 4 / Mathf.Sqrt(2) - isoScale * AttachedCamera.aspect))
            {
                isoPosition.x = -(map.Width * 4 / Mathf.Sqrt(2) - isoScale * AttachedCamera.aspect);
            }
            if (isoPosition.y < isoScale)
            {
                isoPosition.y = isoScale;
            }

            if (isoPosition.x > map.Width * 4 / Mathf.Sqrt(2) - isoScale * AttachedCamera.aspect)
            {
                isoPosition.x = map.Width * 4 / Mathf.Sqrt(2) - isoScale * AttachedCamera.aspect;
            }
            if (isoPosition.y > map.Height * 4 / Mathf.Sqrt(2) - isoScale)
            {
                isoPosition.y = map.Height * 4 / Mathf.Sqrt(2) - isoScale;
            }

            bool fitsHorizontally = map.Width * 2 * Mathf.Sqrt(2) < isoScale * AttachedCamera.aspect;
            bool fitsVertically = map.Height * 2 / Mathf.Sqrt(2) < isoScale;

            if (fitsHorizontally)
            {
                isoPosition.x = 0;
            }
            if (fitsVertically)
            {
                isoPosition.y = map.Height * 2 / Mathf.Sqrt(2);
            }
        }

        private void UpdateState()
        {
            Transform cameraTransform = AttachedCamera.transform;
            if (CameraMode == CameraMode.Perspective || CameraMode == CameraMode.Wurmian)
            {
                AttachedCamera.orthographic = false;
                cameraTransform.localPosition = fppPosition;
                cameraTransform.localRotation = Quaternion.Euler(fppRotation);
                parentTransform.localRotation = Quaternion.identity;
            }
            else if (cameraMode == CameraMode.Top)
            {
                AttachedCamera.orthographic = true;
                AttachedCamera.orthographicSize = topScale;
                cameraTransform.localPosition = new Vector3(topPosition.x, 10000, topPosition.y);
                cameraTransform.localRotation = Quaternion.Euler(90, 0, 0);
                parentTransform.localRotation = Quaternion.identity;
            }
            else if (cameraMode == CameraMode.Isometric)
            {
                AttachedCamera.orthographic = true;
                AttachedCamera.orthographicSize = isoScale;
                cameraTransform.localPosition = new Vector3(isoPosition.x, isoPosition.y, -10000);
                cameraTransform.localRotation = Quaternion.identity;
                parentTransform.localRotation = Quaternion.Euler(30, 45, 0);
            }
        }

        private void OnRenderObject()
        {
            Camera[] waterCameras = ultraQualityWater.GetComponentsInChildren<Camera>();
            bool currentWaterCamera = waterCameras.Contains(Camera.current);
            bool currentAttachedCamera = Camera.current == AttachedCamera;
            if (!currentWaterCamera && !currentAttachedCamera)
            {
                return;
            }
            
            GameObject hitObject = null;
            if (CurrentRaycast.collider)
            {
                hitObject = CurrentRaycast.collider.gameObject;
            }
            
            bool gridOrGroundHit = hitObject != null && (hitObject.GetComponent<Ground>() || hitObject.GetComponent<GridTile>() || hitObject.GetComponent<HeightmapHandle>());

            if (hitObject != null && !gridOrGroundHit)
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

            float raytraceAlpha = 0.25f;

            if (hitCollider.GetType() == typeof(MeshCollider))
            {
                MeshCollider collider = (MeshCollider)hitCollider;
                Mesh mesh = collider.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                if (normals == null || normals.Length == 0)
                {
                    normals = new Vector3[vertices.Length];
                }
                int[] triangles = mesh.triangles;
                GL.Begin(GL.TRIANGLES);
                GL.Color(new Color(1, 1, 0, raytraceAlpha));
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
                BoxCollider collider = (BoxCollider)hitCollider;
                Vector3 size = collider.size * 1.01f;
                Vector3 center = collider.center;

                Vector3 v000 = center + new Vector3(-size.x, -size.y, -size.z) / 2f;
                Vector3 v001 = center + new Vector3(-size.x, -size.y, size.z) / 2f;
                Vector3 v010 = center + new Vector3(-size.x, size.y, -size.z) / 2f;
                Vector3 v011 = center + new Vector3(-size.x, size.y, size.z) / 2f;
                Vector3 v100 = center + new Vector3(size.x, -size.y, -size.z) / 2f;
                Vector3 v101 = center + new Vector3(size.x, -size.y, size.z) / 2f;
                Vector3 v110 = center + new Vector3(size.x, size.y, -size.z) / 2f;
                Vector3 v111 = center + new Vector3(size.x, size.y, size.z) / 2f;

                GL.Begin(GL.QUADS);
                GL.Color(new Color(1, 1, 0, raytraceAlpha));
                //bottom
                GL.Vertex(v000);
                GL.Vertex(v100);
                GL.Vertex(v101);
                GL.Vertex(v001);
                //top
                GL.Vertex(v010);
                GL.Vertex(v110);
                GL.Vertex(v111);
                GL.Vertex(v011);
                //down
                GL.Vertex(v000);
                GL.Vertex(v100);
                GL.Vertex(v110);
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
                GL.Vertex(v100);
                GL.Vertex(v101);
                GL.Vertex(v111);
                GL.Vertex(v110);
                GL.End();
            }
        }

        private void OnPostRender()
        {
            attachedProjector.gameObject.SetActive(false);
            if (Properties.Instance.WaterQuality == Gui.WaterQuality.ULTRA)
            {
                ultraQualityWater.gameObject.SetActive(false);
            }
        }

    }
}