using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Gui.Widgets
{
    public class IconUnityListElement : UnityListElement
    {

        [SerializeField] private TextMeshProUGUI text = null;
        [SerializeField] private Toggle toggle = null;
        [SerializeField] private Image image = null;

        private TextureReference textureReference;
        private object value;

        public override object Value {
            get => value;
            set {
                this.value = value;
                text.SetText(value.ToString());
            }
        }

        public TextureReference TextureReference {
            get => TextureReference;
            set {
                textureReference = value;
                if (gameObject.activeInHierarchy && image.sprite == null)
                {
                    LoadSprite();
                }
            }
        }

        public override Toggle Toggle => toggle;

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
