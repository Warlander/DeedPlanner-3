using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class Map : MonoBehaviour, IXMLSerializable
    {

        private Tile[,] tiles;

        private Transform[] surfaceLevelRoots;
        private Transform[] caveLevelRoots;
        private Transform surfaceGridRoot;
        private Transform caveGridRoot;

        private int renderedFloor;
        private bool renderEntireLayer = true;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public int LowestSurfaceHeight { get; private set; }
        public int HighestSurfaceHeight { get; private set; }

        public int LowestCaveHeight { get; private set; }
        public int HighestCaveHeight { get; private set; }

        public Tile this[int x, int y] {
            get {
                if (x < 0 || y < 0 || x > Width || y > Height)
                {
                    return null;
                }

                return tiles[x, y];
            }
        }

        public int RenderedFloor {
            get {
                return renderedFloor;
            }
            set {
                renderedFloor = value;

                bool underground = renderedFloor < 0;
                int absoluteFloor = underground ? -renderedFloor + 1 : renderedFloor;

                if (underground)
                {
                    for (int i = 0; i < surfaceLevelRoots.Length; i++)
                    {
                        Transform root = surfaceLevelRoots[i];
                        root.gameObject.SetActive(false);
                    }
                    surfaceGridRoot.gameObject.SetActive(false);
                    for (int i = 0; i < caveLevelRoots.Length; i++)
                    {
                        Transform root = caveLevelRoots[i];
                        bool renderFloor = RenderEntireLayer ? true : absoluteFloor == i;
                        root.gameObject.SetActive(renderFloor);
                    }
                    surfaceGridRoot.gameObject.SetActive(false);
                    caveGridRoot.gameObject.SetActive(true);

                    caveGridRoot.localPosition = new Vector3(0, absoluteFloor * 3, 0);
                }
                else
                {
                    for (int i = 0; i < surfaceLevelRoots.Length; i++)
                    {
                        Transform root = surfaceLevelRoots[i];
                        bool renderFloor = RenderEntireLayer ? true : absoluteFloor == i;
                        root.gameObject.SetActive(renderFloor);
                    }
                    surfaceGridRoot.gameObject.SetActive(false);
                    for (int i = 0; i < caveLevelRoots.Length; i++)
                    {
                        Transform root = caveLevelRoots[i];
                        root.gameObject.SetActive(false);
                    }
                    surfaceGridRoot.gameObject.SetActive(true);
                    caveGridRoot.gameObject.SetActive(false);

                    surfaceGridRoot.localPosition = new Vector3(0, absoluteFloor * 3, 0);
                }
            }
        }

        public bool RenderEntireLayer {
            get {
                return renderEntireLayer;
            }
            set {
                renderEntireLayer = value;
                RenderedFloor = renderedFloor;
            }
        }

        public void Initialize(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new Tile[width + 1, height + 1];

            surfaceLevelRoots = new Transform[16];
            for (int i = 0; i < surfaceLevelRoots.Length; i++)
            {
                Transform root = new GameObject("Floor " + (i + 1)).transform;
                root.SetParent(transform);
                surfaceLevelRoots[i] = root;
            }
            caveLevelRoots = new Transform[6];
            for (int i = 0; i < caveLevelRoots.Length; i++)
            {
                Transform root = new GameObject("Floor -" + (i + 1)).transform;
                root.SetParent(transform);
                caveLevelRoots[i] = root;
            }
            surfaceGridRoot = new GameObject("Surface Grid").transform;
            surfaceGridRoot.SetParent(transform);
            caveGridRoot = new GameObject("Cave Grid").transform;
            caveGridRoot.SetParent(transform);
            caveGridRoot.gameObject.SetActive(false);

            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    bool edge = i == Width || i2 == Height;

                    GameObject surfaceGridObject = new GameObject("Grid " + i + "X" + i2, typeof(GridTile));
                    surfaceGridObject.transform.SetParent(surfaceGridRoot);
                    surfaceGridObject.transform.localPosition = new Vector3(i * 4, 0, i2 * 4);
                    GridTile surfaceGrid = surfaceGridObject.GetComponent<GridTile>();

                    GameObject caveGridObject = new GameObject("Grid " + i + "X" + i2, typeof(GridTile));
                    caveGridObject.transform.SetParent(caveGridRoot);
                    caveGridObject.transform.localPosition = new Vector3(i * 4, 0, i2 * 4);
                    GridTile caveGrid = caveGridObject.GetComponent<GridTile>();

                    Tile tile = ScriptableObject.CreateInstance<Tile>();
                    tile.Initialize(this, surfaceGrid, caveGrid, i, i2, edge);
                    tiles[i, i2] = tile;

                    if (edge)
                    {
                        surfaceGridObject.SetActive(false);
                        caveGridObject.SetActive(false);
                    }
                }
            }

            RecalculateHeights();
            RecalculateRoofs();
        }

        public void AddEntityToMap(GameObject entity, int floor)
        {
            bool cave = floor < 0;
            int absoluteFloor = cave ? -floor - 1 : floor;
            if (cave)
            {
                entity.transform.SetParent(caveLevelRoots[absoluteFloor]);
            }
            else
            {
                entity.transform.SetParent(surfaceLevelRoots[absoluteFloor]);
            }
        }

        public void RecalculateHeights()
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            int caveMin = int.MaxValue;
            int caveMax = int.MinValue;

            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    int elevation = this[i, i2].SurfaceHeight;
                    int caveElevation = this[i, i2].CaveHeight;
                    if (elevation > max)
                    {
                        max = elevation;
                    }
                    if (elevation < min)
                    {
                        min = elevation;
                    }
                    if (caveElevation > caveMax)
                    {
                        caveMax = caveElevation;
                    }
                    if (caveElevation < caveMin)
                    {
                        caveMin = caveElevation;
                    }
                }
            }

            LowestSurfaceHeight = min;
            HighestSurfaceHeight = max;
            LowestCaveHeight = caveMin;
            HighestCaveHeight = caveMax;
        }

        public void RecalculateRoofs()
        {
            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    for (int i3 = 0; i3 < Constants.FloorLimit; i3++)
                    {
                        TileEntity entity = this[i, i2].GetTileContent(i3);
                        if (entity != null && entity.GetType() == typeof(Roof))
                        {
                            ((Roof)this[i, i2].GetTileContent(i3)).RecalculateRoofLevel();
                        }
                    }
                }
            }

            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    for (int i3 = 0; i3 < Constants.FloorLimit; i3++)
                    {
                        TileEntity entity = this[i, i2].GetTileContent(i3);
                        if (entity != null && entity.GetType() == typeof(Roof))
                        {
                            ((Roof)this[i, i2].GetTileContent(i3)).RecalculateRoofModel();
                        }
                    }
                }
            }
        }

        public Tile GetRelativeTile(Tile tile, int relativeX, int relativeY)
        {
            int x = tile.X + relativeX;
            int y = tile.Y + relativeY;

            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return null;
            }

            return this[x, y];
        }

        public float GetInterpolatedHeight(float x, float y)
        {
            Ray ray = new Ray(new Vector3(x, 10000, y), new Vector3(0, -1, 0));
            RaycastHit raycastHit;
            int mask = LayerMasks.GroundMask;
            bool hit = Physics.Raycast(ray, out raycastHit, 20000, mask);
            if (hit)
            {
                return raycastHit.point.y;
            }
            return 0;
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
