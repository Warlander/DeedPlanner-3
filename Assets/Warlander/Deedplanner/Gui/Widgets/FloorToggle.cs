using System;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Gui.Widgets
{
    [RequireComponent(typeof(Toggle))]
    public class FloorToggle : MonoBehaviour
    {
        [Inject] private CameraCoordinator _cameraCoordinator;
        
        [SerializeField] private int floor = 0;

        private Toggle toggle;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            
            toggle.onValueChanged.AddListener(toggled =>
            {
                if (toggled)
                {
                    FloorChangedManually(floor);
                }

                if (toggled == false && _cameraCoordinator.Current.Floor == floor)
                {
                    toggle.isOn = true;
                }
            });
        }

        private void Start()
        {
            _cameraCoordinator.CurrentCameraChanged += CameraCoordinatorOnCurrentCameraChanged;
            _cameraCoordinator.FloorChanged += CameraCoordinatorOnFloorChanged;
        }

        private void CameraCoordinatorOnFloorChanged()
        {
            bool newState = _cameraCoordinator.Current.Floor == floor;

            if (newState != toggle.isOn)
            {
                toggle.isOn = newState;
            }
        }

        private void FloorChangedManually(int newFloor)
        {
            _cameraCoordinator.Current.Floor = newFloor;
        }
        
        private void CameraCoordinatorOnCurrentCameraChanged()
        {
            int newFloor = _cameraCoordinator.Current.Floor;
            toggle.isOn = newFloor == floor;
        }
        
        private void OnDestroy()
        {
            _cameraCoordinator.CurrentCameraChanged -= CameraCoordinatorOnCurrentCameraChanged;
            _cameraCoordinator.FloorChanged -= CameraCoordinatorOnFloorChanged;
        }
    }
}