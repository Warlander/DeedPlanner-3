using UnityEngine;

namespace Warlogic.Features
{
    [CreateAssetMenu(fileName = "FeatureStates", menuName = "Features/Feature States", order = 0)]
    public class FeatureStateRepository : ScriptableObject, IFeatureStateRepository
    {
        [SerializeField] private FeatureState[] featureStates;

#if UNITY_EDITOR
        [SerializeField] private UnityEditor.MonoScript _featureNamesSource;
#endif

        public bool IsEnabledInProduction(string featureName)
        {
            return GetFeatureState(featureName).EnabledInProduction;
        }

        public bool IsEnabledInDebug(string featureName)
        {
            return GetFeatureState(featureName).EnabledInDebug;
        }

        public bool IsEnabledInEditor(string featureName)
        {
            return GetFeatureState(featureName).EnabledInEditor;
        }

        private FeatureState GetFeatureState(string featureName)
        {
            foreach (FeatureState state in featureStates)
            {
                if (state.FeatureName == featureName)
                {
                    return state;
                }
            }

            Debug.LogWarning($"No feature state found for feature {featureName}");
            return new FeatureState(featureName);
        }
    }
}
