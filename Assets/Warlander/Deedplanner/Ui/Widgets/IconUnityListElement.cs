using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Rendering.Assets;

namespace Warlander.Deedplanner.Ui.Widgets
{
    public class IconUnityListElement : UnityListElement, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {

        [SerializeField] private TextMeshProUGUI text = null;
        [SerializeField] private Toggle toggle = null;
        [SerializeField] private Image image = null;
        [SerializeField] private LayoutElement _rowLayout = null;
        [SerializeField] private CanvasGroup _selectionGroup = null;
        [SerializeField] private Image _focusOutline = null;

        public event Action<string> TooltipRequested;

        private TextureReference textureReference;
        private object value;
        private float _layoutWidth = -1;
        private bool _hovered;
        private bool _focused;

        public override object Value {
            get => value;
            set {
                this.value = value;
                text.SetText(value.ToString());
                _layoutWidth = -1;
            }
        }

        public TextureReference TextureReference {
            get => textureReference;
            set {
                textureReference = value;
                if (gameObject.activeInHierarchy && image.sprite == null)
                {
                    LoadSprite();
                }
            }
        }

        public Sprite Sprite
        {
            set
            {
                textureReference = null;
                image.sprite = value;
                image.enabled = value;
            }
        }

        public override Toggle Toggle => toggle;

        private void OnEnable()
        {
            _layoutWidth = -1;
            if (_selectionGroup != null)
            {
                _selectionGroup.alpha = toggle.isOn ? 1 : 0;
            }
        }

        private void OnDisable()
        {
            _hovered = false;
            _focused = false;
            if (_focusOutline != null)
            {
                _focusOutline.enabled = false;
            }
        }

        private void LateUpdate()
        {
            float width = text.rectTransform.rect.width;
            if (width > 0 && !Mathf.Approximately(width, _layoutWidth))
            {
                _layoutWidth = width;
                bool twoLines = text.GetPreferredValues(text.text, width, float.PositiveInfinity).y > 24;
                text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, twoLines ? 44 : 24);
                _rowLayout.minHeight = twoLines ? 50 : 30;
                _rowLayout.preferredHeight = _rowLayout.minHeight;
            }

            _selectionGroup.alpha = toggle.isOn ? 1 : 0;
            if ((_hovered || _focused) && text.isTextTruncated)
            {
                TooltipRequested?.Invoke(text.text);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            _focused = true;
            _focusOutline.enabled = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _focused = false;
            _focusOutline.enabled = false;
        }

        public void Start()
        {
            if (image.sprite == null)
            {
                LoadSprite();
            }
        }

        private void LoadSprite()
        {
            if (textureReference != null)
            {
                textureReference.LoadOrGetSpriteAsync().ToObservable().Subscribe(sprite => image.sprite = sprite);
            }
        }

    }
}
