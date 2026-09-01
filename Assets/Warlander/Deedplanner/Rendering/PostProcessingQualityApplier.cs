using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using VContainer.Unity;
using Warlander.Deedplanner.Cameras;

namespace Warlander.Deedplanner.Rendering
{
    public class PostProcessingQualityApplier : IInitializable, IDisposable
    {
        private const int MinPostProcessingLevel = 2;
        private const int MinSsaoLevel = 3;

        private readonly CameraCoordinator _cameraCoordinator;

        private List<ScreenSpaceAmbientOcclusion> _ssaoFeatures;

        public PostProcessingQualityApplier(CameraCoordinator cameraCoordinator)
        {
            _cameraCoordinator = cameraCoordinator;
        }

        public void Initialize()
        {
            _ssaoFeatures = FindSsaoFeatures();
            QualitySettings.activeQualityLevelChanged += OnQualityLevelChanged;
            Apply(QualitySettings.GetQualityLevel());
        }

        public void Dispose()
        {
            QualitySettings.activeQualityLevelChanged -= OnQualityLevelChanged;
        }

        private void OnQualityLevelChanged(int previousLevel, int newLevel)
        {
            Apply(newLevel);
        }

        private void Apply(int level)
        {
            bool postProcessing = level >= MinPostProcessingLevel;
            foreach (MultiCamera camera in _cameraCoordinator.Cameras)
            {
                // GetComponent over AttachedCamera: entry points can run before MultiCamera.Awake
                camera.GetComponent<Camera>().GetUniversalAdditionalCameraData().renderPostProcessing = postProcessing;
            }

            bool ssao = level >= MinSsaoLevel;
            foreach (ScreenSpaceAmbientOcclusion feature in _ssaoFeatures)
            {
                feature.SetActive(ssao);
            }
        }

        private List<ScreenSpaceAmbientOcclusion> FindSsaoFeatures()
        {
            List<ScreenSpaceAmbientOcclusion> results = new List<ScreenSpaceAmbientOcclusion>();

            int levelCount = QualitySettings.count;
            var seen = new HashSet<UniversalRenderPipelineAsset>();

            for (int i = 0; i < levelCount; i++)
            {
                var asset = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                if (asset == null || !seen.Add(asset)) continue;

                foreach (ScriptableRendererData rendererData in asset.rendererDataList)
                {
                    if (rendererData == null) continue;
                    ScreenSpaceAmbientOcclusion feature = rendererData.rendererFeatures
                        .OfType<ScreenSpaceAmbientOcclusion>()
                        .FirstOrDefault();
                    if (feature != null)
                        results.Add(feature);
                }
            }

            return results;
        }
    }
}
