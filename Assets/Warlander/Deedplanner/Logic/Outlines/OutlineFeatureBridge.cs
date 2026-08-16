using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Warlander.Deedplanner.Graphics.Outline;
using VContainer;
using VContainer.Unity;

namespace Warlander.Deedplanner.Logic.Outlines
{
    public class OutlineFeatureBridge : IInitializable, IDisposable
    {
        private readonly IOutlineCoordinator _coordinator;

        private List<ScreenSpaceOutlineFeature> _registeredFeatures;

        public OutlineFeatureBridge(IOutlineCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        void IInitializable.Initialize()
        {
            _registeredFeatures = FindAllFeatures();
            foreach (ScreenSpaceOutlineFeature feature in _registeredFeatures)
            {
                feature.RegisterCoordinator(_coordinator);
            }

        }

        void IDisposable.Dispose()
        {
            foreach (ScreenSpaceOutlineFeature feature in _registeredFeatures)
            {
                feature.UnregisterCoordinator();
            }

            _registeredFeatures.Clear();
        }

        private List<ScreenSpaceOutlineFeature> FindAllFeatures()
        {
            List<ScreenSpaceOutlineFeature> results = new List<ScreenSpaceOutlineFeature>();

            int levelCount = QualitySettings.count;
            var seen = new HashSet<UniversalRenderPipelineAsset>();

            for (int i = 0; i < levelCount; i++)
            {
                var asset = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                if (asset == null || !seen.Add(asset)) continue;

                foreach (ScriptableRendererData rendererData in asset.rendererDataList)
                {
                    if (rendererData == null) continue;
                    ScreenSpaceOutlineFeature feature = rendererData.rendererFeatures
                        .OfType<ScreenSpaceOutlineFeature>()
                        .FirstOrDefault();
                    if (feature != null)
                        results.Add(feature);
                }
            }

            return results;
        }
    }
}
