using System;
using UnityEngine;

namespace Warlogic.Features
{
    public class FeatureStateRetriever<TFeature> : IFeatureStateRetriever<TFeature>
        where TFeature : Enum
    {
        private readonly IFeatureStateRepository<TFeature> _repository;

        public FeatureStateRetriever(IFeatureStateRepository<TFeature> repository)
        {
            _repository = repository;
        }

        public bool IsFeatureEnabled(TFeature feature)
        {
            if (Application.isEditor)
            {
                return _repository.IsEnabledInEditor(feature);
            }

            if (Debug.isDebugBuild)
            {
                return _repository.IsEnabledInDebug(feature);
            }

            return _repository.IsEnabledInProduction(feature);
        }
    }
}
