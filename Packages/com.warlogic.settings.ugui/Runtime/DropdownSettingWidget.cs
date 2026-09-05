using System;
using TMPro;
using UnityEngine;

namespace Warlogic.Settings.Ugui
{
    public sealed class DropdownSettingWidget : MonoBehaviour, ISettingWidget
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Dropdown dropdown;
        [SerializeField] private TMP_Text descriptionText;

        private Func<int> _readIndex;
        private Action<int> _writeIndex;

        public event Action ValueEdited;

        public RectTransform Root => (RectTransform) transform;

        public void BindEnum(ISetting setting, Type enumType)
        {
            labelText.text = setting.Label;
            bool hasDescription = !string.IsNullOrEmpty(setting.Description);
            descriptionText.gameObject.SetActive(hasDescription);
            if (hasDescription)
            {
                descriptionText.text = setting.Description;
            }

            System.Reflection.PropertyInfo valueProp = setting.GetType().GetProperty("Value");
            System.Reflection.PropertyInfo stagedProp = setting.GetType().GetProperty("StagedValue");
            var values = new System.Collections.Generic.List<object>();
            foreach (object value in Enum.GetValues(enumType))
            {
                if (!values.Contains(value))
                {
                    values.Add(value);
                }
            }
            var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            foreach (object value in values)
            {
                options.Add(new TMP_Dropdown.OptionData(value.ToString()));
            }
            dropdown.options = options;

            _readIndex = () =>
            {
                object current = setting.ApplyMode == ApplyMode.OnSave ? stagedProp.GetValue(setting) : valueProp.GetValue(setting);
                return values.IndexOf(current);
            };
            _writeIndex = index => valueProp.SetValue(setting, values[index]);

            dropdown.onValueChanged.AddListener(OnDropdownChanged);
            Refresh();
        }

        public void Refresh()
        {
            if (_readIndex == null)
            {
                return;
            }
            dropdown.SetValueWithoutNotify(Mathf.Max(0, _readIndex()));
        }

        private void OnDropdownChanged(int index)
        {
            _writeIndex(index);
            ValueEdited?.Invoke();
        }
    }
}
