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
        public SurfaceTile Surface { get; private set; }
        public CaveTile Cave { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        public Tile(Map map, SurfaceTile surface, CaveTile cave, int x, int y)
        {
            Map = map;
            Surface = surface;
            Cave = cave;
            X = x;
            Y = y;
        }

        public BasicTile GetTileForFloor(int floor)
        {
            if (floor < 0)
            {
                return Cave;
            }
            else
            {
                return Surface;
            }
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {

        }

    }
}
