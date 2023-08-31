using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgePart : DynamicModelBehaviour
    {
        public Bridge ParentBridge { get; private set; }

        public Materials Materials => ParentBridge.Data.GetMaterialsForPart(partType, partSide);
        public BridgePartType PartType => partType;

        private BridgePartType partType;
        private BridgePartSide partSide;
        private EntityOrientation orientation;

        private GameObject model;
        private MeshCollider _selectionMeshCollider;
        private Mesh _selectionMesh;
        private int _skew;

        public void Initialise(Bridge parentBridge, BridgePartType partType, BridgePartSide partSide,
            EntityOrientation orientation, int x, int y, float height, int skew)
        {
            gameObject.layer = LayerMasks.BridgeLayer;
            ParentBridge = parentBridge;
            this.partType = partType;
            this.partSide = partSide;
            this.orientation = orientation;

            // We need to use custom mesh collider here due to shape complexity of different kinds of bridges and their varying dimensions.
            if (!GetComponent<MeshCollider>())
            {
                _selectionMeshCollider = gameObject.AddComponent<MeshCollider>();
            }

            bool isRotatedAwayFromOrigin =
                orientation == EntityOrientation.Right || orientation == EntityOrientation.Up;
            _skew = isRotatedAwayFromOrigin ? -skew : skew;
            
            _selectionMesh = CreateSelectionMesh(_skew);
            _selectionMeshCollider.sharedMesh = _selectionMesh;
            
            if (orientation == EntityOrientation.Left)
            {
                transform.position = new Vector3((x + 1) * 4, height * 0.1f, (y + 1) * 4);
                transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
            else if (orientation == EntityOrientation.Up)
            {
                transform.position = new Vector3((x + 1) * 4, height * 0.1f + skew * 0.1f, y * 4);
                transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
            else if (orientation == EntityOrientation.Right)
            {
                transform.position = new Vector3(x * 4, height * 0.1f + skew * 0.1f, y * 4);
                transform.localRotation = Quaternion.Euler(0, 270, 0);
            }
            else
            {
                transform.position = new Vector3(x * 4, height * 0.1f, (y + 1) * 4);
            }

            Model rootModel = parentBridge.Data.GetModelForPart(partType, partSide);
            rootModel.CreateOrGetModel(new Vector2(0, _skew), OnModelCreated);
        }

        private Mesh CreateSelectionMesh(int slopeDifference)
        {
            // temporary bounds for new wall before it is initialized with final model
            Bounds bounds = new Bounds(new Vector3(-2, 0, -2), new Vector3(4, 0.01f, 4));
            
            Mesh mesh = new Mesh();
            
            Vector3[] vectors = CreateBoundsVerticesArray(bounds, slopeDifference);
            int[] triangles = new int[36];

            // bottom
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 2;
            triangles[4] = 3;
            triangles[5] = 0;

            // top
            triangles[6] = 4;
            triangles[7] = 5;
            triangles[8] = 6;
            triangles[9] = 6;
            triangles[10] = 7;
            triangles[11] = 4;

            // left
            triangles[12] = 0;
            triangles[13] = 1;
            triangles[14] = 4;
            triangles[15] = 1;
            triangles[16] = 5;
            triangles[17] = 4;

            // right
            triangles[18] = 2;
            triangles[19] = 3;
            triangles[20] = 6;
            triangles[21] = 3;
            triangles[22] = 7;
            triangles[23] = 6;

            //up
            triangles[24] = 4;
            triangles[25] = 3;
            triangles[26] = 0;
            triangles[27] = 4;
            triangles[28] = 7;
            triangles[29] = 3;

            //down
            triangles[30] = 1;
            triangles[31] = 2;
            triangles[32] = 5;
            triangles[33] = 2;
            triangles[34] = 6;
            triangles[35] = 5;
            
            mesh.vertices = vectors;
            mesh.triangles = triangles;

            return mesh;
        }
        
        private static Vector3[] CreateBoundsVerticesArray(Bounds bounds, int slopeDifference)
        {
            Vector3[] vectors = new Vector3[8];
            const float padding = 1.01f;
            vectors[0] = (bounds.center - new Vector3(bounds.extents.x, bounds.extents.y - slopeDifference * 0.1f, -bounds.extents.z) * padding);
            vectors[1] = (bounds.center - new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z) * padding);
            vectors[2] = (bounds.center - new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z) * padding);
            vectors[3] = (bounds.center - new Vector3(-bounds.extents.x, bounds.extents.y - slopeDifference * 0.1f, -bounds.extents.z) * padding);
            vectors[4] = (bounds.center - new Vector3(bounds.extents.x, -bounds.extents.y - slopeDifference * 0.1f, -bounds.extents.z) * padding);
            vectors[5] = (bounds.center - new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z) * padding);
            vectors[6] = (bounds.center - new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z) * padding);
            vectors[7] = (bounds.center - new Vector3(-bounds.extents.x, -bounds.extents.y - slopeDifference * 0.1f, -bounds.extents.z) * padding);

            return vectors;
        }

        private void OnModelCreated(GameObject newModel)
        {
            if (model)
            {
                Destroy(model);
            }
            
            model = newModel;
            model.transform.SetParent(transform, false);

            Bounds bounds = GetTotalModelBounds(ParentBridge.Data.GetModelForPart(partType, partSide).OriginalModel);
            const float wallDepthComfortableMargin = 0.75f;
            float comfortableWallDepth = Mathf.Max(bounds.size.z, wallDepthComfortableMargin);
            bounds.size = new Vector3(-bounds.size.x, bounds.size.y, comfortableWallDepth);
            
            Vector3[] vectors = CreateBoundsVerticesArray(bounds, _skew);

            _selectionMesh.vertices = vectors;
            // turning collider off and on to force it to update
            _selectionMeshCollider.enabled = false;
            // ReSharper disable once Unity.InefficientPropertyAccess
            _selectionMeshCollider.enabled = true;
            
            OnModelLoadedCallback(model);
        }
        
        private Bounds GetTotalModelBounds(GameObject model)
        {
            Bounds bounds = new Bounds();
            MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter filter in filters)
            {
                Mesh mesh = filter.sharedMesh;
                bounds.Encapsulate(mesh.bounds);
            }
            return bounds;
        }
        
        private void OnDestroy()
        {
            if (_selectionMesh)
            {
                Destroy(_selectionMesh);
            }
        }
    }
}