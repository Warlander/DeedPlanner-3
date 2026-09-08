using UnityEngine;
using UnityEngine.Rendering;
using Warlander.Deedplanner.Domain;

namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveChunk : MonoBehaviour
    {
        public int MinimumX { get; private set; }
        public int MinimumY { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int RenderRevision { get; private set; }
        public int ColliderRevision { get; private set; }
        public int RenderTriangleCount => _renderMesh ? _renderMesh.triangles.Length / 3 : 0;
        public int ColliderTriangleCount => _faces.Length;

        private Mesh _renderMesh;
        private Mesh _colliderMesh;
        private MeshCollider _meshCollider;
        private CaveFace[] _faces = new CaveFace[0];

        public void Initialize(int minimumX, int minimumY, int width, int height, Material material)
        {
            gameObject.layer = LayerMasks.CaveLayer;
            MinimumX = minimumX;
            MinimumY = minimumY;
            Width = width;
            Height = height;

            var meshFilter = gameObject.AddComponent<MeshFilter>();
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshCollider = gameObject.AddComponent<MeshCollider>();
            _renderMesh = new Mesh { name = $"Cave Render {minimumX},{minimumY}" };
            _colliderMesh = new Mesh { name = $"Cave Collider {minimumX},{minimumY}" };
            meshFilter.sharedMesh = _renderMesh;
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            _meshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation |
                                           MeshColliderCookingOptions.UseFastMidphase;
        }

        public void RebuildRender(CaveTopologyBuilder builder)
        {
            RenderRevision++;
            CaveTopology topology = builder.Build(MinimumX, MinimumY, Width, Height, RenderRevision);
            topology.ApplyTo(_renderMesh);
        }

        public void RebuildCollider(CaveTopologyBuilder builder)
        {
            int revision = ColliderRevision + 1;
            CaveTopology topology = builder.BuildCollider(MinimumX, MinimumY, Width, Height, revision);
            topology.ApplyTo(_colliderMesh);
            _meshCollider.sharedMesh = null;
            if (topology.TriangleCount > 0)
            {
                _meshCollider.sharedMesh = _colliderMesh;
            }
            _faces = topology.Faces.ToArray();
            ColliderRevision = revision;
        }

        public bool TryGetFace(int triangleIndex, out CaveFace face)
        {
            if (triangleIndex < 0 || triangleIndex >= _faces.Length)
            {
                face = default;
                return false;
            }

            face = _faces[triangleIndex];
            return face.Revision == ColliderRevision;
        }

        public bool TryGetLogicalFaceTriangleRange(int triangleIndex, out int firstTriangleIndex,
            out int triangleCount)
        {
            if (!TryGetFace(triangleIndex, out CaveFace face))
            {
                firstTriangleIndex = 0;
                triangleCount = 0;
                return false;
            }

            firstTriangleIndex = triangleIndex;
            while (firstTriangleIndex > 0 && face.IsSameLogicalFace(_faces[firstTriangleIndex - 1]))
            {
                firstTriangleIndex--;
            }

            int lastTriangleIndex = triangleIndex;
            while (lastTriangleIndex + 1 < _faces.Length
                   && face.IsSameLogicalFace(_faces[lastTriangleIndex + 1]))
            {
                lastTriangleIndex++;
            }

            triangleCount = lastTriangleIndex - firstTriangleIndex + 1;
            return true;
        }

        private void OnDestroy()
        {
            DestroyMesh(_renderMesh);
            DestroyMesh(_colliderMesh);
        }

        private static void DestroyMesh(Object target)
        {
            if (!target)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
