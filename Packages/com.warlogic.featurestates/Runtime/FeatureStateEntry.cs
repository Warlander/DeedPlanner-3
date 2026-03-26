using System;
using UnityEngine;

namespace Warlogic.Features
{
    [Serializable]
    public struct FeatureStateEntry<TFeature> where TFeature : Enum
    {
        [SerializeField] private TFeature _feature;
        [SerializeField] private bool _enabledInProduction;
        [SerializeField] private bool _enabledInDebug;
        [SerializeField] private bool _enabledInEditor;

        public TFeature Feature => _feature;
        public bool EnabledInProduction => _enabledInProduction;
        public bool EnabledInDebug => _enabledInDebug;
        public bool EnabledInEditor => _enabledInEditor;

        public FeatureStateEntry(TFeature feature)
        {
            _feature = feature;
            _enabledInProduction = false;
            _enabledInDebug = false;
            _enabledInEditor = false;
        }
    }
}
