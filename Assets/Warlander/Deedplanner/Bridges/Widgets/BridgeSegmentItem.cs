using Warlander.Deedplanner.Gui.Widgets;
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Bridges;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.UI.Utils;
using VContainer;

namespace Warlander.Deedplanner.Bridges.Widgets
{
    public class BridgeSegmentItem : MonoBehaviour
    {
        private static readonly Color SelectedColor = Color.white;
        private static readonly Color UnselectedColor = new Color(0.8f, 0.8f, 0.8f);
        private const float NormalSize = 32f;
        private const float SelectedSize = 40f;

        [Inject] private TooltipHandler _tooltipHandler;

        [SerializeField] private Button _button;
        [SerializeField] private Image _bridgePartImage;
        [SerializeField] private PointerOverDetector _pointerOverDetector;

        public event Action Clicked;

        public bool IsPointerOver => _pointerOverDetector.IsPointerOver;

        private string _tooltipText;
        private Sprite _partSprite;
        private bool _mirrored;
        private bool _paletteEntry;

        private void Awake()
        {
            _button.onClick.AddListener(ButtonOnClick);
        }

        private void ButtonOnClick()
        {
            Clicked?.Invoke();
        }

        private void Update()
        {
            if (_pointerOverDetector.IsPointerOver && !string.IsNullOrEmpty(_tooltipText))
            {
                _tooltipHandler.ShowTooltipText(_tooltipText);
            }
        }

        public void Set(BridgePart bridgePart, string tooltipSuffix = null)
        {
            _tooltipText = bridgePart.PartType.ToHumanFriendlyName() + tooltipSuffix;
            _mirrored = bridgePart.Mirrored;
            _paletteEntry = false;

            bridgePart.GetUISprite().LoadOrGetSpriteAsync().ToObservable().Subscribe(sprite =>
            {
                if (this == null)
                {
                    // Object was destroyed, do nothing.
                    return;
                }

                _partSprite = sprite;
                ApplySprite();
            });
        }

        // Palette entries show the pavement texture itself; the eraser passes the invalid-section X sprite.
        public void SetPaletteEntry(Sprite sprite, string tooltipText)
        {
            _tooltipText = tooltipText;
            _partSprite = sprite;
            _paletteEntry = true;
            _mirrored = false;
            ApplySprite();
        }

        public void SetSelected(bool selected)
        {
            _bridgePartImage.color = selected ? SelectedColor : UnselectedColor;
            if (_paletteEntry)
            {
                float size = selected ? SelectedSize : NormalSize;
                ((RectTransform)transform).sizeDelta = new Vector2(size, size);
            }
        }

        private void ApplySprite()
        {
            _bridgePartImage.sprite = _partSprite;
            if (!_paletteEntry)
            {
                int mirroredImageScale = _mirrored ? -1 : 1;
                transform.localScale = new Vector3(mirroredImageScale, 1, 1);
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }

        public void SetPreview(TextureReference sprite, BridgePartType partType)
        {
            _tooltipText = partType.ToHumanFriendlyName();
            _mirrored = false;
            _paletteEntry = false;

            sprite.LoadOrGetSpriteAsync().ToObservable().Subscribe(loadedSprite =>
            {
                if (this == null)
                {
                    // Object was destroyed, do nothing.
                    return;
                }

                _partSprite = loadedSprite;
                ApplySprite();
            });
        }

        public void SetIncorrect(Sprite incorrectSprite, string tooltipText)
        {
            _tooltipText = tooltipText;
            _partSprite = incorrectSprite;
            _paletteEntry = false;
            ApplySprite();
        }

        public void SetClickable(bool clickable)
        {
            _button.interactable = clickable;
        }
    }
}
