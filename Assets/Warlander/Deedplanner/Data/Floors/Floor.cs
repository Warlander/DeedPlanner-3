using System.Text;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data.Floors
{
    public class Floor : TileEntity
    {
        
        private GameObject model;

        public FloorData Data { get; private set; }
        public FloorOrientation Orientation { get; private set; }
        public override Materials Materials => Data.Materials;

        public void Initialize(Tile tile, FloorData data, FloorOrientation orientation)
        {
            Tile = tile;

            gameObject.layer = LayerMasks.FloorRoofLayer;

            Data = data;
            Orientation = orientation;

            CoroutineManager.Instance.QueueCoroutine(Data.Model.CreateOrGetModel(OnModelCreated));

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
            if (Orientation == FloorOrientation.Right)
            {
                model.transform.localRotation = Quaternion.Euler(0, 90, 0);
                model.transform.localPosition = new Vector3(0, 0, -4);
            }
            else if (Orientation == FloorOrientation.Down)
            {
                model.transform.localRotation = Quaternion.Euler(0, 180, 0);
                model.transform.localPosition = new Vector3(-4, 0, -4);
            }
            else if (Orientation == FloorOrientation.Left)
            {
                model.transform.localRotation = Quaternion.Euler(0, 270, 0);
                model.transform.localPosition = new Vector3(-4, 0, 0);
            }
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
            
            return build.ToString();
        }
    }
}
