using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class TextureReference
    {
        private static readonly Dictionary<string, TextureReference> references = new Dictionary<string, TextureReference>();

        public static TextureReference GetTextureReference(string location)
        {
            location = location.Replace(Application.streamingAssetsPath + "/", "");

            if (string.IsNullOrEmpty(Path.GetExtension(location)))
            {
                Debug.Log("Attempting to load invalid texture from " + location);
                return null;
            }
            
            if (references.ContainsKey(location))
            {
                return references[location];
            }

            TextureReference reference = new TextureReference(location);
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
        private bool textureLoading = false;
        private bool spriteRequested = false;

        public string Location { get; }

        private TextureReference(string location)
        {
            Location = location;
        }
        
        public IEnumerator LoadOrGetTexture(Action<Texture2D> callback)
        {
            if (texture)
            {
                callback.Invoke(texture);
                yield break;
            }

            if (!textureLoading)
            {
                textureLoading = true;
                string location;
                if (Path.IsPathRooted(Location))
                {
                    location = Location;
                }
                else
                {
                    location = Application.streamingAssetsPath + "/" + Location;
                }
                yield return WurmAssetsLoader.LoadTexture(location, false, OnTextureLoaded);
            }

            while (textureLoading)
            {
                yield return null;
            }
            
            callback.Invoke(texture);
        }
        
        public IEnumerator LoadOrGetSprite(Action<Sprite> callback)
        {
            if (sprite)
            {
                callback.Invoke(sprite);
                yield break;
            }

            if (texture)
            {
                sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                callback.Invoke(sprite);
                yield break;
            }

            if (!textureLoading)
            {
                textureLoading = true;
                spriteRequested = true;
                string location;
                if (Path.IsPathRooted(Location))
                {
                    location = Location;
                }
                else
                {
                    location = Application.streamingAssetsPath + "/" + Location;
                }
                yield return WurmAssetsLoader.LoadTexture(location, false, OnTextureLoaded);
                sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
            
            while (textureLoading)
            {
                yield return null;
            }
            
            callback.Invoke(sprite);
        }

        private void OnTextureLoaded(Texture2D tex)
        {
            if (!tex)
            {
                textureLoading = false;
                return;
            }

            texture = tex;
            texture.name = Location;

            if (spriteRequested)
            {
                sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
            
            textureLoading = false;
        }
    }
}
