using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data.Docks
{
    public class Dock : TileEntity
    {
        private GameObject _deckModel;
        private GameObject _supportRoot;
        private GameObject _supportModel;
        private readonly List<GameObject> _extensions = new List<GameObject>();
        private int _supportGeneration;

        public int Height { get; private set; }
        public FloorData Floor { get; private set; }
        public DockSupportData Support { get; private set; }
        public EntityOrientation BraceRotation { get; private set; }
        public override Materials Materials => Floor.Materials;

        public void Initialize(Tile tile, int height, FloorData floor, DockSupportData support, EntityOrientation braceRotation)
        {
            Tile = tile;
            Height = height;
            Floor = floor;
            Support = support;
            BraceRotation = braceRotation;

            gameObject.layer = LayerMasks.FloorRoofLayer;
            transform.position = new Vector3(tile.X * 4, height * 0.1f, tile.Y * 4);
            transform.rotation = Quaternion.Euler(0, 180, 0);

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

        private void OnDeckModelCreated(GameObject newModel)
        {
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
            if (_supportModel)
            {
                Destroy(_supportModel);
            }

            _supportModel = baseModel;
            _supportModel.transform.SetParent(_supportRoot.transform, false);
            _supportModel.transform.localPosition = new Vector3(0, 0, 4);

            RefreshSupportExtensions();
        }

        private void OnBraceModelCreated(GameObject braceModel)
        {
            if (_supportModel)
            {
                Destroy(_supportModel);
            }

            _supportModel = braceModel;
            _supportModel.transform.SetParent(_supportRoot.transform, false);
            _supportModel.transform.localPosition = new Vector3(0, 0, 4);
            _supportModel.transform.localRotation = Quaternion.Euler(0, BraceYaw(), 0);
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
                    if (generation != _supportGeneration)
                    {
                        Destroy(instance);
                        return;
                    }

                    instance.transform.SetParent(_supportRoot.transform, false);
                    instance.transform.localPosition = new Vector3(0, yOffset, 4);
                    _extensions.Add(instance);
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

            return build.ToString();
        }
    }
}
