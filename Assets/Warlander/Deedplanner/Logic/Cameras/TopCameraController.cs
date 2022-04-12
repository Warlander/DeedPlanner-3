using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic.Cameras
{
    public class TopCameraController : ICameraController
    {
        private Vector2 topPosition;
        private float topScale = 40;

        public bool SupportsMode(CameraMode mode)
        {
            return mode == CameraMode.Top;
        }

        public void UpdateDrag(PointerEventData eventData)
        {
            float factor = (topScale * 0.0031f);
            topPosition += new Vector2(-eventData.delta.x * factor, -eventData.delta.y * factor);
        }

        public void UpdateInput(Map map, CameraMode mode, Vector3 focusedPoint, float aspect, int currentFloor, bool focusedWindow, bool mouseOver)
        {
            if (focusedWindow)
            {
                if (mouseOver)
                {
                    Vector2 topPoint = new Vector2(focusedPoint.x, focusedPoint.z);

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

            if (topPosition.x < topScale * aspect)
            {
                topPosition.x = topScale * aspect;
            }
            if (topPosition.y < topScale)
            {
                topPosition.y = topScale;
            }

            if (topPosition.x > map.Width * 4 - topScale * aspect)
            {
                topPosition.x = map.Width * 4 - topScale * aspect;
            }
            if (topPosition.y > map.Height * 4 - topScale)
            {
                topPosition.y = map.Height * 4 - topScale;
            }

            bool fitsHorizontally = map.Width * 2 < topScale * aspect;
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

        public void UpdateState(Camera camera, Transform cameraTransform, Transform cameraParentTransform)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.orthographic = true;
            camera.orthographicSize = topScale;
            cameraTransform.localPosition = new Vector3(topPosition.x, 10000, topPosition.y);
            cameraTransform.localRotation = Quaternion.Euler(90, 0, 0);
            cameraParentTransform.localRotation = Quaternion.identity;
        }

        public Vector2 CalculateWaterTablePosition(Vector3 cameraPosition)
        {
            return new Vector2(cameraPosition.x, cameraPosition.z);
        }
    }
}
