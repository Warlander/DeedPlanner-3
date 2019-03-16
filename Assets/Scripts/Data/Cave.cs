using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class Cave : MonoBehaviour, IXMLSerializable
    {

        private CaveData data;

        public CaveData Data {
            get {
                return data;
            }
            set {
                data = value;
            }
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
