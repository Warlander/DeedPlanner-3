using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Ui.Widgets;

namespace Warlander.Deedplanner.Editing
{
    public class CaveUpdaterView : MonoBehaviour, ICaveUpdaterView
    {
        [SerializeField] private UnityTree _cavesTree;
        [SerializeField] private TextMeshProUGUI _primaryText;
        [SerializeField] private TextMeshProUGUI _secondaryText;
        [SerializeField] private Image _primaryImage;
        [SerializeField] private Image _secondaryImage;
        [SerializeField] private Toggle _primaryToggle;
        [SerializeField] private Toggle _pencilToggle;
        [SerializeField] private Toggle _fillToggle;
        [SerializeField] private Toggle _cullingToggle;

        public event Action<CaveData> CaveSelected;
        public event Action<CaveTool> ToolChanged;
        public event Action<bool> PrimaryTargetChanged;
        public event Action<bool> CullingChanged;

        private int _primaryImageRequest;
        private int _secondaryImageRequest;

        private void Awake()
        {
            _cavesTree.ValueChanged += OnCaveSelected;
            _primaryToggle.onValueChanged.AddListener(value => PrimaryTargetChanged?.Invoke(value));
            _pencilToggle.onValueChanged.AddListener(value =>
            {
                if (value) ToolChanged?.Invoke(CaveTool.Pencil);
            });
            _fillToggle.onValueChanged.AddListener(value =>
            {
                if (value) ToolChanged?.Invoke(CaveTool.Fill);
            });
            _cullingToggle.onValueChanged.AddListener(value => CullingChanged?.Invoke(value));
        }

        public void AddCaveEntry(CaveData data, string[] category)
        {
            IconUnityListElement element = (IconUnityListElement)_cavesTree.Add(data, category);
            element.TextureReference = data.Texture;
        }

        public void SetPrimaryData(CaveData data)
        {
            _primaryText.text = data.Name;
            SetPrimaryImageAsync(data, ++_primaryImageRequest);
        }

        public void SetSecondaryData(CaveData data)
        {
            _secondaryText.text = data.Name;
            SetSecondaryImageAsync(data, ++_secondaryImageRequest);
        }

        public void SetCullingEnabled(bool enabled)
        {
            _cullingToggle.SetIsOnWithoutNotify(enabled);
        }

        private void OnCaveSelected(object value)
        {
            CaveSelected?.Invoke(value as CaveData);
        }

        private async void SetPrimaryImageAsync(CaveData data, int request)
        {
            Sprite sprite = await data.Texture.LoadOrGetSpriteAsync();
            if (request != _primaryImageRequest)
            {
                return;
            }

            _primaryImage.sprite = sprite;
            _primaryImage.enabled = sprite;
        }

        private async void SetSecondaryImageAsync(CaveData data, int request)
        {
            Sprite sprite = await data.Texture.LoadOrGetSpriteAsync();
            if (request != _secondaryImageRequest)
            {
                return;
            }

            _secondaryImage.sprite = sprite;
            _secondaryImage.enabled = sprite;
        }
    }
}
