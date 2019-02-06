using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class Tile : MonoBehaviour, IXMLSerializable
    {

        public Map Map { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        public int Height { get; private set; }

        public Ground Ground { get; private set; }
        private Dictionary<EntityData, ITileEntity> entities;

        public void Initialize(Map map, int x, int y)
        {
            Map = map;
            X = x;
            Y = y;

            entities = new Dictionary<EntityData, ITileEntity>();

            GameObject groundObject = new GameObject("Ground", typeof(Ground));
            groundObject.transform.SetParent(transform);
            groundObject.transform.localPosition = Vector3.zero;
            Ground = groundObject.GetComponent<Ground>();
            Ground.Initialize(Data.Grounds["gr"]);
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            
        }
    }
}
