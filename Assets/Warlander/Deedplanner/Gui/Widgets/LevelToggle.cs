using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Warlander.Deedplanner.Platform.Features;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Warlogic.Features;
using VContainer;

namespace Warlander.Deedplanner.Gui.Widgets
{
    [RequireComponent(typeof(Toggle))]
    public class LevelToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const string LevelLockedTooltip = "Current editing mode works only on the ground floor";

        [Inject] private CameraCoordinator _cameraCoordinator;
        [Inject] private GroundLevelLock _groundLevelLock;
        [Inject] private TooltipHandler _tooltipHandler;
        [Inject] private IFeatureStateRetriever<Feature> _featureStateRetriever;

        [FormerlySerializedAs("floor")] [SerializeField] private int _level = 0;

        private Toggle toggle;
        private bool _hovered;

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
            _groundLevelLock.LockChanged += GroundLevelLockOnLockChanged;
            UpdateInteractable();
        }

        private void Update()
        {
            if (_hovered && ShowsLockTooltip())
            {
                _tooltipHandler.ShowTooltipText(LevelLockedTooltip);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
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

        private void GroundLevelLockOnLockChanged()
        {
            UpdateInteractable();
        }

        private void UpdateInteractable()
        {
            toggle.interactable = IsLevelAvailable();
        }

        private bool IsLevelAvailable()
        {
            if (_level < 0)
            {
                if (!_featureStateRetriever.IsFeatureEnabled(Feature.Caves))
                {
                    return false;
                }
                return !_groundLevelLock.Locked || _level == -1;
            }
            return _groundLevelLock.IsLevelAllowed(_level);
        }

        private bool ShowsLockTooltip()
        {
            return _level > 0 && !_groundLevelLock.IsLevelAllowed(_level);
        }

        private void OnDestroy()
        {
            _cameraCoordinator.CurrentCameraChanged -= CameraCoordinatorOnCurrentCameraChanged;
            _cameraCoordinator.LevelChanged -= CameraCoordinatorOnLevelChanged;
            _groundLevelLock.LockChanged -= GroundLevelLockOnLockChanged;
        }
    }
}
