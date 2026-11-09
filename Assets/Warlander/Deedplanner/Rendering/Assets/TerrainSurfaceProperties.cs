using System;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public sealed class TerrainSurfaceProperties : IDisposable
    {
        private readonly Texture2DArray _properties;
        private readonly Texture2DArray _heights;

        public TerrainSurfaceProperties(int count)
        {
            _properties = new Texture2DArray(1, 1, count, TextureFormat.RGBA32, false, true);
            _properties.filterMode = FilterMode.Point;
            _heights = new Texture2DArray(1, 1, count, TextureFormat.RGBAHalf, false, true);
            _heights.filterMode = FilterMode.Point;
        }

        public void Set(int index, TextureReference reference, bool hasNormal, bool hasHeight = false)
        {
            Vector2 range = reference.SpecularRange;
            _properties.SetPixels(new[] { new Color(range.x, range.y, hasNormal ? 0.35f : 0, hasNormal ? 1 : 0) }, index);
            _properties.Apply(false, false);
            _heights.SetPixels(new[] { new Color(hasHeight ? reference.HeightScale : 0, 0, 0, 0) }, index);
            _heights.Apply(false, false);
        }

        public void ApplyTo(Material material)
        {
            material.SetTexture("_SurfaceProperties", _properties);
            material.SetTexture("_HeightProperties", _heights);
        }

        public void Dispose()
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(_properties);
            else UnityEngine.Object.DestroyImmediate(_properties);
            if (Application.isPlaying) UnityEngine.Object.Destroy(_heights);
            else UnityEngine.Object.DestroyImmediate(_heights);
        }
    }
}
