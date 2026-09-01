using Warlander.Deedplanner.Domain;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Floors;

namespace Warlander.Deedplanner.Docks
{
    public class Dock : TileEntity
    {
        private GameObject _deckModel;
        private GameObject _supportRoot;
        private GameObject _supportModel;
        private readonly List<GameObject> _extensions = new List<GameObject>();
        private int _supportGeneration;
        private Material _ghostMaterial;
        private readonly Dictionary<Renderer, Material> _originalMaterials = new Dictionary<Renderer, Material>();
        private MaterialPropertyBlock _invalidPropertyBlock;

        public int Height { get; private set; }
        public int AnchorLevel { get; private set; }
        public FloorData Floor { get; private set; }
        public DockSupportData Support { get; private set; }
        public EntityOrientation BraceRotation { get; private set; }
        public IReadOnlyList<string> ValidationErrors { get; private set; } = new List<string>();
        public override Materials Materials
        {
            get
            {
                Materials materials = new Materials();
                materials.Add(Floor.Materials);

                if (Support != null && Support.Materials != null)
                {
                    int count = Support.Type == DockSupportType.Brace ? 1 : ChargedSegmentCount();
                    for (int i = 0; i < count; i++)
                    {
                        materials.Add(Support.Materials);
                    }
                }

                return materials;
            }
        }

        public void Initialize(Tile tile, int height, FloorData floor, DockSupportData support,
            EntityOrientation braceRotation, Material ghostMaterial = null, int? anchorLevel = null)
        {
            Tile = tile;
            Height = height;
            AnchorLevel = anchorLevel ?? ComputeTerrainLevel();
            Floor = floor;
            Support = support;
            BraceRotation = braceRotation;
            _ghostMaterial = ghostMaterial;

            gameObject.layer = LayerMasks.FloorRoofLayer;
            transform.position = new Vector3(tile.X * 4, height * 0.1f, tile.Y * 4);
            transform.rotation = Quaternion.Euler(0, 180, 0);

            ModelLoaded += OnAnyModelLoaded;
            Floor.Model.CreateOrGetModel(OnDeckModelCreated);

            if (Support != null)
            {
                CreateSupport();
            }

            if (!GetComponent<BoxCollider>())
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(-2f, 0.125f, -2f);
                collider.size = new Vector3(4f, 0.25f, 4f);
            }
        }

        private void OnAnyModelLoaded(DynamicModelBehaviour behaviour, GameObject model)
        {
            ApplyVisualState();
        }

        public void Revalidate()
        {
            ValidationErrors = DockSupportResolver.ValidateDock(Tile.Map, this);
            ApplyVisualState();
        }

        // Level visibility anchors to the floor level the dock was painted from, not the terrain
        // below — a pier over deep water keeps the level of the structure it extends.
        public int GetEffectiveLevel()
        {
            return AnchorLevel;
        }

        private int ComputeTerrainLevel()
        {
            return Mathf.RoundToInt((Height - Mathf.Max(Tile.SurfaceHeight, 0)) / 30f);
        }

        public void ApplyLevelRendering(float opacity)
        {
            _levelOpacity = opacity;
            gameObject.SetActive(opacity > 0);
            if (opacity > 0)
            {
                ApplyVisualState();
            }
        }

        private float _levelOpacity = 1f;

