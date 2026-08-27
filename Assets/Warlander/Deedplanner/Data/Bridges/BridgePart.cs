using System.Collections.Generic;
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

                if (Pavement != null)
                {
                    materials.Add(Pavement.Materials);
                }

                return materials;
            }
        }
        public BridgePartType PartType => partType;
        public int SegmentIndex { get; private set; }
        public int LaneIndex { get; private set; }
        public BridgePavementData Pavement { get; private set; }
        public bool Mirrored => orientation == EntityOrientation.Right || orientation == EntityOrientation.Up;

        private BridgePartType partType;
        private BridgePartSide partSide;
        private BridgePartSide modelSide;
        private EntityOrientation orientation;

        private GameObject model;
        private readonly List<GameObject> _extensions = new List<GameObject>();
        private int _extensionGeneration;
        private EntityOrientation _renderOrientation;
        private GameObject _pavingOverlay;
        private Mesh _pavingMesh;
        private MeshCollider _selectionMeshCollider;
        private Mesh _selectionMesh;
        private int _skew;
        private float _height;

        public void Initialise(Bridge parentBridge, BridgePartType partType, BridgePartSide partSide,
            EntityOrientation orientation, int x, int y, float height, int skew, int segmentIndex, int laneIndex)
        {
            gameObject.layer = LayerMasks.BridgeLayer;
            ParentBridge = parentBridge;
            this.partType = partType;
            this.partSide = partSide;
            this.orientation = orientation;
            SegmentIndex = segmentIndex;
            LaneIndex = laneIndex;
            _height = height;

            // Abutment and bracing have dedicated left/right models selected by lane and row
            // facing; every other part type shares one side model and one lane is rotated 180.
            // Mesh mirroring is never correct here - mirror and rotation differ by a length flip.
            bool directional = partType == BridgePartType.Abutment || partType == BridgePartType.Bracing;
            modelSide = partSide;
            if (directional && (partSide == BridgePartSide.LEFT || partSide == BridgePartSide.RIGHT))
            {
                bool useLeftModel = (partSide == BridgePartSide.LEFT)
                    == (orientation == EntityOrientation.Down || orientation == EntityOrientation.Right);
                modelSide = useLeftModel ? BridgePartSide.LEFT : BridgePartSide.RIGHT;
            }

            bool flipLane = !directional && (partSide == BridgePartSide.LEFT || partSide == BridgePartSide.RIGHT)
                && ((partSide == BridgePartSide.LEFT)
                    == (orientation == EntityOrientation.Down || orientation == EntityOrientation.Right));
            EntityOrientation renderOrientation = flipLane ? Opposite(orientation) : orientation;
            _renderOrientation = renderOrientation;

            // We need to use custom mesh collider here due to shape complexity of different kinds of bridges and their varying dimensions.
            if (!GetComponent<MeshCollider>())
            {
                _selectionMeshCollider = gameObject.AddComponent<MeshCollider>();
            }

            _skew = (renderOrientation == EntityOrientation.Right || renderOrientation == EntityOrientation.Up)
                ? -skew : skew;

            _selectionMesh = CreateSelectionMesh(_skew);
            _selectionMeshCollider.sharedMesh = _selectionMesh;

            if (renderOrientation == EntityOrientation.Left)
            {
                transform.position = GetPositionForHeight(height, skew);
                transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
            else if (renderOrientation == EntityOrientation.Up)
            {
                transform.position = GetPositionForHeight(height, skew);
                transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
            else if (renderOrientation == EntityOrientation.Right)
            {
                transform.position = GetPositionForHeight(height, skew);
                transform.localRotation = Quaternion.Euler(0, 270, 0);
            }
            else
            {
                transform.position = GetPositionForHeight(height, skew);
            }

            ModelHandle rootModel = parentBridge.Data.GetModelForPart(partType, modelSide);
            rootModel.CreateOrGetModel(new Vector2(0, _skew), OnModelCreated);

            RefreshPaving();
        }

        private const float PavingEpsilon = 0.02f;

        public void SetPavement(BridgePavementData pavement)
        {
            Pavement = pavement;
            RefreshPaving();
        }

        public void RefreshPaving()
        {
            BridgePavementData pavement = Pavement;

            if (pavement == null)
            {
                if (_pavingOverlay)
                {
                    Destroy(_pavingOverlay);
                    _pavingOverlay = null;
                    DestroyPavingMesh();
                }

                return;
            }

            if (!_pavingOverlay)
            {
                _pavingOverlay = CreatePavingOverlay();
            }

            _pavingOverlay.GetComponent<MeshRenderer>().sharedMaterial = pavement.GetOrCreateOverlayMaterial();
        }

        // Flat quad over the deck in part-local space (x in [0,4], z in [-4,0], deck at y=0),
        // skew baked into the z=0 edge (local slope direction) so no shear shader property is needed.
        private GameObject CreatePavingOverlay()
        {
            GameObject overlay = new GameObject("Paving Overlay");
            overlay.layer = LayerMasks.BridgeLayer;
            overlay.transform.SetParent(transform, false);

            float lowY = PavingEpsilon;
            float highY = PavingEpsilon + _skew * 0.1f;
            _pavingMesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(0, lowY, -4),
                    new Vector3(4, lowY, -4),
                    new Vector3(0, highY, 0),
                    new Vector3(4, highY, 0)
                },
                uv = new[]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1)
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 }
            };
            _pavingMesh.RecalculateNormals();

            overlay.AddComponent<MeshFilter>().sharedMesh = _pavingMesh;
            overlay.AddComponent<MeshRenderer>();
            return overlay;
        }

        private void DestroyPavingMesh()
        {
            if (_pavingMesh)
            {
                Destroy(_pavingMesh);
                _pavingMesh = null;
            }
        }

        private static EntityOrientation Opposite(EntityOrientation orientation)
        {
            switch (orientation)
            {
                case EntityOrientation.Up:
                    return EntityOrientation.Down;
                case EntityOrientation.Down:
                    return EntityOrientation.Up;
                case EntityOrientation.Left:
                    return EntityOrientation.Right;
                default:
                    return EntityOrientation.Left;
            }
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

        private Vector3 GetPositionForHeight(float height, int skew)
        {
            if (_renderOrientation == EntityOrientation.Left)
            {
                return new Vector3((Tile.X + 1) * 4, height * 0.1f, (Tile.Y + 1) * 4);
            }
            if (_renderOrientation == EntityOrientation.Up)
            {
                return new Vector3((Tile.X + 1) * 4, height * 0.1f + skew * 0.1f, Tile.Y * 4);
            }
            if (_renderOrientation == EntityOrientation.Right)
            {
                return new Vector3(Tile.X * 4, height * 0.1f + skew * 0.1f, Tile.Y * 4);
            }
            return new Vector3(Tile.X * 4, height * 0.1f, (Tile.Y + 1) * 4);
        }

        public void UpdateHeight(float height, int delta)
        {
            _height = height;
            transform.position = GetPositionForHeight(height, delta);

            int signedSkew = (_renderOrientation == EntityOrientation.Right || _renderOrientation == EntityOrientation.Up)
                ? -delta : delta;
            if (signedSkew != _skew)
            {
                _skew = signedSkew;
                // Skew selects a different model variant; OnModelCreated rebuilds selection mesh and extensions.
                ParentBridge.Data.GetModelForPart(partType, modelSide).CreateOrGetModel(new Vector2(0, _skew), OnModelCreated);
                return;
            }

            RefreshExtensions();
        }

        public void RefreshExtensions()
        {
            if (partType != BridgePartType.Support || model == null)
            {
                return;
            }
            if (GetExtensionCount() == _extensions.Count)
            {
                return;
            }

            _extensionGeneration++;
            foreach (GameObject extension in _extensions)
            {
                if (extension != null)
                {
                    Destroy(extension);
                }
            }
            _extensions.Clear();
            CreateSupportExtensions();
        }

        private void OnModelCreated(GameObject newModel)
        {
            if (model)
            {
                Destroy(model);
            }

            model = newModel;
            model.transform.SetParent(transform, false);

            ModelHandle sourceModel = ParentBridge.Data.GetModelForPart(partType, modelSide);

            _extensionGeneration++;
            _extensions.Clear();
            CreateSupportExtensions();

            Bounds bounds = GetTotalModelBounds(sourceModel.OriginalModel);
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

            // The column lands in the tile interior where ground blends all four corners;
            // min guarantees the chain reaches ground on slopes.
            int groundHeight = Tile.SurfaceHeight;
            for (int dx = 0; dx <= 1; dx++)
            {
                for (int dy = 0; dy <= 1; dy++)
                {
                    Tile corner = Tile.Map[Tile.X + dx, Tile.Y + dy];
                    if (corner != null)
                    {
                        groundHeight = Mathf.Min(groundHeight, corner.SurfaceHeight);
                    }
                }
            }

            float relativeHeight = _height - groundHeight - ParentBridge.Data.SupportHeight;
            return Mathf.Max(0, Mathf.CeilToInt(relativeHeight / 20f));
        }

        // Extensions are purely visual (never serialized): a chain of extension models under each
        // support, from deck-supportHeight down in steps of 20 until terrain level (DP2 behavior).
        private void CreateSupportExtensions()
        {
            if (partType != BridgePartType.Support)
            {
                return;
            }

            ModelHandle extensionModel = ParentBridge.Data.GetModelForPart(BridgePartType.Extension, partSide);
            int supportHeight = ParentBridge.Data.SupportHeight;
            int extensionCount = GetExtensionCount();
            int generation = _extensionGeneration;

            for (int i = 0; i < extensionCount; i++)
            {
                float yOffset = -(supportHeight + 20f * i) * 0.1f;
                extensionModel.CreateOrGetModel(new Vector2(0, _skew), instance =>
                {
                    if (generation != _extensionGeneration)
                    {
                        Destroy(instance);
                        return;
                    }

                    // Parented under the main model so outline renderer snapshots include extensions.
                    instance.transform.SetParent(model.transform, false);
                    instance.transform.localPosition = new Vector3(0, yOffset, 0);
                    _extensions.Add(instance);

                    foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
                    {
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

            DestroyPavingMesh();
        }
    }
}