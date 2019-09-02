using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Warlander.Deedplanner.Graphics
{
    public class IndexedTextureArray<T>
    {
        
        private readonly Dictionary<T, int> indexToSlice;
        
        public Texture2DArray TextureArray { get; private set; }
        public int Length { get; private set; }

        public IndexedTextureArray(int width, int height, int depth, TextureFormat format, bool mipmaps = true)
        {
            TextureArray = new Texture2DArray(width, height, depth, format, mipmaps);
            indexToSlice = new Dictionary<T, int>();
        }

        public int PutOrGetTexture(T key, Texture2D texture)
        {
            int index = GetTextureIndex(key);

            if (index == -1)
            {
                PutTexture(key, texture);
                index = GetTextureIndex(key);
            }

            return index;
        }
        
        public int GetTextureIndex(T key)
        {
            if (!Contains((key)))
            {
                return -1;
            }

            return indexToSlice[key];
        }
        
        public bool PutTexture(T key, Texture2D texture)
        {
            if (Length == TextureArray.depth)
            {
                return false;
            }

            try
            {
                if (texture.mipmapCount != TextureArray.mipmapCount)
                {
                    Debug.Log("Resizing texture: " + texture.name);
                    texture.Resize(TextureArray.width, TextureArray.height, TextureFormat.RGBA32, true);
                    texture.Apply();
                    texture.Compress(true);
                    texture.Apply();
                }
                
                UnityEngine.Graphics.CopyTexture(texture, 0, TextureArray, Length);
                indexToSlice[key] = Length;
                Length++;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        public bool Contains(T key)
        {
            return indexToSlice.ContainsKey(key);
        }
        
    }
}