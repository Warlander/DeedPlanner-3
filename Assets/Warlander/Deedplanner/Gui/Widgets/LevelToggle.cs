using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Gui.Widgets
{
    [RequireComponent(typeof(Toggle))]
    public class LevelToggle : MonoBehaviour
    {
        [Inject] private CameraCoordinator _cameraCoordinator;
        
        [FormerlySerializedAs("floor")] [SerializeField] private int _level = 0;

        private Toggle toggle;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            
            toggle.onValueChanged.AddListener(toggled =>
            {
                if (toggled)
                {
                    LevelChangedManually(_level);
                }

                if (toggled == false && _cameraCoordinator.Current.Level == _level)
                {
                    toggle.isOn = true;
                }
            });
        }

        private void Start()
        {
            _cameraCoordinator.CurrentCameraChanged += CameraCoordinatorOnCurrentCameraChanged;
            _cameraCoordinator.LevelChanged += CameraCoordinatorOnLevelChanged;
        }

        private void CameraCoordinatorOnLevelChanged()
        {
            bool newState = _cameraCoordinator.Current.Level == _level;

            if (newState != toggle.isOn)
            {
                toggle.isOn = newState;
            }
        }

        private void LevelChangedManually(int newLevel)
        {
            _cameraCoordinator.Current.Level = newLevel;
        }
        
        private void CameraCoordinatorOnCurrentCameraChanged()
        {
            int newLevel = _cameraCoordinator.Current.Level;
            toggle.isOn = newLevel == _level;
        }
        
        private void OnDestroy()
        {
            _cameraCoordinator.CurrentCameraChanged -= CameraCoordinatorOnCurrentCameraChanged;
            _cameraCoordinator.LevelChanged -= CameraCoordinatorOnLevelChanged;
        }
    }
}