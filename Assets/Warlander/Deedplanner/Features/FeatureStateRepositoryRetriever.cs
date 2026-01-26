using UnityEngine;

namespace Warlander.Deedplanner.Features
{
    public class FeatureStateRepositoryRetriever
    {
        public FeatureStateRepository Get()
        {
            return Resources.Load<FeatureStateRepository>("FeatureStates");
        }
    }
}