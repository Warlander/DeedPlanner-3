using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlogic.Settings.Ugui
{
    public sealed class SettingsTabButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Toggle toggle;
        [SerializeField] private Outline activeOutline;

        public event Action Clicked;

        public RectTransform Root => (RectTransform) transform;

        private bool _selected;

        private void Awake()
        {
            toggle.onValueChanged.AddListener(OnToggleChanged);
            RefreshOutline();
        }

        public void SetLabel(string label)
        {
            labelText.text = label;
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            toggle.SetIsOnWithoutNotify(selected);
            RefreshOutline();
        }

        private void OnToggleChanged(bool value)
        {
            if (value)
            {
                Clicked?.Invoke();
            }
            else if (_selected)
            {
                toggle.SetIsOnWithoutNotify(true);
            }
            RefreshOutline();
        }

        private void RefreshOutline()
        {
            if (activeOutline != null)
            {
                activeOutline.enabled = toggle.isOn;
            }
        }
    }
}
