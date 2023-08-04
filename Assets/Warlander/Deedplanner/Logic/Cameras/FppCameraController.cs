using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Settings;
using Zenject;

namespace Warlander.Deedplanner.Logic.Cameras
{
    public class FppCameraController : ICameraController
    {
        [Inject] private DPSettings _settings;
        
        private Vector3 fppPosition = new Vector3(-3, 4, -3);
        private Vector3 fppRotation = new Vector3(15, 45, 0);
        private const float WurmianHeight = 1.4f;

        public bool SupportsMode(CameraMode mode)
        {
            return mode == CameraMode.Perspective || mode == CameraMode.Wurmian;
        }

        public void UpdateDrag(Camera attachedCamera, PointerEventData eventData)
        {
            fppRotation += new Vector3(-eventData.delta.y * _settings.FppMouseSensitivity, eventData.delta.x * _settings.FppMouseSensitivity, 0);
            fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);
        }

        public void UpdateInput(Map map, CameraMode mode, Vector3 focusedPoint, float aspect, int currentFloor, bool focusedWindow, bool mouseOver)
        {
            if (focusedWindow)
            {
                float movementMultiplier = 1;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    movementMultiplier *= _settings.FppShiftModifier;
                }
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    movementMultiplier *= _settings.FppControlModifier;
                }

                Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                movement *= _settings.FppMovementSpeed * Time.deltaTime * movementMultiplier;

                if (Input.GetKey(KeyCode.Q))
                {
                    fppRotation += new Vector3(0, -Time.deltaTime * _settings.FppKeyboardRotationSensitivity, 0);
                    fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);
                }
                if (Input.GetKey(KeyCode.E))
                {
                    fppRotation += new Vector3(0, Time.deltaTime * _settings.FppKeyboardRotationSensitivity, 0);
                    fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);
                }

                if (Input.GetKey(KeyCode.R))
                {
                    fppPosition += new Vector3(0, Time.deltaTime * _settings.FppMovementSpeed * movementMultiplier, 0);
                }
                if (Input.GetKey(KeyCode.F))
                {
                    fppPosition += new Vector3(0, -Time.deltaTime * _settings.FppMovementSpeed * movementMultiplier, 0);
                }

                fppPosition += Quaternion.Euler(fppRotation) * movement;
            }

            if (mode == CameraMode.Wurmian)
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

                float h00 = map[currentTileX, currentTileY].GetHeightForFloor(currentFloor) * 0.1f;
                float h10 = map[currentTileX + 1, currentTileY].GetHeightForFloor(currentFloor) * 0.1f;
                float h01 = map[currentTileX, currentTileY + 1].GetHeightForFloor(currentFloor) * 0.1f;
                float h11 = map[currentTileX + 1, currentTileY + 1].GetHeightForFloor(currentFloor) * 0.1f;

                float x0 = (h00 * xPartRev + h10 * xPart);
                float x1 = (h01 * xPartRev + h11 * xPart);

                float height = (x0 * yPartRev + x1 * yPart);
                height += WurmianHeight;
                if (height < 0.3f)
                {
                    height = 0.3f;
                }
                fppPosition.y = height;
            }
        }

        public void UpdateState(Camera camera, Transform cameraTransform, Transform cameraParentTransform)
        {
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.orthographic = false;
            cameraTransform.localPosition = fppPosition;
            cameraTransform.localRotation = Quaternion.Euler(fppRotation);
            cameraParentTransform.localRotation = Quaternion.identity;
        }

        public Vector2 CalculateWaterTablePosition(Vector3 cameraPosition)
        {
            return new Vector2(cameraPosition.x, cameraPosition.z);
        }
    }
}
