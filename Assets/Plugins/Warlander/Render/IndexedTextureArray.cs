using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Warlander.Render
{
    public class IndexedTextureArray<T>
    {
        private const TextureFormat DefaultGroundTexturesFormat = TextureFormat.DXT5;
        private const TextureFormat FallbackGroundTexturesFormat = TextureFormat.ARGB32;
        
        private readonly Dictionary<T, int> indexToSlice;
        
        public Texture2DArray TextureArray { get; private set; }
        public int MaxLength => TextureArray.depth;
        public int Length { get; private set; }

        private readonly Material blitCopyMaterial;

        public IndexedTextureArray(int width, int height, int depth, bool mipmaps = true)
        {
            TextureArray = new Texture2DArray(width, height, depth, GetTextureFormatToUse(), mipmaps);
            TextureArray.name = "GroundTextureArray";
            indexToSlice = new Dictionary<T, int>();
            
            Shader blitCopyShader = Shader.Find("Hidden/BlitCopy");
            blitCopyMaterial = new Material(blitCopyShader);
        }

        private TextureFormat GetTextureFormatToUse()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                return DefaultGroundTexturesFormat;
            }
            else
            {
                return FallbackGroundTexturesFormat;
            }
        }
        
        public int PutOrGetTexture(T key, Texture2D texture)
        {
            int index = GetTextureIndex(key);

            if (index == -1)
            {
                bool textureAdded = PutTexture(key, texture);
                index = GetTextureIndex(key);

                if (!textureAdded)
                {
                    Debug.LogWarning("Trying to add new texture to full Texture2DArray");
                }
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
                if (texture.mipmapCount == TextureArray.mipmapCount && texture.format == TextureArray.format)
                {
                    AppendTexture(texture, Length);
                }
                else
                {
                    Debug.Log("Resizing and changing texture format to fit texture array: " + texture.name);
                    Texture2D tempTexture = Resize(texture, TextureArray.width, TextureArray.height);
                    if (TextureArray.format == TextureFormat.DXT5)
                    {
                        tempTexture.Compress(true);
                    }

                    AppendTexture(tempTexture, Length);
                    
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

        private void AppendTexture(Texture2D texture, int index)
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                Graphics.CopyTexture(texture, 0, TextureArray, index);
            }
            else
            {
                var pixels =  texture.GetPixels();
                TextureArray.SetPixels(pixels, index);
                TextureArray.Apply(true);
            }
        }
        
        public bool Contains(T key)
        {
            return indexToSlice.ContainsKey(key);
        }
        
        private Texture2D Resize(Texture2D source, int newWidth, int newHeight)
        {
            source.filterMode = FilterMode.Bilinear;
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            rt.filterMode = FilterMode.Bilinear;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt, blitCopyMaterial);
            Texture2D nTex = new Texture2D(newWidth, newHeight);
            nTex.ReadPixels(new Rect(0, 0, newWidth, newWidth), 0,0);
            nTex.Apply();
            RenderTexture.active = null;
            return nTex;
        }
    }
}