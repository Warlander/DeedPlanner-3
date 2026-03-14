using System;
using UnityEngine;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Settings;

namespace Warlander.Deedplanner.Graphics.Water
{
    /// <summary>
    /// Subscribes to DPSettings.Modified and keeps the PLANAR_REFLECTIONS shader keyword
    /// in sync with the current water quality setting.
    /// Independent of WaterController — neither depends on the other.
    /// </summary>
    public class WaterSettingsApplier : IDisposable
    {
        private static readonly string PlanarReflectionsKeyword = "PLANAR_REFLECTIONS";

        private readonly WaterObjectContainer _container;
        private readonly DPSettings _settings;

        public WaterSettingsApplier(WaterObjectContainer container, DPSettings settings)
        {
            _container = container;
            _settings = settings;
            _settings.Modified += Apply;
            Apply();
        }

        private void Apply()
        {
            Material mat = _container.ComplexWaterRenderer.sharedMaterial;
            if (_settings.WaterQuality == WaterQuality.Ultra)
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
            _settings.Modified -= Apply;
        }
    }
}
