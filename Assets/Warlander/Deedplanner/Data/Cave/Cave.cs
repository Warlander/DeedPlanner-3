using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Cave
{
    public class Cave : MonoBehaviour, IXMLSerializable
    {

        private CaveData data;

        public CaveData Data {
            get => data;
            set => data = value;
        }

        public void Initialize(CaveData data)
        {
            gameObject.layer = LayerMasks.GroundLayer;
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            
        }
    }
}
