using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Gui.Widgets;

namespace Warlander.Deedplanner.Gui.Updaters
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

        public void AddGroundEntry(GroundData data, string[] category)
        {
            IconUnityListElement iconListElement = (IconUnityListElement) _groundsTree.Add(data, category);
            iconListElement.TextureReference = data.Tex2d;
        }

        public void SetLeftClickData(GroundData data)
        {
            _leftClickText.text = data.Name;
            data.Tex2d.LoadOrGetSpriteAsync().ToObservable().Subscribe(sprite => _leftClickImage.sprite = sprite);
        }

        public void SetRightClickData(GroundData data)
        {
            _rightClickText.text = data.Name;
            data.Tex2d.LoadOrGetSpriteAsync().ToObservable().Subscribe(sprite => _rightClickImage.sprite = sprite);
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
