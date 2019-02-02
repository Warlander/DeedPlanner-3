using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class Map : IXMLSerializable
    {

        public int Width { get; private set; }
        public int Height { get; private set; }

        private readonly Tile[,] tiles;

        public Tile this[int x, int y] {
            get {
                return tiles[x, y];
            }
        }

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new Tile[width, height];
            for (int i = 0; i < Width; i++)
            {
                for (int i2 = 0; i2 < Height; i2++)
                {
                    tiles[i, i2] = new Tile(this, i, i2);
                }
            }
        }

        public Tile getRelativeTile(Tile tile, int relativeX, int relativeY)
        {
            int x = tile.X + relativeX;
            int y = tile.Y + relativeY;

            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return null;
            }

            return this[x, y];
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            // localRoot for map is always null at start
            localRoot = document.CreateElement("map");
            localRoot.SetAttribute("width", Width.ToString());
            localRoot.SetAttribute("height", Height.ToString());
            localRoot.SetAttribute("exporter", Constants.TitleString);
            document.AppendChild(localRoot);

            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    tiles[i, i2].Serialize(document, localRoot);
                }
            }
        }
    }
}