        private void ApplyVisualState()
        {
            bool invalid = ValidationErrors.Count > 0;
            if (invalid)
            {
                if (_ghostMaterial == null)
                {
                    return;
                }

                if (_invalidPropertyBlock == null)
                {
                    _invalidPropertyBlock = new MaterialPropertyBlock();
                }

                _invalidPropertyBlock.SetColor(ShaderPropertyIds.BaseColor,
                    new Color(_levelOpacity, 0.2f * _levelOpacity, 0.2f * _levelOpacity, 0.6f));

                foreach (Renderer childRenderer in GetComponentsInChildren<Renderer>())
                {
                    if (!_originalMaterials.ContainsKey(childRenderer))
                    {
                        _originalMaterials[childRenderer] = childRenderer.sharedMaterial;
                    }

                    childRenderer.sharedMaterial = _ghostMaterial;
                    childRenderer.SetPropertyBlock(_invalidPropertyBlock);
                }
            }
            else
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                Color opacityColor = new Color(_levelOpacity, _levelOpacity, _levelOpacity);
                foreach (Renderer childRenderer in GetComponentsInChildren<Renderer>())
                {
                    if (_originalMaterials.TryGetValue(childRenderer, out Material original))
                    {
                        childRenderer.sharedMaterial = original;
                    }

                    propertyBlock.SetColor(ShaderPropertyIds.BaseColor, opacityColor);
                    childRenderer.SetPropertyBlock(propertyBlock);
                }
            }
        }

        private void OnDeckModelCreated(GameObject newModel)
        {
            if (!this)
            {
                Destroy(newModel);
                return;
            }

            if (_deckModel)
            {
                Destroy(_deckModel);
            }

            _deckModel = newModel;
            _deckModel.transform.SetParent(transform, false);
            _deckModel.transform.localRotation = Quaternion.Euler(0, 180, 0);
            _deckModel.transform.localPosition = new Vector3(0, 0, -4);

            OnModelLoadedCallback(_deckModel);
        }

        // Support models live in a counter-rotated root so their local axes match world axes
        // (the dock transform itself is rotated 180 for the deck model convention).
        private void CreateSupport()
        {
            _supportRoot = new GameObject("Support");
            _supportRoot.transform.SetParent(transform, false);
            _supportRoot.transform.localRotation = Quaternion.Euler(0, 180, 0);

            if (Support.Type == DockSupportType.Brace)
            {
                Support.BaseModel.CreateOrGetModel(OnBraceModelCreated);
            }
            else
            {
                Support.BaseModel.CreateOrGetModel(OnPillarBaseCreated);
            }
        }

        private void OnPillarBaseCreated(GameObject baseModel)
        {
            if (!this)
            {
                Destroy(baseModel);
                return;
            }

            if (_supportModel)
            {
                Destroy(_supportModel);
            }

            _supportModel = baseModel;
            _supportModel.transform.SetParent(_supportRoot.transform, false);
            _supportModel.transform.localPosition = new Vector3(0, 0, 4);

            RefreshSupportExtensions();
            ApplyVisualState();
        }

        private void OnBraceModelCreated(GameObject braceModel)
        {
            if (!this)
            {
                Destroy(braceModel);
                return;
            }

            if (_supportModel)
            {
                Destroy(_supportModel);
            }

            _supportModel = braceModel;
            _supportModel.transform.SetParent(_supportRoot.transform, false);
            _supportModel.transform.localPosition = BracePosition();
            _supportModel.transform.localRotation = Quaternion.Euler(0, BraceYaw(), 0);
            ApplyVisualState();
        }

        // The brace model footprint is x 0..4, z -4..0 relative to its origin, so yaw rotation
        // swings it off-tile — each orientation needs a compensating offset to stay on the tile.
        private Vector3 BracePosition()
        {
            switch (BraceRotation)
            {
                case EntityOrientation.Up:
                    return new Vector3(0, 0, 4);
                case EntityOrientation.Left:
                    return new Vector3(4, 0, 4);
                case EntityOrientation.Down:
                    return new Vector3(4, 0, 0);
                default:
                    return Vector3.zero;
            }
        }

        private float BraceYaw()
        {
            switch (BraceRotation)
            {
                case EntityOrientation.Up:
                    return 0;
                case EntityOrientation.Down:
                    return 180;
                case EntityOrientation.Left:
                    return 90;
                default:
                    return 270;
            }
        }

        public void RefreshSupportExtensions()
        {
            if (Support == null || Support.Type == DockSupportType.Brace || !Support.HasExtension)
            {
                return;
            }

            _supportGeneration++;
            foreach (GameObject extension in _extensions)
            {
                if (extension)
                {
                    Destroy(extension);
                }
            }
            _extensions.Clear();

            int extensionCount = GetExtensionCount();
            int generation = _supportGeneration;
            for (int i = 0; i < extensionCount; i++)
            {
                float yOffset = -30f * (i + 1) * 0.1f;
                Support.ExtensionModel.CreateOrGetModel(instance =>
                {
                    if (!this || generation != _supportGeneration)
                    {
                        Destroy(instance);
                        return;
                    }

                    instance.transform.SetParent(_supportRoot.transform, false);
                    instance.transform.localPosition = new Vector3(0, yOffset, 4);
                    _extensions.Add(instance);
                    ApplyVisualState();
                });
            }
        }

        // Base model covers the first 3m; extensions stack below down to the tile's lowest corner.
        private int GetExtensionCount()
        {
            int minCorner = MinCornerHeight();
            int drop = Height - minCorner - 30;
            return Mathf.Max(0, Mathf.CeilToInt(drop / 30f));
        }

        // Charged pillar segments use floor division with a minimum of 1, so the materials
        // total matches the build cost even where the rendered extension count rounds up.
        private int ChargedSegmentCount()
        {
            return Mathf.Max(1, (Height - MinCornerHeight()) / 30);
        }

        private int MinCornerHeight()
        {
            Map map = Tile.Map;
            return Mathf.Min(
                map[Tile.X, Tile.Y].SurfaceHeight,
                map[Tile.X + 1, Tile.Y].SurfaceHeight,
                map[Tile.X, Tile.Y + 1].SurfaceHeight,
                map[Tile.X + 1, Tile.Y + 1].SurfaceHeight);
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("x", Tile.X.ToString());
            localRoot.SetAttribute("y", Tile.Y.ToString());
            localRoot.SetAttribute("height", Height.ToString());
            localRoot.SetAttribute("anchorLevel", AnchorLevel.ToString());
            localRoot.SetAttribute("floor", Floor.ShortName);
            localRoot.SetAttribute("support", Support != null ? Support.ShortName : "none");
            if (Support != null && Support.Type == DockSupportType.Brace)
            {
                localRoot.SetAttribute("braceDir", BraceRotation.ToString().ToUpperInvariant());
            }
        }

        public override string ToString()
        {
            StringBuilder build = new StringBuilder();

            build.Append("X: ").Append(Tile.X).Append(" Y: ").Append(Tile.Y).AppendLine();
            build.Append("Dock · h ").Append(Height);
            if (Support != null)
            {
                build.Append(" · ").Append(Support.Name);
            }

            if (ValidationErrors.Count > 0)
            {
                build.AppendLine();
                build.Append("<color=red>").Append(string.Join(", ", ValidationErrors)).Append("</color>");
            }

            return build.ToString();
        }
    }
}
