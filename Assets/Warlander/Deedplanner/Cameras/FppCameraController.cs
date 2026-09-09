using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Ui;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Settings;
using Warlander.ExtensionUtils;
using VContainer;

namespace Warlander.Deedplanner.Cameras
{
    public class FppCameraController : ICameraController
    {
        private readonly CameraSettings _settings;
        private readonly DPInput _input;

        public FppCameraController(CameraSettings settings, DPInput input)
        {
            _settings = settings;
            _input = input;
        }

        public GridMaterialType GridMaterialToUse => GridMaterialType.ProximityBased;
        
        private Vector3 fppPosition = new Vector3(-3, 4, -3);
        private Vector3 fppRotation = new Vector3(15, 45, 0);
        private const float WurmianHeight = 1.4f;

        public bool SupportsMode(CameraMode mode)
        {
            return mode == CameraMode.Perspective || mode == CameraMode.Wurmian;
        }

        public void OnLevelChanged(Map map, int previousLevel, int currentLevel)
        {
            if (previousLevel == currentLevel)
            {
                return;
            }

            if (currentLevel < 0)
            {
                bool movedToOpenCell = MoveToNearestOpenCaveCellIfNeeded(map);
                float floorHeight = SampleLevelHeight(map, fppPosition, currentLevel);
                float clearance = SampleCaveClearance(map, fppPosition);
                float maximumOffset = Mathf.Max(0.3f, clearance - 0.3f);
                float offset = movedToOpenCell
                    ? Mathf.Min(WurmianHeight, maximumOffset)
                    : Mathf.Clamp(fppPosition.y - SampleLevelHeight(map, fppPosition, previousLevel),
                        0.3f, maximumOffset);
                fppPosition.y = floorHeight + offset;
                return;
            }

            float previousHeight = SampleLevelHeight(map, fppPosition, previousLevel);
            float currentHeight = SampleLevelHeight(map, fppPosition, currentLevel);
            fppPosition.y += currentHeight - previousHeight;
        }

        public void UpdateDrag(Camera attachedCamera, PointerEventData eventData)
        {
            fppRotation += new Vector3(-eventData.delta.y * _settings.FppMouseSensitivity, eventData.delta.x * _settings.FppMouseSensitivity, 0);
            fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);
        }

        public void UpdateInput(Map map, CameraMode mode, Vector3 focusedPoint, float aspect, int currentLevel, bool focusedWindow, bool mouseOver)
        {
            if (focusedWindow)
            {
                float movementMultiplier = 1;
                if (_input.MapInputShared.Boost.IsPressed())
                {
                    movementMultiplier *= _settings.ShiftSpeedModifier;
                }
                else if (_input.MapInputShared.AltBoost.IsPressed())
                {
                    movementMultiplier *= _settings.ControlSpeedModifier;
                }

                Vector2 movementInput = _input.MapInput3D.MoveMap.ReadValue<Vector2>();
                Vector3 movement = movementInput.ToVector3XZ();
                movement *= _settings.FppMovementSpeed * Time.deltaTime * movementMultiplier;

                float sidewaysRotation = _input.MapInput3D.RotateSideways.ReadValue<float>();
                fppRotation = fppRotation.AddY(Time.deltaTime * _settings.FppKeyboardRotationSensitivity * sidewaysRotation);
                fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);

                float verticalMovement = _input.MapInput3D.MoveVertically.ReadValue<float>();
                fppPosition = fppPosition.AddY(Time.deltaTime * _settings.FppMovementSpeed * movementMultiplier * verticalMovement);

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

                float height = SampleLevelHeight(map, fppPosition, currentLevel) + WurmianHeight;
                if (height < 0.3f)
                {
                    height = 0.3f;
                }
                fppPosition.y = height;
            }
        }

        private static float SampleLevelHeight(Map map, Vector3 position, int level)
        {
            float tileX = Mathf.Clamp(position.x / 4f, 0f, map.Width);
            float tileY = Mathf.Clamp(position.z / 4f, 0f, map.Height);
            int x0 = Mathf.Min(Mathf.FloorToInt(tileX), map.Width - 1);
            int y0 = Mathf.Min(Mathf.FloorToInt(tileY), map.Height - 1);
            float xPart = tileX - x0;
            float yPart = tileY - y0;

            float h00 = map[x0, y0].GetHeightForLevel(level) * 0.1f;
            float h10 = map[x0 + 1, y0].GetHeightForLevel(level) * 0.1f;
            float h01 = map[x0, y0 + 1].GetHeightForLevel(level) * 0.1f;
            float h11 = map[x0 + 1, y0 + 1].GetHeightForLevel(level) * 0.1f;
            float south = Mathf.Lerp(h00, h10, xPart);
            float north = Mathf.Lerp(h01, h11, xPart);
            return Mathf.Lerp(south, north, yPart);
        }

        private bool MoveToNearestOpenCaveCellIfNeeded(Map map)
        {
            int currentX = Mathf.FloorToInt(fppPosition.x / 4f);
            int currentY = Mathf.FloorToInt(fppPosition.z / 4f);
            if (currentX >= 0 && currentY >= 0 && currentX < map.Width && currentY < map.Height
                && !map[currentX, currentY].Cave.IsSolid)
            {
                return false;
            }

            int nearestX = -1;
            int nearestY = -1;
            float nearestDistance = float.MaxValue;
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (map[x, y].Cave.IsSolid)
                    {
                        continue;
                    }

                    float worldX = x * 4f + 2f;
                    float worldZ = y * 4f + 2f;
                    float distance = (worldX - fppPosition.x) * (worldX - fppPosition.x)
                        + (worldZ - fppPosition.z) * (worldZ - fppPosition.z);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestX = x;
                        nearestY = y;
                    }
                }
            }

            if (nearestX < 0)
            {
                return false;
            }

            fppPosition.x = nearestX * 4f + 2f;
            fppPosition.z = nearestY * 4f + 2f;
            return true;
        }

        private static float SampleCaveClearance(Map map, Vector3 position)
        {
            float tileX = Mathf.Clamp(position.x / 4f, 0f, map.Width);
            float tileY = Mathf.Clamp(position.z / 4f, 0f, map.Height);
            int x0 = Mathf.Min(Mathf.FloorToInt(tileX), map.Width - 1);
            int y0 = Mathf.Min(Mathf.FloorToInt(tileY), map.Height - 1);
            float xPart = tileX - x0;
            float yPart = tileY - y0;

            float south = Mathf.Lerp(map[x0, y0].Cave.Clearance, map[x0 + 1, y0].Cave.Clearance, xPart);
            float north = Mathf.Lerp(map[x0, y0 + 1].Cave.Clearance,
                map[x0 + 1, y0 + 1].Cave.Clearance, xPart);
            return Mathf.Lerp(south, north, yPart) * 0.1f;
        }

        public void UpdateState(MultiCamera camera, Transform cameraTransform)
        {
            camera.AttachedCamera.clearFlags = CameraClearFlags.Skybox;
            camera.AttachedCamera.orthographic = false;
            cameraTransform.localPosition = fppPosition;
            cameraTransform.localRotation = Quaternion.Euler(fppRotation);
        }

        public Vector2 CalculateWaterTablePosition(Vector3 cameraPosition)
        {
            return new Vector2(cameraPosition.x, cameraPosition.z);
        }
        
        public float CalculateGridAlphaMultiplier()
        {
            return 1;
        }
    }
}
