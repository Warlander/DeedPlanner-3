using System;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Render;

namespace Warlander.Deedplanner.Domain.Entities.Grounds
{
    public sealed class GroundTextureArray : IDisposable
    {
        private const int TextureWidth = 512;
        private const int TextureHeight = 512;

        private readonly IndexedTextureArray<TextureReference> _textures;

        public GroundTextureArray(IDataCatalog dataCatalog)
        {
            var textures = new HashSet<TextureReference>();
            foreach (GroundData data in dataCatalog.GetAllGrounds())
            {
                textures.Add(data.Tex3d);
            }

            _textures = new IndexedTextureArray<TextureReference>(TextureWidth, TextureHeight, textures.Count);
        }

        public void ApplyTo(Material material)
        {
            material.SetTexture("_MainTex", _textures.TextureArray);
        }

        public bool TryGetOrAdd(TextureReference reference, Texture2D texture, out int index)
        {
            if (!_textures.IsValid)
            {
                index = -1;
                return false;
            }

            index = _textures.PutOrGetTexture(reference, texture);
            return index >= 0;
        }

        public void Dispose()
        {
            _textures.Dispose();
        }
    }
}
