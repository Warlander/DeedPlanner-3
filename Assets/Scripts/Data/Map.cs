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
    public class Map : MonoBehaviour, IXMLSerializable
    {

        private Tile[,] tiles;

        private Transform surfaceRoot;
        private Transform caveRoot;
        private Transform surfaceGridRoot;
        private Transform caveGridRoot;

        private int renderedFloor;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Tile this[int x, int y] {
            get {
                return tiles[x, y];
            }
        }

        public int RenderedFloor {
            get {
                return RenderedFloor;
            }
            set {
                renderedFloor = value;

                bool underground = renderedFloor < 0;
                int absoluteFloor = underground ? -renderedFloor + 1 : renderedFloor;

                if (underground)
                {
                    surfaceRoot.gameObject.SetActive(false);
                    surfaceGridRoot.gameObject.SetActive(false);
                    caveRoot.gameObject.SetActive(true);
                    caveGridRoot.gameObject.SetActive(true);

                    caveGridRoot.localPosition = new Vector3(0, absoluteFloor * 3, 0);
                }
                else
                {
                    surfaceRoot.gameObject.SetActive(true);
                    surfaceGridRoot.gameObject.SetActive(true);
                    caveRoot.gameObject.SetActive(false);
                    caveGridRoot.gameObject.SetActive(false);

                    surfaceGridRoot.localPosition = new Vector3(0, absoluteFloor * 3, 0);
                }
            }
        }

        public void Initialize(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new Tile[width + 1, height + 1];

            surfaceRoot = new GameObject("Surface").transform;
            surfaceRoot.SetParent(transform);
            caveRoot = new GameObject("Cave").transform;
            caveRoot.SetParent(transform);
            caveRoot.gameObject.SetActive(false);
            surfaceGridRoot = new GameObject("Surface Grid").transform;
            surfaceGridRoot.SetParent(transform);
            caveGridRoot = new GameObject("Cave Grid").transform;
            caveGridRoot.SetParent(transform);
            caveGridRoot.gameObject.SetActive(false);

            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    GameObject surfaceGridObject = new GameObject("Grid " + i + "X" + i2, typeof(GridTile));
                    surfaceGridObject.transform.SetParent(surfaceGridRoot);
                    surfaceGridObject.transform.localPosition = new Vector3(i * 4, 0, i2 * 4);
                    GridTile surfaceGrid = surfaceGridObject.GetComponent<GridTile>();

                    GameObject surfaceObject = new GameObject("Tile " + i + "X" + i2, typeof(SurfaceTile));
                    surfaceObject.transform.SetParent(surfaceRoot);
                    surfaceObject.transform.localPosition = new Vector3(i * 4, 0, i2 * 4);
                    SurfaceTile surface = surfaceObject.GetComponent<SurfaceTile>();
                    surface.Initialize(surfaceGrid);

                    GameObject caveGridObject = new GameObject("Grid " + i + "X" + i2, typeof(GridTile));
                    caveGridObject.transform.SetParent(caveGridRoot);
                    caveGridObject.transform.localPosition = new Vector3(i * 4, 0, i2 * 4);
                    GridTile caveGrid = caveGridObject.GetComponent<GridTile>();

                    GameObject caveObject = new GameObject("Tile " + i + "X" + i2, typeof(CaveTile));
                    caveObject.transform.SetParent(caveRoot);
                    caveObject.transform.localPosition = new Vector3(i * 4, 0, i2 * 4);
                    CaveTile cave = caveObject.GetComponent<CaveTile>();
                    cave.Initialize(caveGrid);

                    Tile tile = new Tile(this, surface, cave, i, i2);
                    tiles[i, i2] = tile;
                }
            }

            StaticBatchingUtility.Combine(surfaceRoot.gameObject);
            StaticBatchingUtility.Combine(caveRoot.gameObject);
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
