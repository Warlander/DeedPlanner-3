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
        private readonly IndexedTextureArray<TextureReference> _normals;
        private readonly Texture2D _flatNormal;

        public GroundTextureArray(IDataCatalog dataCatalog)
        {
            var textures = new HashSet<TextureReference>();
            foreach (GroundData data in dataCatalog.GetAllGrounds())
            {
                textures.Add(data.Tex3d);
                textures.Add(data.Tex2d);
            }

            _textures = new IndexedTextureArray<TextureReference>(TextureWidth, TextureHeight, textures.Count);
            _normals = new IndexedTextureArray<TextureReference>(TextureWidth, TextureHeight, textures.Count, linear: true);
            _flatNormal = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            _flatNormal.SetPixels(new[] { new Color(0.5f, 0.5f, 1, 0), new Color(0.5f, 0.5f, 1, 0),
                new Color(0.5f, 0.5f, 1, 0), new Color(0.5f, 0.5f, 1, 0) });
            _flatNormal.Apply();
        }

        public void ApplyTo(Material material)
        {
            material.SetTexture("_MainTex", _textures.TextureArray);
            material.SetTexture("_NormalArray", _normals.TextureArray);
        }

        public bool TryGetOrAdd(TextureReference reference, Texture2D texture, Texture2D normal, out int index)
        {
            if (!_textures.IsValid)
            {
                index = -1;
                return false;
            }

            index = _textures.PutOrGetTexture(reference, texture);
            return index >= 0 && _normals.PutOrGetTexture(reference, normal ? normal : _flatNormal) == index;
        }

        public void Dispose()
        {
            _textures.Dispose();
            _normals.Dispose();
            if (Application.isPlaying) UnityEngine.Object.Destroy(_flatNormal);
            else UnityEngine.Object.DestroyImmediate(_flatNormal);
        }
    }
}
