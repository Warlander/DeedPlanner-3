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

        private List<Action<Texture2D>> textureLoadRequests = null;
        private List<Action<Sprite>> spriteLoadRequests = null;

        public string Location { get; }

        private TextureReference(string location)
        {
            Location = location;
        }
        
        public void LoadOrGetTexture(Action<Texture2D> onLoaded)
        {
            if (texture)
            {
                onLoaded.Invoke(texture);
                return;
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
                WurmAssetsLoader.LoadTexture(location, false, tex =>
                {
                    OnTextureLoaded(tex);
                    onLoaded.Invoke(tex);
                });
            }
            else
            {
                if (textureLoadRequests == null)
                {
                    textureLoadRequests = new List<Action<Texture2D>>();
                }
                
                textureLoadRequests.Add(onLoaded);
            }
        }
        
        public void LoadOrGetSprite(Action<Sprite> callback)
        {
            if (sprite)
            {
                callback.Invoke(sprite);
                return;
            }

            if (texture)
            {
                sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                callback.Invoke(sprite);
                return;
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
                WurmAssetsLoader.LoadTexture(location, false, texture =>
                {
                    OnTextureLoaded(texture);
                    sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    callback.Invoke(sprite);
                    
                    if (spriteLoadRequests != null)
                    {
                        foreach (Action<Sprite> loadRequest in spriteLoadRequests)
                        {
                            loadRequest(sprite);
                        }
                
                        spriteLoadRequests.Clear();
                        spriteLoadRequests = null;
                    }
                });
            }
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

            if (textureLoadRequests != null)
            {
                foreach (Action<Texture2D> loadRequest in textureLoadRequests)
                {
                    loadRequest(texture);
                }
                
                textureLoadRequests.Clear();
                textureLoadRequests = null;
            }
        }
    }
}
