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

        private Camera attachedCamera;
        [SerializeField]
        private int screenId;
        [SerializeField]
        private GameObject screen;
        [SerializeField]
        private CameraMode cameraMode = CameraMode.Top;
        [SerializeField]
        private int floor = 0;

        private Vector3 fppPosition = new Vector3(-3, 4, -3);
        private Vector3 fppRotation = new Vector3(15, 45, 0);

        private Vector2 topPosition;
        private float topScale = 40;

        private Vector2 isometricPosition;
        private float isometricScale = 40;

        private bool mouseOver = false;

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
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                lineDrawingMaterial = new Material(shader);
                lineDrawingMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineDrawingMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            attachedCamera = GetComponent<Camera>();

            MouseEventCatcher eventCatcher = screen.GetComponent<MouseEventCatcher>();

            eventCatcher.OnDragEvent.AddListener(data =>
            {
                if (CameraMode == CameraMode.Perspective)
                {
                    fppRotation += new Vector3(-data.delta.y * Properties.FppMouseSensitivity, data.delta.x * Properties.FppMouseSensitivity, 0);
                    attachedCamera.transform.localRotation = Quaternion.Euler(fppRotation);
                }
                else if (CameraMode == CameraMode.Top)
                {
                    topPosition += new Vector2(-data.delta.x * Properties.TopMouseSensitivity, -data.delta.y * Properties.TopMouseSensitivity);
                    attachedCamera.transform.localPosition = new Vector3(topPosition.x, 10000, topPosition.y);
                }
            });

            eventCatcher.OnBeginDragEvent.AddListener(data =>
            {
                Cursor.visible = false;
            });

            eventCatcher.OnEndDragEvent.AddListener(data =>
            {
                Cursor.visible = true;
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

        void Update()
        {
            if (mouseOver)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(screen.GetComponent<RectTransform>(), Input.mousePosition, null, out local);
                local /= screen.GetComponent<RectTransform>().sizeDelta;
                local += new Vector2(0.5f, 0.5f);
                Ray ray = attachedCamera.ViewportPointToRay(local);
                RaycastHit raycastHit;
                bool hit = Physics.Raycast(ray, out raycastHit, 20000);
                if (hit)
                {
                    Debug.Log("Hit something!");
                }
            }

            int activeWindow = LayoutManager.Instance.ActiveWindow;
            if (activeWindow != screenId)
            {
                return;
            }

            if (CameraMode == CameraMode.Perspective)
            {
                Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                movement *= Properties.FppMovementSpeed * Time.deltaTime;
                attachedCamera.transform.localPosition = fppPosition;
                attachedCamera.transform.Translate(movement, Space.Self);
                fppPosition = attachedCamera.transform.position;
            }
        }

        private void UpdateState()
        {
            if (CameraMode == CameraMode.Perspective)
            {
                attachedCamera.orthographic = false;
                attachedCamera.transform.position = fppPosition;
                attachedCamera.transform.rotation = Quaternion.Euler(fppRotation);
            }
            else if (cameraMode == CameraMode.Top)
            {
                attachedCamera.orthographic = true;
                attachedCamera.orthographicSize = topScale;
                attachedCamera.transform.position = new Vector3(topPosition.x, 10000, topPosition.y);
                attachedCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            }
        }

        private void OnPostRender()
        {
            Map map = GameManager.Instance.Map;

            GL.PushMatrix();
            lineDrawingMaterial.SetPass(0);
            lineDrawingMaterial.SetVector("_Color", new Color(1, 1, 1, 0.5f));
            GL.Begin(GL.LINES);
            for (int i = 0; i < map.Width; i++)
            {
                for (int i2 = 0; i2 < map.Height; i2++)
                {
                    GL.Vertex3(i * 4, 1, i2 * 4);
                    GL.Vertex3(i * 4, 1, i2 * 4 + 4);
                    GL.Vertex3(i * 4, 1, i2 * 4);
                    GL.Vertex3(i * 4 + 4, 1, i2 * 4);
                }
            }
            GL.End();
            GL.PopMatrix();
        }

    }
}