using System;
using UnityEngine;

namespace Warlogic.Features
{
    public class ResourceFeatureStateRepositoryRetriever<TFeature>
        where TFeature : Enum
    {
        private readonly string _resourcePath;

        public ResourceFeatureStateRepositoryRetriever(string resourcePath)
        {
            _resourcePath = resourcePath;
        }

        public IFeatureStateRepository<TFeature> Get()
        {
            return Resources.Load<FeatureStateRepositoryBase>(_resourcePath) as IFeatureStateRepository<TFeature>;
        }
    }
}
