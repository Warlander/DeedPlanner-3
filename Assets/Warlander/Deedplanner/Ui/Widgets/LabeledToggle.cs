using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Ui.Widgets
{
    [RequireComponent(typeof(Toggle))]
    public class LabeledToggle : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private TextMeshProUGUI _label;

        public bool IsOn
        {
            get => _toggle.isOn;
            set => _toggle.isOn = value;
        }

        public ToggleGroup Group
        {
            get => _toggle.group;
            set => _toggle.group = value;
        }

        public string LabelText
        {
            get => _label.text;
            set => _label.text = value;
        }

        public event Action<bool> Toggled;

        private void Awake()
        {
            _toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        private void OnDestroy()
        {
            _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }

        private void OnToggleValueChanged(bool isOn)
        {
            Toggled?.Invoke(isOn);
        }
    }
}
