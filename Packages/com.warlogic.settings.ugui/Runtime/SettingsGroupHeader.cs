using TMPro;
using UnityEngine;

namespace Warlogic.Settings.Ugui
{
    public sealed class SettingsGroupHeader : MonoBehaviour
    {
        [SerializeField] private TMP_Text headerText;

        public RectTransform Root => (RectTransform) transform;

        public void SetLabel(string label)
        {
            headerText.text = label;
        }
    }
}
