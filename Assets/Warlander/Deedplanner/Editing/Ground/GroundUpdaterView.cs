using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Ui.Widgets;

namespace Warlander.Deedplanner.Editing
{
    public class GroundUpdaterView : MonoBehaviour, IGroundUpdaterView
    {
        [SerializeField] private UnityTree _groundsTree;

        [SerializeField] private Image _leftClickImage = null;
        [SerializeField] private TextMeshProUGUI _leftClickText = null;
        [SerializeField] private Image _rightClickImage = null;
        [SerializeField] private TextMeshProUGUI _rightClickText = null;

        [SerializeField] private Toggle _leftClickToggle = null;
        [SerializeField] private Toggle _pencilToggle = null;
        [SerializeField] private Toggle _fillToggle = null;
        [SerializeField] private Toggle _editCornersToggle = null;

        public event Action<GroundData> GroundSelected;
        public event Action<GroundTool> ToolChanged;
        public event Action<bool> LeftClickTargetChanged;
        public event Action<bool> EditCornersChanged;

        private void Awake()
        {
            _groundsTree.ValueChanged += OnGroundsTreeValueChanged;
            _leftClickToggle.onValueChanged.AddListener(OnLeftClickTargetChanged);
            _pencilToggle.onValueChanged.AddListener(OnPencilToggled);
            _fillToggle.onValueChanged.AddListener(OnFillToggled);
            _editCornersToggle.onValueChanged.AddListener(OnEditCornersChanged);
        }

        public void AddGroundEntry(GroundData data, string[] category, Sprite sprite)
        {
            IconUnityListElement iconListElement = (IconUnityListElement) _groundsTree.Add(data, category);
            iconListElement.Sprite = sprite;
        }

        public void SetLeftClickData(GroundData data, Sprite sprite)
        {
            _leftClickText.text = data.Name;
            _leftClickImage.sprite = sprite;
            _leftClickImage.enabled = sprite;
        }

        public void SetRightClickData(GroundData data, Sprite sprite)
        {
            _rightClickText.text = data.Name;
            _rightClickImage.sprite = sprite;
            _rightClickImage.enabled = sprite;
        }

        private void OnGroundsTreeValueChanged(object value)
        {
            GroundSelected?.Invoke(value as GroundData);
        }

        private void OnLeftClickTargetChanged(bool targeted)
        {
            LeftClickTargetChanged?.Invoke(targeted);
        }

        private void OnPencilToggled(bool toggledOn)
        {
            if (toggledOn)
            {
                ToolChanged?.Invoke(GroundTool.Pencil);
            }
        }

        private void OnFillToggled(bool toggledOn)
        {
            if (toggledOn)
            {
                ToolChanged?.Invoke(GroundTool.Fill);
            }
        }

        private void OnEditCornersChanged(bool editCorners)
        {
            EditCornersChanged?.Invoke(editCorners);
        }
    }
}
