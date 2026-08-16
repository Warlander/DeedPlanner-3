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

        private string _tooltipText;

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
            transform.localScale = Vector3.one;

            bridgePart.GetUISprite().LoadOrGetSpriteAsync().ToObservable().Subscribe(sprite =>
            {
                if (this == null)
                {
                    // Object was destroyed, do nothing.
                    return;
                }

                _bridgePartImage.sprite = sprite;
                int mirroredImageScale = bridgePart.Mirrored ? -1 : 1;
                transform.localScale = new Vector3(mirroredImageScale, 1, 1);
            });
        }

        public void SetPreview(TextureReference sprite, BridgePartType partType)
        {
            _tooltipText = partType.ToHumanFriendlyName();
            transform.localScale = Vector3.one;

            sprite.LoadOrGetSpriteAsync().ToObservable().Subscribe(loadedSprite =>
            {
                if (this == null)
                {
                    // Object was destroyed, do nothing.
                    return;
                }

                _bridgePartImage.sprite = loadedSprite;
            });
        }

        public void SetIncorrect(Sprite incorrectSprite, string tooltipText)
        {
            _tooltipText = tooltipText;
            transform.localScale = Vector3.one;
            _bridgePartImage.sprite = incorrectSprite;
        }

        public void SetClickable(bool clickable)
        {
            _button.interactable = clickable;
        }
    }
}
