using System;
using UnityEngine;

namespace Warlander.Deedplanner.Domain
{
    public class DynamicModelBehaviour : MonoBehaviour
    {
        public event Action<DynamicModelBehaviour, GameObject> ModelLoaded;

        public GameObject Model { get; private set; }
        private int? _raycastLayer;

        public void SetRaycastLayer(int layer)
        {
            _raycastLayer = layer;
            ApplyRaycastLayer(gameObject, layer);
        }
        
        protected void OnModelLoadedCallback(GameObject modelObject)
        {
            Model = modelObject;
            if (_raycastLayer.HasValue)
            {
                ApplyRaycastLayer(modelObject, _raycastLayer.Value);
            }
            ModelLoaded?.Invoke(this, modelObject);
        }

        private static void ApplyRaycastLayer(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.gameObject.layer = layer;
            }
        }
    }
}
