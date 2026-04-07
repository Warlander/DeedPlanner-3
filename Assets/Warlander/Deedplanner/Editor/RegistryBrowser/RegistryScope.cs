using System;
using UnityEngine;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    [Serializable]
    public struct RegistryScope
    {
        [SerializeField] private string _scope;
        [SerializeField] private string _registryUrl;

        public string Scope => _scope;
        public string RegistryUrl => _registryUrl;

        public RegistryScope(string scope, string registryUrl)
        {
            _scope = scope;
            _registryUrl = registryUrl;
        }
    }
}
