using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class Tile : IXMLSerializable
    {

        public Map Map { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        public Ground Ground { get; private set; }

        public Tile(Map map, int x, int y)
        {
            Map = map;
            X = x;
            Y = y;

            Ground = new Ground(Data.Grounds["gr"]);
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            
        }
    }
}
