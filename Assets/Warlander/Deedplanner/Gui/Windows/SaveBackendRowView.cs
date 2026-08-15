using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class SaveBackendRowView : MonoBehaviour
    {
        private static readonly Color SelectedColor = new Color(0.2f, 0.35f, 0.55f);
        private static readonly Color NormalColor = new Color(0.19f, 0.2f, 0.23f);

        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descText;
        [SerializeField] private Image _background;
        [SerializeField] private Button _button;

        public event Action Clicked;

        private void Awake()
        {
            _button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void SetData(string backendName, string description)
        {
            _nameText.text = backendName;
            _descText.text = description;
        }

        public void SetSelected(bool selected)
        {
            _background.color = selected ? SelectedColor : NormalColor;
        }
    }
}
