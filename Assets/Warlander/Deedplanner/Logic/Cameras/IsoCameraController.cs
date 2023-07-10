using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic.Cameras
{
    public class IsoCameraController : ICameraController
    {
        private Vector2 isoPosition;
        private float isoScale = 40;

        private int rotation = 45;

        public bool SupportsMode(CameraMode mode)
        {
            return mode == CameraMode.Isometric;
        }

        public void UpdateDrag(PointerEventData eventData)
        {
            float factor = (isoScale * 0.0028f);
            isoPosition += new Vector2(-eventData.delta.x * factor, -eventData.delta.y * factor);
            Debug.Log(isoPosition.x);
            Debug.Log(isoPosition.y);
        }

        public void UpdateInput(Map map, CameraMode mode, Vector3 focusedPoint, float aspect, int currentFloor, bool focusedWindow, bool mouseOver)
        {
            if (focusedWindow)
            {
                if (mouseOver && Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift))
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
                if (Input.GetKey(KeyCode.LeftShift)) {
                    movement *= (Properties.Instance.IsoMovementSpeed * 4) * Time.deltaTime;
                }
                else {
                    movement *= Properties.Instance.IsoMovementSpeed * Time.deltaTime;
                }
                isoPosition += movement;

            }
            if (mouseOver)
            {
                if (Input.GetKeyDown(KeyCode.PageUp)) {
                    float posX = isoPosition.x;
                    float posY = isoPosition.y;

                    rotation += 90;
                    if (rotation > 360) {
                        rotation = 45;
                    }

                    isoPosition.x = -(posY * 2);
                    isoPosition.y = (posX / 2);
                }
                else if (Input.GetKeyDown(KeyCode.PageDown)) {
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

        public void UpdateState(Camera camera, Transform cameraTransform, Transform cameraParentTransform)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.orthographic = true;
            camera.orthographicSize = isoScale;
            cameraTransform.localPosition = new Vector3(isoPosition.x, isoPosition.y, -10000);
            cameraTransform.localRotation = Quaternion.identity;
            cameraParentTransform.localRotation = Quaternion.Euler(30, rotation, 0);
        }

        public Vector2 CalculateWaterTablePosition(Vector3 cameraPosition)
        {
            return new Vector2(isoPosition.x, isoPosition.y);
        }
    }
}
