using System;
using Warlander.Deedplanner.Persistence;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Warlander.Deedplanner.Domain;

namespace Warlander.Deedplanner.Cameras
{
    public class CameraCoordinator : MonoBehaviour
    {
        [Inject] private MapHandler _mapHandler;
        
        [SerializeField] private MultiCamera[] _cameras;

        public event Action CurrentCameraChanged;
        public event Action LevelChanged;
        public event Action ModeChanged;
        
        public MultiCamera Current => _cameras[_activeCamera];
        public MultiCamera Hovered => GetCurrentlyHoveredCamera();
        public IEnumerable<MultiCamera> Cameras => _cameras;
        public int ActiveId => _activeCamera;

        private int _activeCamera = 0;

        private void Awake()
        {
            foreach (MultiCamera cam in _cameras)
            {
                cam.LevelChanged += CameraOnLevelChanged;
                cam.ModeChanged += CameraOnModeChanged;
                cam.PointerDown += CameraOnPointerDown;
            }
            
            _mapHandler.MapInitialized += MapHandlerOnMapInitialized;
            ApplyCurrentView();
        }

        private void MapHandlerOnMapInitialized()
        {
            if (_activeCamera == -1)
            {
                ChangeCurrentCamera(0);
                return;
            }

            ApplyCurrentView();
        }

        private void CameraOnModeChanged()
        {
            ApplyCurrentView();
            ModeChanged?.Invoke();
        }

        private void CameraOnPointerDown(MultiCamera cam)
        {
            ChangeCurrentCamera(cam.ScreenId);
        }

        private void CameraOnLevelChanged()
        {
            ApplyCurrentView();
            LevelChanged?.Invoke();
        }

        public void ChangeCurrentCamera(int newCamera)
        {
            if (_activeCamera == newCamera)
            {
                return;
            }
            
            _activeCamera = newCamera;
            ApplyCurrentView();
            CurrentCameraChanged?.Invoke();
            LevelChanged?.Invoke();
        }

        public void ToggleCamera(int cameraId, bool render)
        {
            _cameras[cameraId].gameObject.SetActive(render);
        }

        private void ApplyCurrentView()
        {
            Map map = _mapHandler.Map;
            if (!map)
            {
                return;
            }

            map.SetActiveRenderView(new MapRenderView(Current.Level, Current.RenderEntireMap, map.RenderGrid));
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
                cam.LevelChanged -= CameraOnLevelChanged;
                cam.ModeChanged -= CameraOnModeChanged;
                cam.PointerDown -= CameraOnPointerDown;
            }
            
            _mapHandler.MapInitialized -= MapHandlerOnMapInitialized;
        }
    }
}
