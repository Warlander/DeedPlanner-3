using System;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Gui.Widgets
{
    [RequireComponent(typeof(Toggle))]
    public class CameraModeToggle : MonoBehaviour
    {
        [Inject] private CameraCoordinator _cameraCoordinator;
        
        [SerializeField] private CameraMode _cameraMode; 
        
        private Toggle _toggle;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            
            _toggle.onValueChanged.AddListener(toggled =>
            {
                if (toggled)
                {
                    _cameraCoordinator.Current.CameraMode = _cameraMode;
                }
                
                if (toggled == false && _cameraCoordinator.Current.CameraMode == _cameraMode)
                {
                    _toggle.isOn = true;
                }
            });
        }

        private void Start()
        {
            _cameraCoordinator.CurrentCameraChanged += CameraCoordinatorOnCurrentCameraChanged;
            _cameraCoordinator.ModeChanged += CameraCoordinatorOnModeChanged;
        }

        private void CameraCoordinatorOnModeChanged()
        {
            bool newState = _cameraCoordinator.Current.CameraMode == _cameraMode;

            if (newState != _toggle.isOn)
            {
                _toggle.isOn = newState;
            }
        }

        private void CameraCoordinatorOnCurrentCameraChanged()
        {
            CameraMode cameraMode = _cameraCoordinator.Current.CameraMode;
            _toggle.isOn = _cameraMode == cameraMode;
        }

        private void OnDestroy()
        {
            _cameraCoordinator.CurrentCameraChanged -= CameraCoordinatorOnCurrentCameraChanged;
            _cameraCoordinator.ModeChanged -= CameraCoordinatorOnModeChanged;
        }
    }
}