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
    public class CaveData : TileEntity
    {

        public string Name { get; private set; }
        public string ShortName { get; private set; }

        public TextureReference Texture { get; private set; }

        public bool Wall { get; private set; }
        public bool Show { get; private set; }
        public bool Entrance { get; private set; }

        public override Materials Materials {
            get {
                return null;
            }
        }

        public CaveData(TextureReference texture, string name, string shortName, bool wall, bool show, bool entrance)
        {
            Texture = texture;
            Name = name;
            ShortName = shortName;
            Wall = wall;
            Show = show;
            Entrance = entrance;
        }

        public override string ToString()
        {
            return Name;
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            
        }
    }
}
