using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text.RegularExpressions;
using UnityEngine;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner.Data.Summary;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;
using Zenject;

namespace Warlander.Deedplanner.Data
{
    public class Map : MonoBehaviour, IXmlSerializable, IEnumerable<Tile>
    {
        [Inject] private IInstantiator _instantiator;
        
        private Tile[,] tiles;

        private Transform[] surfaceLevelRoots;
        private Transform[] caveLevelRoots;
        private Transform surfaceGridRoot;
        private Transform caveGridRoot;

        private int renderedFloor;
        private bool renderEntireMap = true;
        private bool renderGrid = true;

        public GroundMesh Ground { get; private set; }

        public GridMesh SurfaceGridMesh { get; private set; }
        public GridMesh CaveGridMesh { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int VisibleTilesCount => Width * Height;
        public int AllTilesCount => (Width + 1) * (Height + 1);
        public string OriginalExporter { get; private set; } = Constants.TitleString;
        public Version OriginalExporterVersion { get; private set; }

        public int LowestSurfaceHeight { get; private set; }
        public int HighestSurfaceHeight { get; private set; }

        public int LowestCaveHeight { get; private set; }
        public int HighestCaveHeight { get; private set; }

        public CommandManager CommandManager { get; set; } = new CommandManager(100);

        public Transform PlaneLineRoot { get; private set; }

        private List<Bridge> bridges = new List<Bridge>();
        
        private bool needsRoofUpdate = false;

        private bool renderDecorations = true;
        private bool renderTrees = true;
        private bool renderBushes = true;
        private bool renderShips = true;

        public bool RenderDecorations
        {
            get => renderDecorations;
            set
            {
                if (renderDecorations == value)
                {
                    return;
                }
                renderDecorations = value;
                RefreshAllTiles();
            }
        }

        public bool RenderTrees
        {
            get => renderTrees;
            set
            {
                if (renderTrees == value)
                {
                    return;
                }
                renderTrees = value;
                RefreshAllTiles();
            }
        }

        public bool RenderBushes
        {
            get => renderBushes;
            set
            {
                if (renderBushes == value)
                {
                    return;
                }
                renderBushes = value;
                RefreshAllTiles();
            }
        }

        public bool RenderShips
        {
            get => renderShips;
            set
            {
                if (renderShips == value)
                {
                    return;
                }

                renderShips = value;
                RefreshAllTiles();
            }
        }

        public Tile this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0 || x > Width || y > Height)
                {
                    return null;
                }

