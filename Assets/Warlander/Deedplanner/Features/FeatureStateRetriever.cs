using UnityEngine;

namespace Warlander.Deedplanner.Features
{
    public class FeatureStateRetriever : IFeatureStateRetriever
    {
        private readonly FeatureStateRepository _featureStateRepository;

        public FeatureStateRetriever(FeatureStateRepository featureStateRepository)
        {
            _featureStateRepository = featureStateRepository;
        }

        public bool IsFeatureEnabled(Feature feature)
        {
            if (Application.isEditor)
            {
                return _featureStateRepository.IsEnabledInEditor(feature);
            }
            
            if (Debug.isDebugBuild)
            {
                return _featureStateRepository.IsEnabledInDebug(feature);
            }

            return _featureStateRepository.IsEnabledInProduction(feature);
        }
    }
}