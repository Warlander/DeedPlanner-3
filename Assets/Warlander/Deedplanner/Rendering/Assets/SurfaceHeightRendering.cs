using System;
using UnityEngine;
using VContainer.Unity;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Platform.Features;
using Warlogic.Features;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public sealed class SurfaceHeightRendering : IInitializable, IDisposable
    {
        private static readonly int _enabledPropertyId = Shader.PropertyToID("_SurfaceHeightDetailEnabled");

        private readonly GraphicsOptions _graphics;
        private readonly IFeatureStateRetriever<Feature> _features;

        public SurfaceHeightRendering(GraphicsOptions graphics, IFeatureStateRetriever<Feature> features)
        {
            _graphics = graphics;
            _features = features;
        }

        public void Initialize()
        {
            Shader.SetGlobalFloat(ShaderPropertyIds.BridgesPavementParallax,
                _features.IsFeatureEnabled(Feature.BridgesPavementParallax) ? 1 : 0);
            Shader.SetGlobalFloat(ShaderPropertyIds.CavesPavementParallax,
                _features.IsFeatureEnabled(Feature.CavesPavementParallax) ? 1 : 0);
            Apply();
            _graphics.SurfaceHeightDetailChanged += Apply;
        }

        private void Apply()
        {
            float enabled = _graphics.SurfaceHeightDetail ? 1 : 0;
            if (Shader.GetGlobalFloat(_enabledPropertyId) == enabled) return;
            Shader.SetGlobalFloat(_enabledPropertyId, enabled);
        }

        public void Dispose()
        {
            _graphics.SurfaceHeightDetailChanged -= Apply;
        }
    }
}
