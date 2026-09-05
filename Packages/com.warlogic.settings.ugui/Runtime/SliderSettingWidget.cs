using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlogic.Settings.Ugui
{
    public sealed class SliderSettingWidget : MonoBehaviour, ISettingWidget
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text descriptionText;

        private Func<float> _read;
        private Action<float> _write;
        private bool _wholeNumbers;

        public event Action ValueEdited;

        public RectTransform Root => (RectTransform) transform;

        public void Bind(ISetting<float> setting, float min, float max)
        {
            BindCommon(setting.Label, setting.Description, min, max, false,
                () => setting.ApplyMode == ApplyMode.OnSave ? setting.StagedValue : setting.Value,
                v => setting.Value = v);
        }

        public void Bind(ISetting<int> setting, int min, int max)
        {
            BindCommon(setting.Label, setting.Description, min, max, true,
                () => setting.ApplyMode == ApplyMode.OnSave ? setting.StagedValue : setting.Value,
                v => setting.Value = Mathf.RoundToInt(v));
        }

        private void BindCommon(string label, string description, float min, float max, bool wholeNumbers, Func<float> read, Action<float> write)
        {
            _read = read;
            _write = write;
            _wholeNumbers = wholeNumbers;
            labelText.text = label;
            SetupDescription(description);
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.onValueChanged.AddListener(OnSliderChanged);
            Refresh();
        }

        public void Refresh()
        {
            if (_read == null)
            {
                return;
            }
            float value = _read();
            slider.SetValueWithoutNotify(value);
            valueText.text = _wholeNumbers ? Mathf.RoundToInt(value).ToString() : value.ToString("0.##");
        }

        private void OnSliderChanged(float value)
        {
            if (_wholeNumbers)
            {
                value = Mathf.RoundToInt(value);
            }
            valueText.text = _wholeNumbers ? ((int) value).ToString() : value.ToString("0.##");
            _write(value);
            ValueEdited?.Invoke();
        }

        private void SetupDescription(string description)
        {
            bool hasDescription = !string.IsNullOrEmpty(description);
            descriptionText.gameObject.SetActive(hasDescription);
            if (hasDescription)
            {
                descriptionText.text = description;
            }
        }
    }
}
