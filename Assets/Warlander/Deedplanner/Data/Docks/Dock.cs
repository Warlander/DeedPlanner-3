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
