using System;
using UnityEngine;
using Warlander.Deedplanner.Ui;
using Warlander.Deedplanner.Settings;

namespace Warlander.Deedplanner.Rendering.Water
{
    /// <summary>
    /// Subscribes to GraphicsOptions.WaterQualityChanged and keeps the PLANAR_REFLECTIONS shader keyword
    /// in sync with the current water quality setting.
    /// Independent of WaterController — neither depends on the other.
    /// </summary>
    public class WaterSettingsApplier : IDisposable
    {
        private static readonly string PlanarReflectionsKeyword = "PLANAR_REFLECTIONS";

        private readonly WaterObjectContainer _container;
        private readonly GraphicsOptions _graphics;

        public WaterSettingsApplier(WaterObjectContainer container, GraphicsOptions graphics)
        {
            _container = container;
            _graphics = graphics;
            _graphics.WaterQualityChanged += Apply;
            Apply();
        }

        private void Apply()
        {
            Material mat = _container.ComplexWaterRenderer.sharedMaterial;
            if (_graphics.WaterQuality == WaterQuality.Ultra)
            {
                mat.EnableKeyword(PlanarReflectionsKeyword);
            }
            else
            {
                mat.DisableKeyword(PlanarReflectionsKeyword);
            }
        }

        public void Dispose()
        {
            _graphics.WaterQualityChanged -= Apply;
        }
    }
}
