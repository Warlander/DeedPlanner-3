using UnityEngine;

namespace Warlogic.Features
{
    public class ResourceFeatureStateRepositoryRetriever
    {
        private readonly string _resourcePath;
        
        public ResourceFeatureStateRepositoryRetriever(string resourcePath)
        {
            _resourcePath = resourcePath;
        }
        
        public IFeatureStateRepository Get()
        {
            return Resources.Load<FeatureStateRepository>(_resourcePath);
        }
    }
}