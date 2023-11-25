using System;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.UI.Utils
{
    public class SwitchObjectOnToggle : MonoBehaviour
    {
        [SerializeField] private Toggle _trackedToggle;
        [SerializeField] private SwitchObjectOnToggleState _showWhen = SwitchObjectOnToggleState.On;

        private void Awake()
        {
            _trackedToggle.onValueChanged.AddListener(TrackedToggleOnValueChanged);
            
            UpdateState();
        }

        private void TrackedToggleOnValueChanged(bool newState)
        {
            UpdateState();
        }
        
        private void UpdateState()
        {
            if (_trackedToggle)
            {
                bool reverse = _showWhen == SwitchObjectOnToggleState.Off;
                if (reverse)
                {
                    gameObject.SetActive(_trackedToggle.isOn == false);
                }
                else
                {
                    gameObject.SetActive(_trackedToggle.isOn);
                }
            }
        }

        private void OnDestroy()
        {
            if (_trackedToggle)
            {
                _trackedToggle.onValueChanged.RemoveListener(TrackedToggleOnValueChanged);
            }
        }
    }

    public enum SwitchObjectOnToggleState
    {
        On, Off
    }
}