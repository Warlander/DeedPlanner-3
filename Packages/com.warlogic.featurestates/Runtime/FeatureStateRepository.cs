using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warlogic.Features
{
    public abstract class FeatureStateRepository<TFeature> : FeatureStateRepositoryBase, IFeatureStateRepository<TFeature>
        where TFeature : Enum
    {
        [SerializeField] private FeatureStateEntry<TFeature>[] _featureStates;

        private Dictionary<TFeature, FeatureStateEntry<TFeature>> _lookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<TFeature, FeatureStateEntry<TFeature>>();
            if (_featureStates == null)
            {
                return;
            }

            foreach (FeatureStateEntry<TFeature> entry in _featureStates)
            {
                _lookup[entry.Feature] = entry;
            }
        }

        public bool IsEnabledInProduction(TFeature feature)
        {
            return Get(feature).EnabledInProduction;
        }

        public bool IsEnabledInDebug(TFeature feature)
        {
            return Get(feature).EnabledInDebug;
        }

        public bool IsEnabledInEditor(TFeature feature)
        {
            return Get(feature).EnabledInEditor;
        }

        private FeatureStateEntry<TFeature> Get(TFeature feature)
        {
            if (_lookup == null)
            {
                BuildLookup();
            }

            if (_lookup.TryGetValue(feature, out FeatureStateEntry<TFeature> entry))
            {
                return entry;
            }

            Debug.LogWarning($"No feature state found for feature {feature}");
            return new FeatureStateEntry<TFeature>(feature);
        }
    }
}
