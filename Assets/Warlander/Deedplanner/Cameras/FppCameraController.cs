using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Ui;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Settings;
using Warlander.ExtensionUtils;
using VContainer;
using Warlander.Deedplanner.Caves;

namespace Warlander.Deedplanner.Cameras
{
    public class FppCameraController : ICameraController, IDisposable
    {
        private readonly CameraSettings _settings;
        private readonly DPInput _input;
        private Map _caveMap;
        private CaveWalkSurface _caveWalkSurface;

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

        public void UpdateDrag(Camera attachedCamera, PointerEventData eventData)
        {
            fppRotation += new Vector3(-eventData.delta.y * _settings.FppMouseSensitivity, eventData.delta.x * _settings.FppMouseSensitivity, 0);
            fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);
        }

        public void UpdateInput(Map map, CameraMode mode, Vector3 focusedPoint, float aspect, int currentLevel, bool focusedWindow, bool mouseOver)
        {
            bool caveWalking = mode == CameraMode.Wurmian && currentLevel < 0;
            if (caveWalking)
            {
                ObserveCaveMap(map);
                if (!_caveWalkSurface.TryPlace(ref fppPosition))
                {
                    return;
                }
            }

            Vector3 previousPosition = fppPosition;
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
                if (caveWalking)
                {
                    fppPosition = _caveWalkSurface.Move(previousPosition, fppPosition);
                    return;
                }
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

        private void ObserveCaveMap(Map map)
        {
            if (_caveMap == map)
            {
                return;
            }
            Dispose();
            _caveMap = map;
            _caveWalkSurface = new CaveWalkSurface(map.Caves, map.Width, map.Height,
                (x, y) => map[x, y].GetHeightForLevel(-1));
            map.CaveEditor.Changed += OnCaveChanged;
            map.CommandManager.Mutated += OnSupportChanged;
        }

        private void OnCaveChanged(CaveDirtyRegion region)
        {
            _caveWalkSurface.Invalidate();
        }

        private void OnSupportChanged()
        {
            _caveWalkSurface.Invalidate();
        }

        public void Dispose()
        {
            if (_caveMap != null)
            {
                _caveMap.CaveEditor.Changed -= OnCaveChanged;
                _caveMap.CommandManager.Mutated -= OnSupportChanged;
                _caveMap = null;
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
