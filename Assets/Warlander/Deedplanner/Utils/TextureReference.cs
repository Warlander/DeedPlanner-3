using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    public class TextureReference
    {

        private static Dictionary<string, TextureReference> references = new Dictionary<string, TextureReference>();

        public static TextureReference GetTextureReference(string location)
        {
            TextureReference reference;

            if (references.ContainsKey(location))
            {
                reference = references[location];
                return reference;
            }

            reference = new TextureReference(location);
            references[location] = reference;
            return reference;
        }

        public static TextureReference GetTextureReference(XmlElement element)
        {
            string location = element.GetAttribute("location");
            return GetTextureReference(location);
        }

        private Texture2D texture;
        private Sprite sprite;
        private Material material;

        public string Location { get; private set; }

        public Texture2D Texture {
            get {
                if (texture)
                {
                    return texture;
                }

                texture = WomModelLoader.LoadTexture(Application.streamingAssetsPath + "/" + Location);
                return texture;
            }
        }

        public Sprite Sprite {
            get {
                if (sprite)
                {
                    return sprite;
                }

                sprite = Sprite.Create(Texture, new Rect(0.0f, 0.0f, Texture.width, Texture.height), new Vector2(0.5f, 0.5f));
                return sprite;
            }
        }

        public Material Material {
            get {
                if (material)
                {
                    return material;
                }

                material = new Material(Shader.Find("Standard"));
                material.SetTexture("_MainTex", Texture);
                material.SetFloat("_Glossiness", 0);
                return material;
            }
        }

        private TextureReference(string location)
        {
            Location = location;
        }

    }
}
