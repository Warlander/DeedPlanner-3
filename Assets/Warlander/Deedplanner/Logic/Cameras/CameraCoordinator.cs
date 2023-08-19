using System;
using UnityEngine;
using Zenject;

namespace Warlander.Deedplanner.Logic.Cameras
{
    public class CameraCoordinator : MonoBehaviour
    {
        [Inject] private GameManager _gameManager;
        
        [SerializeField] private MultiCamera[] _cameras;

        public event Action CurrentCameraChanged;
        public event Action FloorChanged;
        public event Action ModeChanged;
        
        public MultiCamera Current => _cameras[_activeCamera];
        public MultiCamera Hovered => GetCurrentlyHoveredCamera();
        public int ActiveId => _activeCamera;

        private int _activeCamera = 0;

        private void Awake()
        {
            if (_gameManager.Map != null)
            {
                ChangeCurrentCamera(0);
            }
            
            foreach (MultiCamera cam in _cameras)
            {
                cam.FloorChanged += CameraOnFloorChanged;
                cam.ModeChanged += CameraOnModeChanged;
                cam.PointerDown += CameraOnPointerDown;
            }
            
            _gameManager.MapInitialized += GameManagerOnMapInitialized;
        }

        private void GameManagerOnMapInitialized()
        {
            if (_activeCamera == -1)
            {
                ChangeCurrentCamera(0);
            }
        }

        private void CameraOnModeChanged()
        {
            ModeChanged?.Invoke();
        }

        private void CameraOnPointerDown(MultiCamera cam)
        {
            ChangeCurrentCamera(cam.ScreenId);
        }

        private void CameraOnFloorChanged()
        {
            FloorChanged?.Invoke();
        }

        public void ChangeCurrentCamera(int newCamera)
        {
            if (_activeCamera == newCamera)
            {
                return;
            }
            
            _activeCamera = newCamera;
            CurrentCameraChanged?.Invoke();
            FloorChanged?.Invoke();
        }

        public void ToggleCamera(int cameraId, bool render)
        {
            _cameras[cameraId].gameObject.SetActive(render);
        }
        
        private MultiCamera GetCurrentlyHoveredCamera()
        {
            foreach (MultiCamera cam in _cameras)
            {
                if (cam.MouseOver)
                {
                    return cam;
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            foreach (MultiCamera cam in _cameras)
            {
                cam.FloorChanged -= CameraOnFloorChanged;
                cam.ModeChanged -= CameraOnModeChanged;
                cam.PointerDown -= CameraOnPointerDown;
            }
            
            _gameManager.MapInitialized -= GameManagerOnMapInitialized;
        }
    }
}