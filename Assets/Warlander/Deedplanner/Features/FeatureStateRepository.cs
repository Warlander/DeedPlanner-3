using UnityEngine;

namespace Warlander.Deedplanner.Features
{
    [CreateAssetMenu(fileName = "FeatureStates", menuName = "DeedPlanner/Feature States", order = 0)]
    public class FeatureStateRepository : ScriptableObject
    {
        [SerializeField] private FeatureState[] featureStates;

        public bool IsEnabledInProduction(Feature feature)
        {
            return GetFeatureState(feature).EnabledInProduction;
        }

        public bool IsEnabledInDebug(Feature feature)
        {
            return GetFeatureState(feature).EnabledInDebug;
        }

        public bool IsEnabledInEditor(Feature feature)
        {
            return GetFeatureState(feature).EnabledInEditor;
        }

        private FeatureState GetFeatureState(Feature feature)
        {
            foreach (FeatureState state in featureStates)
            {
                if (state.Feature == feature)
                {
                    return state;
                }
            }

            return new FeatureState(feature);
        }
    }
}