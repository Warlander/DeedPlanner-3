using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Settings;
using Warlander.ExtensionUtils;
using Zenject;

namespace Warlander.Deedplanner.Logic.Cameras
{
    public class IsoCameraController : ICameraController
    {
        [Inject] private DPSettings _settings;
        [Inject] private DPInput _input;
        
        public GridMaterialType GridMaterialToUse => GridMaterialType.Uniform;
        
        private Vector2 isoPosition;
        private Vector2 velocity;
        private float isoScale = 40;

        private int rotation = 45;

        public bool SupportsMode(CameraMode mode)
        {
            return mode == CameraMode.Isometric;
        }

        public void UpdateDrag(Camera attachedCamera, PointerEventData eventData)
        {
            float factor = isoScale * Mathf.Pow(attachedCamera.scaledPixelHeight, -1f) * 2f;
            isoPosition += new Vector2(-eventData.delta.x * factor, -eventData.delta.y * factor);
        }

        public void UpdateInput(Map map, CameraMode mode, Vector3 focusedPoint, float aspect, int currentFloor, bool focusedWindow, bool mouseOver)
        {
            if (focusedWindow)
            {
                bool boostPressed = _input.MapInput2D.Boost.IsPressed();
                bool altBoostPressed = _input.MapInput2D.AltBoost.IsPressed();
                
                if (mouseOver && !boostPressed && !altBoostPressed)
                {
                    float scroll = _input.MapInput2D.ZoomInOut.ReadValue<float>();
                    if (scroll > 0 && isoScale > 10)
                    {
                        isoScale -= 4;
                    }
                    else if (scroll < 0)
                    {
                        isoScale += 4;
                    }
                }

                Vector2 movement = _input.MapInput2D.MoveMap.ReadValue<Vector2>();
                velocity = Vector2.Lerp(velocity, movement, Time.deltaTime * 10);
                float scaleMultiplier = isoScale / 10f;
                float velocityMultiplier = Time.deltaTime * _settings.IsoMovementSpeed * scaleMultiplier;
                
                if (boostPressed)
                {
                    velocityMultiplier *= _settings.ShiftSpeedModifier;
                }

                if (altBoostPressed)
                {
                    velocityMultiplier *= _settings.ControlSpeedModifier;
                }
                
                isoPosition += velocity * velocityMultiplier;

            }
            if (mouseOver)
            {
                if (_input.MapInput2D.TurnCameraClockwise.triggered) {
                    float posX = isoPosition.x;
                    float posY = isoPosition.y;

                    rotation += 90;
                    if (rotation > 360) {
                        rotation = 45;
                    }

                    isoPosition.x = -(posY * 2);
                    isoPosition.y = (posX / 2);
                }
                else if (_input.MapInput2D.TurnCameraCounterclockwise.triggered) {
                    float posX = isoPosition.x;
                    float posY = isoPosition.y;

                    rotation -= 90;
                     if (rotation < 0) {
                        rotation = 360 - 45;
                    }

                    isoPosition.x = (posY * 2);
                    isoPosition.y = -(posX / 2);
                }
            }


            /*
             * The following code to limit drag zone to always keep map in view does not work with the new rotation.
             * It is kept here in the source, in case someone has the urge to tackle it later.
             * Uncomment, and you can see that limits work in default orientation, but not when rotated.
             */
            // float padding = 4f;

            // if (isoPosition.x < -(map.Height * 4 / Mathf.Sqrt(2) - isoScale * aspect + padding))
            // {
            //     isoPosition.x = -(map.Height * 4 / Mathf.Sqrt(2) - isoScale * aspect + padding);
            // }
            // if (isoPosition.y < isoScale - padding)
            // {
            //     isoPosition.y = isoScale - padding;
            // }

            // if (isoPosition.x > map.Width * 4 / Mathf.Sqrt(2) - isoScale * aspect + padding)
            // {
            //     isoPosition.x = map.Width * 4 / Mathf.Sqrt(2) - isoScale * aspect + padding;
            // }
            // if (map.Height >= map.Width)
            // {
            //     if (isoPosition.y > map.Height * 4 / Mathf.Sqrt(2) - isoScale + padding)
            //     {
            //         isoPosition.y = map.Height * 4 / Mathf.Sqrt(2) - isoScale + padding;
            //     }
            // }
            // else if (isoPosition.y > map.Width * 4 / Mathf.Sqrt(2) - isoScale + padding) {
            //     isoPosition.y = map.Width * 4 / Mathf.Sqrt(2) - isoScale + padding;
            // }

            // bool fitsHorizontally = map.Width * 2 * Mathf.Sqrt(2) < isoScale * aspect;
            // bool fitsVertically = map.Height * 2 / Mathf.Sqrt(2) < isoScale;

            // if (fitsHorizontally)
            // {
            //     isoPosition.x = 0;
            // }
            // if (fitsVertically)
            // {
            //     isoPosition.y = map.Height * 2 / Mathf.Sqrt(2);
            // }
        }

        public void UpdateState(MultiCamera camera, Transform cameraTransform)
        {
            Quaternion rotationQuaternion = Quaternion.Euler(30, rotation, 0);

            camera.AttachedCamera.clearFlags = CameraClearFlags.SolidColor;
            camera.AttachedCamera.orthographic = true;
            camera.AttachedCamera.orthographicSize = isoScale;
            cameraTransform.localPosition = rotationQuaternion * new Vector3(isoPosition.x, isoPosition.y, -10000);
            cameraTransform.localRotation = rotationQuaternion;
        }

        public Vector2 CalculateWaterTablePosition(Vector3 cameraPosition)
        {
            return new Vector2(isoPosition.x, isoPosition.y);
        }
        
        public float CalculateGridAlphaMultiplier()
        {
            float scaleReversed = 1 / isoScale;
            return Mathf.Min(scaleReversed * 20, 1);
        }
    }
}
