using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Settings;
using Zenject;

namespace Warlander.Deedplanner.Logic.Cameras
{
    public class TopCameraController : ICameraController
    {
        [Inject] private DPSettings _settings;

        public GridMaterialType GridMaterialToUse => GridMaterialType.Uniform;
        
        private Vector2 topPosition;
        private float topScale = 40;

        public bool SupportsMode(CameraMode mode)
        {
            return mode == CameraMode.Top;
        }

        public void UpdateDrag(Camera attachedCamera, PointerEventData eventData)
        {
            float factor = topScale * Mathf.Pow(attachedCamera.scaledPixelHeight, -1f) * 2f;

            topPosition += new Vector2(-eventData.delta.x * factor, -eventData.delta.y * factor);
        }

        public void UpdateInput(Map map, CameraMode mode, Vector3 focusedPoint, float aspect, int currentFloor, bool focusedWindow, bool mouseOver)
        {
            if (focusedWindow)
            {
                if (mouseOver && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift))
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
                movement *= _settings.TopMovementSpeed * Time.deltaTime;
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

        public void UpdateState(MultiCamera camera, Transform cameraTransform)
        {
            camera.AttachedCamera.clearFlags = CameraClearFlags.SolidColor;
            camera.AttachedCamera.orthographic = true;
            camera.AttachedCamera.orthographicSize = topScale;
            cameraTransform.localPosition = new Vector3(topPosition.x, 10000, topPosition.y);
            cameraTransform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        public Vector2 CalculateWaterTablePosition(Vector3 cameraPosition)
        {
            return new Vector2(cameraPosition.x, cameraPosition.z);
        }

        public float CalculateGridAlphaMultiplier()
        {
            float scaleReversed = 1 / topScale;
            return Mathf.Min(scaleReversed * 20, 1);
        }
    }
}
