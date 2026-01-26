using System;
using UnityEngine;

namespace Warlander.Deedplanner.Features
{
    [Serializable]
    public struct FeatureState
    {
        [SerializeField] private Feature _feature;
        [SerializeField] private bool _enabledInProduction;
        [SerializeField] private bool _enabledInDebug;
        [SerializeField] private bool _enabledInEditor;

        public Feature Feature => _feature;
        public bool EnabledInProduction => _enabledInProduction;
        public bool EnabledInDebug => _enabledInDebug;
        public bool EnabledInEditor => _enabledInEditor;
        
        public FeatureState(Feature feature)
        {
            _feature = feature;
            _enabledInProduction = false;
            _enabledInDebug = false;
            _enabledInEditor = false;
        }
    }
}