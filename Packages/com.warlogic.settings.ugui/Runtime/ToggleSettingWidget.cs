using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlogic.Settings.Ugui
{
    public sealed class ToggleSettingWidget : MonoBehaviour, ISettingWidget
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Toggle toggle;
        [SerializeField] private TMP_Text descriptionText;

        private ISetting<bool> _setting;

        public event Action ValueEdited;

        public RectTransform Root => (RectTransform) transform;

        public void Bind(ISetting<bool> setting)
        {
            _setting = setting;
            labelText.text = setting.Label;
            bool hasDescription = !string.IsNullOrEmpty(setting.Description);
            descriptionText.gameObject.SetActive(hasDescription);
            if (hasDescription)
            {
                descriptionText.text = setting.Description;
            }
            toggle.onValueChanged.AddListener(OnToggleChanged);
            Refresh();
        }

        public void Refresh()
        {
            if (_setting == null)
            {
                return;
            }
            toggle.SetIsOnWithoutNotify(_setting.ApplyMode == ApplyMode.OnSave ? _setting.StagedValue : _setting.Value);
        }

        private void OnToggleChanged(bool value)
        {
            _setting.Value = value;
            ValueEdited?.Invoke();
        }
    }
}
