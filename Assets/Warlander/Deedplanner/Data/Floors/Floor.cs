using System.Text;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data.Floors
{
    public class Floor : LevelEntity
    {
        private GameObject model;

        public FloorData Data { get; private set; }
        public EntityOrientation Orientation { get; private set; }
        public override Materials Materials => Data.Materials;

        public void Initialize(Tile tile, FloorData data, EntityOrientation orientation)
        {
            Tile = tile;

            gameObject.layer = LayerMasks.FloorRoofLayer;

            Data = data;
            Orientation = orientation;

            Data.Model.CreateOrGetModel(OnModelCreated);

            if (!GetComponent<BoxCollider>())
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(-2f, 0.125f, -2f);
                collider.size = new Vector3(4f, 0.25f, 4f);
            }

            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        private void OnModelCreated(GameObject newModel)
        {
            if (model)
            {
                Destroy(model);
            }

            model = newModel;
            model.transform.SetParent(transform, false);
            if (Orientation == EntityOrientation.Right)
            {
                model.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
            else if (Orientation == EntityOrientation.Down)
            {
                model.transform.localRotation = Quaternion.Euler(0, 180, 0);
                model.transform.localPosition = new Vector3(0, 0, -4);
            }
            else if (Orientation == EntityOrientation.Left)
            {
                model.transform.localRotation = Quaternion.Euler(0, 270, 0);
                model.transform.localPosition = new Vector3(-4, 0, -4);
            }
            else if (Orientation == EntityOrientation.Up)
            {
                model.transform.localPosition = new Vector3(-4, 0, 0);
            }

            OnModelLoadedCallback(model);
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("id", Data.ShortName);
            localRoot.SetAttribute("orientation", Orientation.ToString().ToUpperInvariant());
        }

        public override string ToString()
        {
            StringBuilder build = new StringBuilder();

            build.Append("X: ").Append(Tile.X).Append(" Y: ").Append(Tile.Y).AppendLine();
            build.Append(Data.Name);
            if (Debug.isDebugBuild)
            {
                build.AppendLine();
                build.Append("Orientation = ").Append(Orientation);
            }

            return build.ToString();
        }
    }
}
