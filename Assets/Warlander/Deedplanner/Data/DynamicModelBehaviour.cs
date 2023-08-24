using System;
using UnityEngine;

namespace Warlander.Deedplanner.Data
{
    public class DynamicModelBehaviour : MonoBehaviour
    {
        public event Action<DynamicModelBehaviour, GameObject> ModelLoaded;

        public GameObject Model { get; private set; }
        
        protected void OnModelLoadedCallback(GameObject modelObject)
        {
            Model = modelObject;
            ModelLoaded?.Invoke(this, modelObject);
        }
    }
}