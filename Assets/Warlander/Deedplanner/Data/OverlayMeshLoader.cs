using UnityEngine;

namespace Warlander.Deedplanner.Data
{
    public class OverlayMeshLoader
    {
        private const string OverlayMeshResourcePath = "Overlay Mesh";

        private OverlayMesh _prefab;

        public OverlayMesh CreateInstance(Transform parent)
        {
            if (_prefab == null)
            {
                _prefab = Resources.Load<OverlayMesh>(OverlayMeshResourcePath);
            }

            return Object.Instantiate(_prefab, parent);
        }
    }
}
