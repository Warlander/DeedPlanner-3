using System;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public sealed class TerrainSurfaceProperties : IDisposable
    {
        private readonly Texture2DArray _properties;

        public TerrainSurfaceProperties(int count)
        {
            _properties = new Texture2DArray(1, 1, count, TextureFormat.RGBA32, false, true);
            _properties.filterMode = FilterMode.Point;
        }

        public void Set(int index, TextureReference reference, bool hasNormal)
        {
            Vector2 range = reference.SpecularRange;
            _properties.SetPixels(new[] { new Color(range.x, range.y, hasNormal ? 0.35f : 0, hasNormal ? 1 : 0) }, index);
            _properties.Apply(false, false);
        }

        public void ApplyTo(Material material)
        {
            material.SetTexture("_SurfaceProperties", _properties);
        }

        public void Dispose()
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(_properties);
            else UnityEngine.Object.DestroyImmediate(_properties);
        }
    }
}
