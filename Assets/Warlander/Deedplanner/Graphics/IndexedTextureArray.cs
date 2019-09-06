using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class IndexedTextureArray<T>
    {
        
        private readonly Dictionary<T, int> indexToSlice;
        
        public Texture2DArray TextureArray { get; private set; }
        public int MaxLength => TextureArray.depth;
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
                if (texture.mipmapCount == TextureArray.mipmapCount)
                {
                    UnityEngine.Graphics.CopyTexture(texture, 0, TextureArray, Length);
                }
                else
                {
                    Texture2D tempTexture = Resize(texture, TextureArray.width, TextureArray.height);
                    tempTexture.Compress(true);
                    UnityEngine.Graphics.CopyTexture(tempTexture, 0, TextureArray, Length);
                    UnityEngine.Object.Destroy(tempTexture);
                }

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
        
        private static Texture2D Resize(Texture2D source, int newWidth, int newHeight)
        {
            source.filterMode = FilterMode.Bilinear;
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            rt.filterMode = FilterMode.Bilinear;
            RenderTexture.active = rt;
            UnityEngine.Graphics.Blit(source, rt);
            Texture2D nTex = new Texture2D(newWidth, newHeight);
            nTex.ReadPixels(new Rect(0, 0, newWidth, newWidth), 0,0);
            nTex.Apply();
            RenderTexture.active = null;
            return nTex;
        }
        
    }
}