using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.UI.Utils;
using Zenject;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeSegmentItem : MonoBehaviour
    {
        [Inject] private TooltipHandler _tooltipHandler;
        
        [SerializeField] private Button _button;
        [SerializeField] private Image _bridgePartImage;
        [SerializeField] private PointerOverDetector _pointerOverDetector;

        public event Action Clicked;

        private BridgePart _shownPart;

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
            if (_pointerOverDetector.IsPointerOver)
            {
                _tooltipHandler.ShowTooltipText(_shownPart.PartType.ToHumanFriendlyName());
            }
        }

        public void Set(BridgePart bridgePart)
        {
            _shownPart = bridgePart;
            
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
    }
}