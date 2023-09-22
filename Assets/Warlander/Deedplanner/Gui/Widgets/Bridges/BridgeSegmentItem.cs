using System;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeSegmentItem : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _bridgePartImage;

        public event Action Clicked;

        private void Awake()
        {
            _button.onClick.AddListener(ButtonOnClick);
        }

        private void ButtonOnClick()
        {
            Clicked?.Invoke();
        }
        
        public void Set(TextureReference spriteReference)
        {
            spriteReference.LoadOrGetSprite(sprite =>
            {
                _bridgePartImage.sprite = sprite;
            });
        }
    }
}