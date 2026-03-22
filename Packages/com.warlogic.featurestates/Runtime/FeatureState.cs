using System;
using UnityEngine;

namespace Warlogic.Features
{
    [Serializable]
    public struct FeatureState
    {
        [SerializeField] private string _featureName;
        [SerializeField] private bool _enabledInProduction;
        [SerializeField] private bool _enabledInDebug;
        [SerializeField] private bool _enabledInEditor;

        public string FeatureName => _featureName;
        public bool EnabledInProduction => _enabledInProduction;
        public bool EnabledInDebug => _enabledInDebug;
        public bool EnabledInEditor => _enabledInEditor;

        public FeatureState(string featureName)
        {
            _featureName = featureName;
            _enabledInProduction = false;
            _enabledInDebug = false;
            _enabledInEditor = false;
        }
    }
}