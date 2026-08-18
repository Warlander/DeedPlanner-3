using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.UI.Utils;
using VContainer;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeSegmentItem : MonoBehaviour
    {
        [Inject] private TooltipHandler _tooltipHandler;

        [SerializeField] private Button _button;
        [SerializeField] private Image _bridgePartImage;
        [SerializeField] private PointerOverDetector _pointerOverDetector;

        public event Action Clicked;

        public bool IsPointerOver => _pointerOverDetector.IsPointerOver;

        private string _tooltipText;
        private Sprite _partSprite;
        private Sprite _pavingSprite;
        private bool _mirrored;

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
            _pavingSprite = null;
            _mirrored = bridgePart.Mirrored;

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

        // Paving sprite replaces the part sprite; null restores it.
        public void SetPaving(Sprite pavingSprite, string tooltipText)
        {
            _pavingSprite = pavingSprite;
            _tooltipText = tooltipText;
            ApplySprite();
        }

        private void ApplySprite()
        {
            if (_pavingSprite)
            {
                _bridgePartImage.sprite = _pavingSprite;
                transform.localScale = Vector3.one;
            }
            else if (_partSprite)
            {
                _bridgePartImage.sprite = _partSprite;
                int mirroredImageScale = _mirrored ? -1 : 1;
                transform.localScale = new Vector3(mirroredImageScale, 1, 1);
            }
        }

        public void SetPreview(TextureReference sprite, BridgePartType partType)
        {
            _tooltipText = partType.ToHumanFriendlyName();
            _pavingSprite = null;
            _mirrored = false;

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
            _pavingSprite = null;
            _partSprite = incorrectSprite;
            ApplySprite();
        }

        public void SetClickable(bool clickable)
        {
            _button.interactable = clickable;
        }
    }
}
