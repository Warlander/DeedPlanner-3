using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data.Roof
{
    public class Roof : TileEntity
    {

        private Tile tile;

        public RoofData Data { get; private set; }
        public override Materials Materials => Data.Materials;

        public int RoofLevel { get; private set; }
        public GameObject Model { get; private set; }

        public override Tile Tile => tile;

        public void Initialize(Tile tile, RoofData data)
        {
            this.tile = tile;

            gameObject.layer = LayerMasks.FloorRoofLayer;

            Data = data;

            if (!GetComponent<BoxCollider>())
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(-2f, 0.125f, -2f);
                collider.size = new Vector3(4f, 0.25f, 4f);
            }

            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        public void RecalculateRoofLevel()
        {
            int floor = Floor;
            int currRadius = 1;
            Map map = Tile.Map;

            while (true)
            {
                for (int i = -currRadius; i <= currRadius; i++)
                {
                    for (int i2 = -currRadius; i2 <= currRadius; i2++)
                    {
                        if (!ContainsRoof(map.GetRelativeTile(Tile, i, i2), floor)) goto outOfLoop;
                    }
                }
                currRadius++;
            }
        outOfLoop:
            RoofLevel = currRadius - 1;
        }

        private bool ContainsRoof(Tile t, int floor)
        {
            if (t != null)
            {
                TileEntity entity = t.GetTileContent(floor);
                return entity != null && entity.GetType() == typeof(Roof);
            }
            return false;
        }

        public void RecalculateRoofModel()
        {
            int floor = Floor;

            foreach (RoofType type in RoofType.RoofTypes)
            {
                int match = type.CheckMatch(Tile, floor);
                if (match != -1)
                {
                    if (Model)
                    {
                        Destroy(Model);
                    }
                    Model = type.GetModelForData(Data).CreateOrGetModel();
                    Model.transform.SetParent(transform, true);
                    Model.transform.localPosition = new Vector3(-2, RoofLevel * 3.5f, -2);
                    if (match == 0)
                    {
                        Model.transform.rotation = Quaternion.Euler(0, 90, 0);
                    }
                    else if (match == 1)
                    {
                        Model.transform.rotation = Quaternion.Euler(0, 0, 0);
                    }
                    else if (match == 2)
                    {
                        Model.transform.rotation = Quaternion.Euler(0, 270, 0);
                    }
                    else if (match == 3)
                    {
                        Model.transform.rotation = Quaternion.Euler(0, 180, 0);
                    }
                    return;
                }
            }

            if (Model)
            {
                Destroy(Model);
            }
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            
        }
    }
}
