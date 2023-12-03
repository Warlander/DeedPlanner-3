using System.Text;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data.Roofs
{
    public class Roof : TileEntity
    {
        public RoofData Data { get; private set; }
        public override Materials Materials => Data.Materials;

        public int RoofLevel { get; private set; }
        
        private GameObject _model;
        private RoofType currentRoofType;
        private int currentRoofMatch = -1;

        public void Initialize(Tile tile, RoofData data)
        {
            Tile = tile;

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
                return entity && entity.GetType() == typeof(Roof);
            }
            return false;
        }

        public void RecalculateRoofModel()
        {
            int floor = Floor;

            foreach (RoofType type in RoofType.RoofTypes)
            {
                int match = type.CheckMatch(Tile, floor);

                if (currentRoofType == type && currentRoofMatch == match)
                {
                    break;
                }
                if (match != -1)
                {
                    currentRoofType = type;
                    currentRoofMatch = match;

                    type.GetModelForData(Data).CreateOrGetModel(OnModelLoaded);
                    
                    break;
                }
            }
        }

        private void OnModelLoaded(GameObject newModel)
        {
            if (_model)
            {
                Destroy(_model);
            }
            
            _model = newModel;
            _model.transform.SetParent(transform);
            _model.transform.localPosition = new Vector3(-2, RoofLevel * 3.5f, -2);
            if (currentRoofMatch == 0)
            {
                _model.transform.rotation = Quaternion.Euler(0, 90, 0);
            }
            else if (currentRoofMatch == 1)
            {
                _model.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else if (currentRoofMatch == 2)
            {
                _model.transform.rotation = Quaternion.Euler(0, 270, 0);
            }
            else if (currentRoofMatch == 3)
            {
                _model.transform.rotation = Quaternion.Euler(0, 180, 0);
            }

            OnModelLoadedCallback(_model);
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("id", Data.ShortName);
        }
        
        public override string ToString()
        {
            StringBuilder build = new StringBuilder();

            build.Append("X: ").Append(Tile.X).Append(" Y: ").Append(Tile.Y).AppendLine();
            build.Append(Data.Name);
            if (Debug.isDebugBuild)
            {
                build.AppendLine();
                build.Append("Roof level = ").Append(RoofLevel).AppendLine();
                build.Append("Roof match = ").Append(currentRoofMatch).AppendLine();
                if (Model)
                {
                    build.Append("Model = ").Append(Model.name);
                }
            }
            
            return build.ToString();
        }
    }
}
