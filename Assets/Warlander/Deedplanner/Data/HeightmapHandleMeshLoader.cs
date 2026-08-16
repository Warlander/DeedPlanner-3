using UnityEngine;

namespace Warlander.Deedplanner.Data
{
    public class HeightmapHandleMeshLoader
    {
        private Mesh _mesh;

        public Mesh GetMesh()
        {
            if (_mesh == null)
            {
                _mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            }

            return _mesh;
        }
    }
}
