using System.Text;
using System.Xml;
using Plugins.Warlander.Utils;
using UnityEngine;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgePart : TileEntity
    {
        public Bridge ParentBridge { get; private set; }

        public override Materials Materials
        {
            get
            {
                Materials materials = ParentBridge.Data.GetMaterialsForPart(partType, partSide);

                int extensionCount = GetExtensionCount();
                if (extensionCount > 0)
                {
                    Materials extensionMaterials = ParentBridge.Data.GetMaterialsForPart(BridgePartType.Extension, partSide);
                    for (int i = 0; i < extensionCount; i++)
                    {
                        materials.Add(extensionMaterials);
                    }
                }

                return materials;
            }
        }
        public BridgePartType PartType => partType;
        public bool Mirrored => orientation == EntityOrientation.Right || orientation == EntityOrientation.Up;

        private BridgePartType partType;
        private BridgePartSide partSide;
        private EntityOrientation orientation;

        private GameObject model;
        private MeshCollider _selectionMeshCollider;
        private Mesh _selectionMesh;
        private int _skew;
        private float _height;

        public void Initialise(Bridge parentBridge, BridgePartType partType, BridgePartSide partSide,
            EntityOrientation orientation, int x, int y, float height, int skew)
        {
            gameObject.layer = LayerMasks.BridgeLayer;
            ParentBridge = parentBridge;
            this.partType = partType;
            this.partSide = partSide;
            this.orientation = orientation;
            _height = height;

            // We need to use custom mesh collider here due to shape complexity of different kinds of bridges and their varying dimensions.
            if (!GetComponent<MeshCollider>())
            {
                _selectionMeshCollider = gameObject.AddComponent<MeshCollider>();
            }
            
            _skew = Mirrored ? -skew : skew;
            
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

            Model sourceModel = ParentBridge.Data.GetModelForPart(partType, partSide);

            // DP2 parity: side RIGHT parts are mirrored, and Up/Right oriented parts get an extra
            // flip - the two cancel out, so the total mirror is a XOR of the two conditions.
            bool mirror = (partSide == BridgePartSide.RIGHT)
                != (orientation == EntityOrientation.Up || orientation == EntityOrientation.Right);
            if (mirror)
            {
                foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
                {
                    filter.sharedMesh = sourceModel.GetMirroredMesh(filter.sharedMesh);
                }
            }

            CreateSupportExtensions(mirror);

            Bounds bounds = GetTotalModelBounds(sourceModel.OriginalModel);
            if (mirror)
            {
                bounds.center = new Vector3(4f - bounds.center.x, bounds.center.y, bounds.center.z);
            }
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
        
        private int GetExtensionCount()
        {
            if (partType != BridgePartType.Support || Tile == null)
            {
                return 0;
            }

            float relativeHeight = _height - Tile.SurfaceHeight - ParentBridge.Data.SupportHeight;
            return Mathf.Max(0, Mathf.CeilToInt(relativeHeight / 20f));
        }

        // Extensions are purely visual (never serialized): a chain of extension models under each
        // support, from deck-supportHeight down in steps of 20 until terrain level (DP2 behavior).
        private void CreateSupportExtensions(bool mirror)
        {
            if (partType != BridgePartType.Support)
            {
                return;
            }

            Model extensionModel = ParentBridge.Data.GetModelForPart(BridgePartType.Extension, partSide);
            int supportHeight = ParentBridge.Data.SupportHeight;
            int extensionCount = GetExtensionCount();

            for (int i = 0; i < extensionCount; i++)
            {
                float yOffset = -(supportHeight + 20f * i) * 0.1f;
                extensionModel.CreateOrGetModel(new Vector2(0, _skew), instance =>
                {
                    // Parented under the main model so outline renderer snapshots include extensions.
                    instance.transform.SetParent(model.transform, false);
                    instance.transform.localPosition = new Vector3(0, yOffset, 0);

                    foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
                    {
                        if (mirror)
                        {
                            filter.sharedMesh = extensionModel.GetMirroredMesh(filter.sharedMesh);
                        }

                        MeshCollider extensionCollider = filter.gameObject.AddComponent<MeshCollider>();
                        extensionCollider.sharedMesh = filter.sharedMesh;
                    }

                    OnModelLoadedCallback(model);
                });
            }
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

        public TextureReference GetUISprite()
        {
            return ParentBridge.Data.GetUISpriteForPart(partType);
        }
        
        public override string ToString()
        {
            StringBuilder build = new StringBuilder();

            build.Append("X: ").Append(Tile.X).Append(" Y: ").Append(Tile.Y).AppendLine();
            string bridgePartRawString = PartType.ToString();
            string bridgePartWithSpaces = StringUtils.AddSpacesToSentence(bridgePartRawString);
            string bridgePartLowercase = bridgePartWithSpaces.ToLower();
            build.Append(bridgePartLowercase);

            return build.ToString();
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