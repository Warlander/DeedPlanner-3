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

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        private static Dictionary<string, TextureReference> references = new Dictionary<string, TextureReference>();

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
        private readonly List<Action<Texture2D>> textureWaitingCallbacks = new List<Action<Texture2D>>();
        private readonly List<Action<Sprite>> spriteWaitingCallbacks = new List<Action<Sprite>>();

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
            
            textureWaitingCallbacks.Add(callback);

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
            
            spriteWaitingCallbacks.Add(callback);
            
            if (!textureLoading)
            {
                textureLoading = true;
                yield return WurmAssetsLoader.LoadTexture(Application.streamingAssetsPath + "/" + Location, false, OnTextureLoaded);
            }
        }

        private void OnTextureLoaded(Texture2D tex)
        {
            textureLoading = false;

            if (tex)
            {
                texture = tex;
                texture.name = Location;
                
                foreach (Action<Texture2D> textureWaitingCallback in textureWaitingCallbacks)
                {
                    textureWaitingCallback.Invoke(texture);
                }
                textureWaitingCallbacks.Clear();

                if (spriteWaitingCallbacks.Count > 0)
                {
                    sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }

                foreach (Action<Sprite> spriteWaitingCallback in spriteWaitingCallbacks)
                {
                    spriteWaitingCallback.Invoke(sprite);
                }
                spriteWaitingCallbacks.Clear();
            }
        }

    }
}
