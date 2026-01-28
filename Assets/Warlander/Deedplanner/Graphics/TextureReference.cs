using System;
using System.Collections.Generic;
using System.IO;
using R3;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class TextureReference
    {
        private readonly ITextureLoader _textureLoader;

        private Texture2D texture;
        private Sprite sprite;
        private bool textureLoading = false;

        private List<Action<Texture2D>> textureLoadRequests = null;
        private List<Action<Sprite>> spriteLoadRequests = null;

        public string Location { get; }

        public TextureReference(ITextureLoader textureLoader, string location)
        {
            _textureLoader = textureLoader;
            Location = location;
        }
        
        public void LoadOrGetTexture(Action<Texture2D> onLoaded)
        {
            if (texture)
            {
                onLoaded.Invoke(texture);
                return;
            }

            if (textureLoadRequests == null)
            {
                textureLoadRequests = new List<Action<Texture2D>>();
            }
                
            textureLoadRequests.Add(onLoaded);

            LoadTextureIfNeeded();
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
            
            if (spriteLoadRequests == null)
            {
                spriteLoadRequests = new List<Action<Sprite>>();
            }
            
            spriteLoadRequests.Add(callback);

            LoadTextureIfNeeded();
        }

        private void LoadTextureIfNeeded()
        {
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
                
                _textureLoader.LoadTexture(location, false)
                    .ToObservable()
                    .Subscribe(OnTextureLoaded);
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

            if (spriteLoadRequests != null)
            {
                sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                
                foreach (Action<Sprite> loadRequest in spriteLoadRequests)
                {
                    loadRequest(sprite);
                }
                
                spriteLoadRequests.Clear();
                spriteLoadRequests = null;
            }
        }
    }
}