                return tiles[x, y];
            }
        }

        public Tile this[Vector2Int v] => this[v.x, v.y];

        public int RenderedFloor
        {
            get => renderedFloor;
            set
            {
                renderedFloor = value;
                UpdateFloorsRendering();
            }
        }

        public bool RenderEntireMap
        {
            get => renderEntireMap;
            set
            {
                renderEntireMap = value;
                UpdateFloorsRendering();
            }
        }

        public bool RenderGrid
        {
            get => renderGrid;
            set
            {
                renderGrid = value;
                UpdateFloorsRendering();
            }
        }

        public void Initialize(Map originalMap, int addLeft, int addRight, int addBottom, int addTop)
        {
            int finalWidth = originalMap.Width + addLeft + addRight;
            int finalHeight = originalMap.Height + addTop + addBottom;
            PreInitialize(finalWidth, finalHeight);

            int pasteBeginX = Math.Max(0, addLeft);
            int pasteBeginY = Math.Max(0, addBottom);
            int pasteEndX = Math.Min(Width, originalMap.Width + addLeft);
            int pasteEndY = Math.Min(Height, originalMap.Height + addBottom);

            for (int x = pasteBeginX; x <= pasteEndX; x++)
            {
                for (int y = pasteBeginY; y <= pasteEndY; y++)
                {
                    int copyX = x - addLeft;
                    int copyY = y - addBottom;

                    this[x, y].PasteTile(originalMap[copyX, copyY]);
                }
            }

            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    this[i, i2].Refresh();
                    RecalculateSurfaceHeight(i, i2);
                }
            }

            RecalculateHeights();
            RecalculateRoofs();
            CommandManager.ForgetAction();
        }

        public void Initialize(int width, int height)
        {
            PreInitialize(width, height);

            RecalculateHeights();
            RecalculateRoofs();
            CommandManager.ForgetAction();
        }

        public void Initialize(XmlDocument document)
        {
            XmlElement mapRoot = document.DocumentElement;
            if (mapRoot == null || mapRoot.LocalName != "map")
            {
                PreInitialize(25, 25);
                return;
            }

            OriginalExporter = mapRoot.GetAttribute("exporter");
            Regex versionRegex = new Regex(@"\d+\.\d+\.\d+");
            Match versionMatch = versionRegex.Match(OriginalExporter);
            if (versionMatch.Value != "")
            {
                OriginalExporterVersion = new Version(versionMatch.Value);
            }

            int width = Convert.ToInt32(mapRoot.GetAttribute("width"));
            int height = Convert.ToInt32(mapRoot.GetAttribute("height"));
            PreInitialize(width, height);

            XmlNodeList tilesList = mapRoot.GetElementsByTagName("tile");
            foreach (XmlElement tileElement in tilesList)
            {
                int x = Convert.ToInt32(tileElement.GetAttribute("x"));
                int y = Convert.ToInt32(tileElement.GetAttribute("y"));
                if (x < 0 || x > Width || y < 0 || y > Height)
                {
                    continue;
                }

                this[x, y].DeserializeHeightmap(tileElement);

                int surfaceHeight = this[x, y].SurfaceHeight;
                int caveHeight = this[x, y].CaveHeight;
                SurfaceGridMesh.SetHeight(x, y, surfaceHeight);
                CaveGridMesh.SetHeight(x, y, caveHeight);
            }

            foreach (XmlElement tileElement in tilesList)
            {
                int x = Convert.ToInt32(tileElement.GetAttribute("x"));
                int y = Convert.ToInt32(tileElement.GetAttribute("y"));
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    continue;
                }

                this[x, y].DeserializeEntities(tileElement);
            }
            
            XmlNodeList bridgesList = mapRoot.GetElementsByTagName("bridge");
            foreach (XmlElement bridgeElement in bridgesList)
            {
                Bridge bridge = new Bridge(this, bridgeElement);
                bridges.Add(bridge);
            }

            Ground.UpdateNow();

            RefreshAllTiles();

            RecalculateHeights();
            RecalculateRoofs();
            CommandManager.ForgetAction();
        }

        private void PreInitialize(int width, int height)
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

            OverlayMesh surfaceOverlayMesh = 
                _instantiator.InstantiatePrefabForComponent<OverlayMesh>(GameManager.Instance.OverlayMeshPrefab, transform);
            
            surfaceGridRoot = surfaceOverlayMesh.transform;

            GameObject groundObject = new GameObject("Ground Mesh", typeof(GroundMesh));
            Ground = groundObject.GetComponent<GroundMesh>();
            Ground.Initialize(width, height, surfaceOverlayMesh);
            surfaceOverlayMesh.Initialize(Ground.ColliderMesh);
            AddEntityToMap(groundObject, 0);

            caveGridRoot = new GameObject("Cave Grid").transform;
            caveGridRoot.SetParent(transform);
            caveGridRoot.gameObject.SetActive(false);
            PlaneLineRoot = new GameObject("Plane Lines").transform;

            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    Tile tile = new Tile(this, i, i2);
                    tiles[i, i2] = tile;
                }
            }

            SurfaceGridMesh = PrepareGridMesh("Surface grid", surfaceGridRoot, false);
            CaveGridMesh = PrepareGridMesh("Cave grid", caveGridRoot, true);

            RenderGrid = LayoutManager.Instance.CurrentTab != Tab.Menu;
            CommandManager.ForgetAction();
        }

        private void LateUpdate()
        {
            if (needsRoofUpdate)
            {
                needsRoofUpdate = false;
                RecalculateRoofsInternal();
            }
        }

        private void RefreshAllTiles()
        {
            foreach (Tile tile in tiles)
            {
                tile.Refresh();
            }
        }

        private void RecalculateRoofsInternal()
        {
            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    for (int i3 = 0; i3 < Constants.FloorLimit; i3++)
                    {
                        TileEntity entity = this[i, i2].GetTileContent(i3);
                        if (entity && entity.GetType() == typeof(Roof))
                        {
                            ((Roof) this[i, i2].GetTileContent(i3)).RecalculateRoofLevel();
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
                        if (entity && entity.GetType() == typeof(Roof))
                        {
                            ((Roof) this[i, i2].GetTileContent(i3)).RecalculateRoofModel();
                        }
                    }
                }
            }
        }

        private GridMesh PrepareGridMesh(string name, Transform parent, bool cave)
        {
            GridMesh gridMesh = _instantiator.InstantiateComponentOnNewGameObject<GridMesh>(name);
            gridMesh.Initialize(this, cave);
            gridMesh.transform.SetParent(parent);
            gridMesh.transform.localPosition = new Vector3(0, 0.01f, 0);

            return gridMesh;
        }

        public Materials CalculateMapMaterials()
        {
            Materials mapMaterials = new Materials();

            foreach (Tile tile in tiles)
            {
                mapMaterials.Add(tile.CalculateTileMaterials(TilePart.Everything));
            }

            return mapMaterials;
        }

        public int CoordinateToIndex(int x, int y)
        {
            return x * (Height + 1) + y;
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

        public void RecalculateSurfaceHeight(int x, int y)
        {
            int elevation = this[x, y].SurfaceHeight;
            if (elevation > HighestSurfaceHeight)
            {
                HighestSurfaceHeight = elevation;
            }

            if (elevation < LowestSurfaceHeight)
            {
                LowestSurfaceHeight = elevation;
            }

            SurfaceGridMesh.SetHeight(x, y, elevation);
        }

        public void RecalculateCaveHeight(int x, int y)
        {
            int caveElevation = this[x, y].CaveHeight;
            if (caveElevation > HighestCaveHeight)
            {
                HighestCaveHeight = caveElevation;
            }

            if (caveElevation < LowestCaveHeight)
            {
                LowestCaveHeight = caveElevation;
            }

            CaveGridMesh.SetHeight(x, y, caveElevation);
        }

        public void RecalculateRoofs()
        {
            needsRoofUpdate = true;
        }

        public Tile GetRelativeTile(Tile tile, int relativeX, int relativeY)
        {
            int x = tile.X + relativeX;
            int y = tile.Y + relativeY;

            if (x < 0 || x > Width || y < 0 || y > Height)
            {
                return null;
            }

            return this[x, y];
        }

        public float GetInterpolatedHeight(float x, float y)
        {
            Ray ray = new Ray(new Vector3(x, 10000, y), new Vector3(0, -1, 0));
            RaycastHit raycastHit;
            const int mask = LayerMasks.GroundMask;
            bool hit = Physics.Raycast(ray, out raycastHit, 20000, mask);
            if (hit)
            {
                return raycastHit.point.y;
            }

            return 0;
        }

        public IEnumerator<Tile> GetEnumerator()
        {
            return tiles.Cast<Tile>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            // localRoot for map is always null at start, we create it
            localRoot = document.CreateElement("map");
            localRoot.SetAttribute("width", Width.ToString());
            localRoot.SetAttribute("height", Height.ToString());
            localRoot.SetAttribute("exporter", Constants.TitleString);
            document.AppendChild(localRoot);

            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    XmlElement tile = document.CreateElement("tile");
                    tiles[i, i2].Serialize(document, tile);
                    localRoot.AppendChild(tile);
                }
            }
        }

        public float GetRelativeFloorOpacity(int relativeFloor)
        {
            if (relativeFloor == 0)
            {
                return 1;
            }
            else if (relativeFloor == -1)
            {
                return 0.6f;
            }
            else if (relativeFloor == -2)
            {
                return 0.25f;
            }

            throw new ArgumentOutOfRangeException(
                "Relative floor opacity is supported only for values from -2 to 0, supplied value: " + relativeFloor);
        }

        private void UpdateFloorsRendering()
        {
            bool underground = renderedFloor < 0;
            int absoluteFloor = underground ? -renderedFloor + 1 : renderedFloor;

            if (underground)
            {
                foreach (Transform root in surfaceLevelRoots)
                {
                    root.gameObject.SetActive(false);
                }

                for (int i = 0; i < caveLevelRoots.Length; i++)
                {
                    Transform root = caveLevelRoots[i];
                    RefreshLevelRendering(root, i - absoluteFloor);
                }

                surfaceGridRoot.gameObject.SetActive(false);
                caveGridRoot.gameObject.SetActive(renderGrid);

                caveGridRoot.localPosition = new Vector3(0, absoluteFloor * 3, 0);
            }
            else
            {
                foreach (Transform root in caveLevelRoots)
                {
                    root.gameObject.SetActive(false);
                }

                for (int i = 0; i < surfaceLevelRoots.Length; i++)
                {
                    Transform root = surfaceLevelRoots[i];
                    RefreshLevelRendering(root, i - absoluteFloor);
                }

                surfaceGridRoot.gameObject.SetActive(renderGrid);
                caveGridRoot.gameObject.SetActive(false);

                surfaceGridRoot.localPosition = new Vector3(0, absoluteFloor * 3 + 0.01f, 0);
            }
        }

        private void RefreshLevelRendering(Transform root, int relativeFloor)
        {
            bool renderFloor = RenderEntireMap || (relativeFloor <= 0 && relativeFloor > -3);
            root.gameObject.SetActive(renderFloor);
            if (renderFloor)
            {
                float opacity = RenderEntireMap ? 1f : GetRelativeFloorOpacity(relativeFloor);
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor(ShaderPropertyIds.Color, new Color(opacity, opacity, opacity));
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }
    }
}
