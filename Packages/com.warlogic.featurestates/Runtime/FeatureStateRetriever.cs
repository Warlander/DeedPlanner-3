using UnityEngine;

namespace Warlogic.Features
{
    public class FeatureStateRetriever : IFeatureStateRetriever
    {
        private readonly IFeatureStateRepository _featureStateRepository;

        public FeatureStateRetriever(IFeatureStateRepository featureStateRepository)
        {
            _featureStateRepository = featureStateRepository;
        }

        public bool IsFeatureEnabled(string featureName)
        {
            if (Application.isEditor)
            {
                return _featureStateRepository.IsEnabledInEditor(featureName);
            }

            if (Debug.isDebugBuild)
            {
                return _featureStateRepository.IsEnabledInDebug(featureName);
            }

            return _featureStateRepository.IsEnabledInProduction(featureName);
        }
    }
}
