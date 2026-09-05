using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlogic.Settings.Ugui
{
    public sealed class KeybindSettingRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text actionLabelText;
        [SerializeField] private TMP_Text bindingText;
        [SerializeField] private Button rebindButton;

        public RectTransform Root => (RectTransform) transform;
        public Button RebindButton => rebindButton;

        public void SetLabels(string actionLabel, string binding)
        {
            actionLabelText.text = actionLabel;
            bindingText.text = binding;
        }
    }
}
