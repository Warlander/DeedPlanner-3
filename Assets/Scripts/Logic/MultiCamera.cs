using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    [RequireComponent(typeof(Camera))]
    public class MultiCamera : MonoBehaviour
    {

        private static Material lineDrawingMaterial;

        private Transform parentTransform;
        private Camera attachedCamera;
        [SerializeField]
        private int screenId = 0;
        [SerializeField]
        private GameObject screen = null;
        [SerializeField]
        private CameraMode cameraMode = CameraMode.Top;
        [SerializeField]
        private int floor = 0;

        private Vector3 fppPosition = new Vector3(-3, 4, -3);
        private Vector3 fppRotation = new Vector3(15, 45, 0);
        private float wurmianHeight = 1.4f;

        private Vector2 topPosition;
        private float topScale = 40;

        private Vector2 isoPosition;
        private float isoScale = 40;

        private bool mouseOver = false;

        public RaycastHit CurrentRaycast { get; private set; }

        public CameraMode CameraMode {
            get {
                return cameraMode;
            }
            set {
                cameraMode = value;
                UpdateState();
            }
        }

        public int Floor {
            get {
                return floor;
            }
            set {
                floor = value;
                UpdateState();
            }
        }

        void Start()
        {
            if (lineDrawingMaterial == null)
            {
                Shader shader = Shader.Find("DeedPlanner/SimpleLineShader");
                lineDrawingMaterial = new Material(shader);
            }

            parentTransform = transform.parent;
            attachedCamera = GetComponent<Camera>();

            MouseEventCatcher eventCatcher = screen.GetComponent<MouseEventCatcher>();

            eventCatcher.OnDragEvent.AddListener(data =>
            {
                if (CameraMode == CameraMode.Perspective || CameraMode == CameraMode.Wurmian)
                {
                    fppRotation += new Vector3(-data.delta.y * Properties.FppMouseSensitivity, data.delta.x * Properties.FppMouseSensitivity, 0);
                }
                else if (CameraMode == CameraMode.Top && Input.GetMouseButton(2))
                {
                    topPosition += new Vector2(-data.delta.x * Properties.TopMouseSensitivity, -data.delta.y * Properties.TopMouseSensitivity);
                }
                else if (CameraMode == CameraMode.Isometric && Input.GetMouseButton(2))
                {
                    isoPosition += new Vector2(-data.delta.x * Properties.IsoMouseSensitivity, -data.delta.y * Properties.IsoMouseSensitivity);
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
                mouseOver = true;
            });

            eventCatcher.OnPointerExitEvent.AddListener(data =>
            {
                mouseOver = false;
            });

            CameraMode = cameraMode;
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
            Map map = GameManager.Instance.Map;
            map.RenderedFloor = Floor;
            CurrentRaycast = default;

            if (mouseOver)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(screen.GetComponent<RectTransform>(), Input.mousePosition, null, out local);
                local /= screen.GetComponent<RectTransform>().sizeDelta;
                local += new Vector2(0.5f, 0.5f);
                Ray ray = attachedCamera.ViewportPointToRay(local);
                RaycastHit raycastHit;
                int mask = LayerMasks.GetMaskForTab(LayoutManager.Instance.CurrentTab);
                bool hit = Physics.Raycast(ray, out raycastHit, 20000, mask);
                if (hit)
                {
                    CurrentRaycast = raycastHit;
                }
            }
        }

        private void UpdatePerspectiveCamera(Map map)
        {
            int activeWindow = LayoutManager.Instance.ActiveWindow;
            if (activeWindow == screenId)
            {
                Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                movement *= Properties.FppMovementSpeed * Time.deltaTime;
                attachedCamera.transform.localPosition = fppPosition;
                attachedCamera.transform.Translate(movement, Space.Self);
                fppPosition = attachedCamera.transform.position;
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

                float h00 = map[currentTileX, currentTileY].GetTileForFloor(floor).Height * 0.1f;
                float h10 = map[currentTileX + 1, currentTileY].GetTileForFloor(floor).Height * 0.1f;
                float h01 = map[currentTileX, currentTileY + 1].GetTileForFloor(floor).Height * 0.1f;
                float h11 = map[currentTileX + 1, currentTileY + 1].GetTileForFloor(floor).Height * 0.1f;

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
                if (mouseOver)
                {
                    float scroll = Input.mouseScrollDelta.y;
                    if (scroll > 0)
                    {
                        topScale -= 4;
                    }
                    else if (scroll < 0)
                    {
                        topScale += 4;
                    }
                }

                Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                movement *= Properties.TopMovementSpeed * Time.deltaTime;
                topPosition += movement;
            }

            if (topPosition.x < topScale * attachedCamera.aspect)
            {
                topPosition.x = topScale * attachedCamera.aspect;
            }
            if (topPosition.y < topScale)
            {
                topPosition.y = topScale;
            }

            if (topPosition.x > map.Width * 4 - topScale * attachedCamera.aspect)
            {
                topPosition.x = map.Width * 4 - topScale * attachedCamera.aspect;
            }
            if (topPosition.y > map.Height * 4 - topScale)
            {
                topPosition.y = map.Height * 4 - topScale;
            }

            bool fitsHorizontally = map.Width * 2 < topScale * attachedCamera.aspect;
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
                if (mouseOver)
                {
                    float scroll = Input.mouseScrollDelta.y;
                    if (scroll > 0)
                    {
                        isoScale -= 4;
                    }
                    else if (scroll < 0)
                    {
                        isoScale += 4;
                    }
                }

                Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                movement *= Properties.IsoMovementSpeed * Time.deltaTime;
                isoPosition += movement;
            }

            if (isoPosition.x < -(map.Width * 4 / Mathf.Sqrt(2) - isoScale * attachedCamera.aspect))
            {
                isoPosition.x = -(map.Width * 4 / Mathf.Sqrt(2) - isoScale * attachedCamera.aspect);
            }
            if (isoPosition.y < isoScale)
            {
                isoPosition.y = isoScale;
            }

            if (isoPosition.x > map.Width * 4 / Mathf.Sqrt(2) - isoScale * attachedCamera.aspect)
            {
                isoPosition.x = map.Width * 4 / Mathf.Sqrt(2) - isoScale * attachedCamera.aspect;
            }
            if (isoPosition.y > map.Height * 4 / Mathf.Sqrt(2) - isoScale)
            {
                isoPosition.y = map.Height * 4 / Mathf.Sqrt(2) - isoScale;
            }

            bool fitsHorizontally = map.Width * 2 * Mathf.Sqrt(2) < isoScale * attachedCamera.aspect;
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
            if (CameraMode == CameraMode.Perspective || CameraMode == CameraMode.Wurmian)
            {
                attachedCamera.orthographic = false;
                attachedCamera.transform.localPosition = fppPosition;
                attachedCamera.transform.localRotation = Quaternion.Euler(fppRotation);
                parentTransform.localRotation = Quaternion.identity;
            }
            else if (cameraMode == CameraMode.Top)
            {
                attachedCamera.orthographic = true;
                attachedCamera.orthographicSize = topScale;
                attachedCamera.transform.localPosition = new Vector3(topPosition.x, 10000, topPosition.y);
                attachedCamera.transform.localRotation = Quaternion.Euler(90, 0, 0);
                parentTransform.localRotation = Quaternion.identity;
            }
            else if (cameraMode == CameraMode.Isometric)
            {
                attachedCamera.orthographic = true;
                attachedCamera.orthographicSize = isoScale;
                attachedCamera.transform.localPosition = new Vector3(isoPosition.x, isoPosition.y, -10000);
                attachedCamera.transform.localRotation = Quaternion.identity;
                parentTransform.localRotation = Quaternion.Euler(30, 45, 0);
            }
        }

        private void OnPostRender()
        {
            GameObject hitObject = CurrentRaycast.collider?.gameObject;
            bool gridOrGroundHit = hitObject != null && (hitObject.GetComponent<Ground>() || hitObject.GetComponent<GridTile>());

            GL.PushMatrix();
            lineDrawingMaterial.SetPass(0);
            RenderLines();
            if (hitObject != null)
            {
                if (!gridOrGroundHit)
                {
                    RenderRaytrace();
                }
                else
                {
                    RenderGridSquare();
                }
            }
            GL.PopMatrix();
        }

        private void RenderLines()
        {
            bool renderHeights = LayoutManager.Instance.CurrentTab == Tab.Height;
            bool cave = floor < 0;
            int absoluteFloor = cave ? -floor + 1 : floor;
            Tab currentTab = LayoutManager.Instance.CurrentTab;
            if (currentTab == Tab.Ground || currentTab == Tab.Height || currentTab == Tab.Borders)
            {
                absoluteFloor = 0;
            }

            Map map = GameManager.Instance.Map;
            float highestHeight = cave ? map.HighestCaveHeight : map.HighestSurfaceHeight;
            float lowestHeight = cave ? map.LowestCaveHeight : map.LowestSurfaceHeight;
            float heightDelta = highestHeight - lowestHeight;

            float linesAlpha = 0.75f;

            GL.Begin(GL.LINES);
            GL.Color(new Color(1, 1, 1, linesAlpha));
            for (int i = 0; i < map.Width; i++)
            {
                for (int i2 = 0; i2 < map.Height; i2++)
                {
                    float cornerHeight = map[i, i2].GetTileForFloor(floor).Height;
                    float cornerColorComponent = (cornerHeight - lowestHeight) / heightDelta;

                    float verticalHeight = map[i, i2 + 1].GetTileForFloor(floor).Height;
                    if (renderHeights)
                    {
                        GL.Color(new Color(cornerColorComponent, 1f - cornerColorComponent, 0, linesAlpha));
                    }
                    GL.Vertex3(i * 4, absoluteFloor * 3 + map[i, i2].GetTileForFloor(floor).Height * 0.1f, i2 * 4);
                    if (renderHeights)
                    {
                        float verticalColorComponent = (verticalHeight - lowestHeight) / heightDelta;
                        GL.Color(new Color(verticalColorComponent, 1f - verticalColorComponent, 0, linesAlpha));
                    }
                    GL.Vertex3(i * 4, absoluteFloor * 3 + map[i, i2 + 1].GetTileForFloor(floor).Height * 0.1f, i2 * 4 + 4);

                    float horizontalHeight = map[i + 1, i2].GetTileForFloor(floor).Height;
                    if (renderHeights)
                    {
                        GL.Color(new Color(cornerColorComponent, 1f - cornerColorComponent, 0, linesAlpha));
                    }
                    GL.Vertex3(i * 4, absoluteFloor * 3 + map[i, i2].GetTileForFloor(floor).Height * 0.1f, i2 * 4);
                    if (renderHeights)
                    {
                        float horizontalColorComponent = (horizontalHeight - lowestHeight) / heightDelta;
                        GL.Color(new Color(horizontalColorComponent, 1f - horizontalColorComponent, 0, linesAlpha));
                    }
                    GL.Vertex3(i * 4 + 4, absoluteFloor * 3 + map[i + 1, i2].GetTileForFloor(floor).Height * 0.1f, i2 * 4);
                }
            }
            GL.End();
        }

        private void RenderRaytrace()
        {
            float raytraceAlpha = 0.25f;
            Collider hitCollider = CurrentRaycast.collider;
            if (hitCollider != null && hitCollider.GetType() == typeof(MeshCollider))
            {
                Transform hitTransform = CurrentRaycast.transform;
                Vector3 position = hitTransform.position;
                MeshCollider collider = (MeshCollider)hitCollider;
                Mesh mesh = collider.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                GL.Begin(GL.TRIANGLES);
                GL.Color(new Color(1, 1, 0, raytraceAlpha));
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    GL.Vertex(position + vertices[triangles[i]]);
                    GL.Vertex(position + vertices[triangles[i + 1]]);
                    GL.Vertex(position + vertices[triangles[i + 2]]);
                }
                GL.End();
            }
        }

        private void RenderGridSquare()
        {
            TileSelectionMode tileSelectionMode = LayoutManager.Instance.TileSelectionMode;
            if (tileSelectionMode == TileSelectionMode.Nothing)
            {
                return;
            }

            Vector3 raycastPosition = CurrentRaycast.point;
            TileSelectionHit tileSelectionHit = TileSelection.PositionToTileSelectionHit(raycastPosition, tileSelectionMode);
            TileSelectionTarget target = tileSelectionHit.Target;
            int tileX = tileSelectionHit.X;
            int tileY = tileSelectionHit.Y;
            Map map = GameManager.Instance.Map;

            GameObject hitObject = CurrentRaycast.collider?.gameObject;
            bool groundHit = hitObject != null && hitObject.GetComponent<Ground>();
            bool cave = floor < 0;
            int absoluteFloor = cave ? -floor + 1 : floor;
            Tab currentTab = LayoutManager.Instance.CurrentTab;
            if (currentTab == Tab.Ground || currentTab == Tab.Height || currentTab == Tab.Borders || groundHit)
            {
                absoluteFloor = 0;
            }

            float raytraceAlpha = 0.25f;
            Collider hitCollider = CurrentRaycast.collider;

            float borderThickness = TileSelection.BorderThickness;

            GL.Begin(GL.QUADS);
            GL.Color(new Color(1, 1, 0, raytraceAlpha));

            float floorHeight = absoluteFloor * 3 + 0.01f;
            if (target == TileSelectionTarget.Tile)
            {
                float xStart = tileX * 4;
                float xEnd = tileX * 4 + 4;
                float yStart = tileY * 4;
                float yEnd = tileY * 4 + 4;
                GL.Vertex3(xEnd, floorHeight + map.GetInterpolatedHeight(tileX + 1, tileY, floor) * 0.1f, yStart);
                GL.Vertex3(xEnd, floorHeight + map.GetInterpolatedHeight(tileX + 1, tileY + 1, floor) * 0.1f, yEnd);
                GL.Vertex3(xStart, floorHeight + map.GetInterpolatedHeight(tileX, tileY + 1, floor) * 0.1f, yEnd);
                GL.Vertex3(xStart, floorHeight + map.GetInterpolatedHeight(tileX, tileY, floor) * 0.1f, yStart);
            }
            else if (target == TileSelectionTarget.InnerTile)
            {
                float xStart = tileX * 4 + borderThickness * 4f;
                float xEnd = tileX * 4 + 4 - borderThickness * 4f;
                float yStart = tileY * 4 + borderThickness * 4f;
                float yEnd = tileY * 4 + 4 - borderThickness * 4f;
                GL.Vertex3(xEnd, floorHeight + map.GetInterpolatedHeight(tileX + 1 - borderThickness, tileY + borderThickness, floor) * 0.1f, yStart);
                GL.Vertex3(xEnd, floorHeight + map.GetInterpolatedHeight(tileX + 1 - borderThickness, tileY + 1 - borderThickness, floor) * 0.1f, yEnd);
                GL.Vertex3(xStart, floorHeight + map.GetInterpolatedHeight(tileX + borderThickness, tileY + 1 - borderThickness, floor) * 0.1f, yEnd);
                GL.Vertex3(xStart, floorHeight + map.GetInterpolatedHeight(tileX + borderThickness, tileY + borderThickness, floor) * 0.1f, yStart);
            }

            GL.End();
        }

    }
}