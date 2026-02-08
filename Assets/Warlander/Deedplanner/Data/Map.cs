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
using Warlander.Deedplanner.Features;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Utils;
using Zenject;

namespace Warlander.Deedplanner.Data
{
    public class Map : MonoBehaviour, IXmlSerializable, IEnumerable<Tile>
    {
        [Inject] private IInstantiator _instantiator;
        [Inject] private GameManager _gameManager;
        [Inject] private TileFactory _tileFactory;
        [Inject] private BridgeFactory _bridgeFactory;
        [Inject] private IMapRenderSettingsRetriever _mapRenderSettingsRetriever;
        [Inject] private IFeatureStateRetriever _featureStateRetriever;
        [Inject] private MapHeightTracker _heightTracker;
        [Inject] private MapRoofCalculator _roofCalculator;

        public GroundMesh Ground { get; private set; }

        public GridMesh SurfaceGridMesh { get; private set; }
        public GridMesh CaveGridMesh { get; private set; }

        public int Width => _tileGrid.Width;
        public int Height => _tileGrid.Height;
        public int VisibleTilesCount => _tileGrid.VisibleTilesCount;
        public int AllTilesCount => _tileGrid.AllTilesCount;
        public string OriginalExporter { get; private set; } = Constants.TitleString;
        public Version OriginalExporterVersion { get; private set; }

        public int LowestSurfaceHeight => _heightTracker.LowestSurfaceHeight;
        public int HighestSurfaceHeight => _heightTracker.HighestSurfaceHeight;
        public int LowestCaveHeight => _heightTracker.LowestCaveHeight;
        public int HighestCaveHeight => _heightTracker.HighestCaveHeight;

        public bool RenderDecorations => _mapRenderSettingsRetriever.RenderDecorations;
        public bool RenderTrees => _mapRenderSettingsRetriever.RenderTrees;
        public bool RenderBushes => _mapRenderSettingsRetriever.RenderBushes;
        public bool RenderShips => _mapRenderSettingsRetriever.RenderShips;

        public CommandManager CommandManager { get; set; } = new CommandManager(100);
        public IReadOnlyList<Bridge> Bridges => _bridgesController.Bridges;

        public Transform PlaneLineRoot { get; private set; }

        private MapTileGrid _tileGrid;
        private MapLevelRenderer _levelRenderer;
        private MapBridgesController _bridgesController;

        private Transform[] _surfaceLevelRoots;
        private Transform[] _caveLevelRoots;
        private Transform _surfaceGridRoot;
        private Transform _caveGridRoot;

        public Tile this[int x, int y] => _tileGrid[x, y];

        public Tile this[Vector2Int v] => _tileGrid[v.x, v.y];

        public int RenderedLevel
        {
            get => _levelRenderer.RenderedLevel;
            set => _levelRenderer.RenderedLevel = value;
        }

        public bool RenderEntireMap
        {
            get => _levelRenderer.RenderEntireMap;
            set => _levelRenderer.RenderEntireMap = value;
        }

        public bool RenderGrid
        {
            get => _levelRenderer.RenderGrid;
            set => _levelRenderer.RenderGrid = value;
        }

        private void Start()
        {
            _mapRenderSettingsRetriever.Changed += GameManagerOnRenderSettingsChanged;
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
                    
                    int surfaceHeight = this[x, y].SurfaceHeight;
                    int caveHeight = this[x, y].CaveHeight;
                    SurfaceGridMesh.SetHeight(x, y, surfaceHeight);
                    CaveGridMesh.SetHeight(x, y, caveHeight);
                }
            }

            _bridgesController.InitializeBridgesAfterResize(originalMap, addLeft, addBottom);

            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    this[i, i2].Refresh();
                    RecalculateSurfaceHeight(i, i2);
                }
            }

            Ground.UpdateNow();
            
            RefreshAllTiles();
            
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

            _bridgesController.InitializeBridges(mapRoot);

            Ground.UpdateNow();

            RefreshAllTiles();

            RecalculateHeights();
            RecalculateRoofs();
            CommandManager.ForgetAction();
        }

        private void PreInitialize(int width, int height)
        {
            _tileGrid = new MapTileGrid(width, height);
            _bridgesController = new MapBridgesController(this, _bridgeFactory, _featureStateRetriever);

            _surfaceLevelRoots = new Transform[16];
            for (int i = 0; i < _surfaceLevelRoots.Length; i++)
            {
                Transform root = new GameObject("Level " + (i + 1)).transform;
                root.SetParent(transform);
                _surfaceLevelRoots[i] = root;
            }

            _caveLevelRoots = new Transform[6];
            for (int i = 0; i < _caveLevelRoots.Length; i++)
            {
                Transform root = new GameObject("Level -" + (i + 1)).transform;
                root.SetParent(transform);
                _caveLevelRoots[i] = root;
            }

            OverlayMesh surfaceOverlayMesh = 
                _instantiator.InstantiatePrefabForComponent<OverlayMesh>(_gameManager.OverlayMeshPrefab, transform);
            
            _surfaceGridRoot = surfaceOverlayMesh.transform;

            _caveGridRoot = new GameObject("Cave Grid").transform;
            _caveGridRoot.SetParent(transform);
            _caveGridRoot.gameObject.SetActive(false);
            PlaneLineRoot = new GameObject("Plane Lines").transform;

            _levelRenderer = new MapLevelRenderer();
            _levelRenderer.Initialize(_surfaceLevelRoots, _caveLevelRoots, _surfaceGridRoot, _caveGridRoot, () => _bridgesController.Bridges);

            GameObject groundObject = new GameObject("Ground Mesh", typeof(GroundMesh));
            Ground = groundObject.GetComponent<GroundMesh>();
            Ground.Initialize(width, height, surfaceOverlayMesh);
            surfaceOverlayMesh.Initialize(Ground.ColliderMesh);
            AddEntityToMap(groundObject, 0);

            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    Tile tile = _tileFactory.CreateTile(this, i, i2);
                    _tileGrid.SetTile(i, i2, tile);
                }
            }

            SurfaceGridMesh = PrepareGridMesh("Surface grid", _surfaceGridRoot, false);
            CaveGridMesh = PrepareGridMesh("Cave grid", _caveGridRoot, true);

            _heightTracker.SetCurrentMap(this);
            _roofCalculator.SetCurrentMap(this);

            RenderGrid = LayoutManager.Instance.CurrentTab != Tab.Menu;
            CommandManager.ForgetAction();
        }

        private void RefreshAllTiles()
        {
            foreach (Tile tile in _tileGrid)
            {
                tile.Refresh();
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

            foreach (Tile tile in _tileGrid)
            {
                mapMaterials.Add(tile.CalculateTileMaterials(TilePart.Everything));
            }

            return mapMaterials;
        }

        public int CoordinateToIndex(int x, int y)
        {
            return _tileGrid.CoordinateToIndex(x, y);
        }

        public void AddEntityToMap(GameObject entity, int level)
        {
            _levelRenderer.AddEntityToMap(entity, level);
        }

        public void RecalculateSurfaceHeight(int x, int y)
        {
            _heightTracker.RecalculateSurfaceHeight(x, y);
        }

        public void RecalculateCaveHeight(int x, int y)
        {
            _heightTracker.RecalculateCaveHeight(x, y);
        }

        public void RecalculateRoofs()
        {
            _roofCalculator.ScheduleRecalculation();
        }

        public Tile GetRelativeTile(Tile tile, int relativeX, int relativeY)
        {
            return _tileGrid.GetRelativeTile(tile, relativeX, relativeY);
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
            return _tileGrid.GetEnumerator();
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
                    _tileGrid[i, i2].Serialize(document, tile);
                    localRoot.AppendChild(tile);
                }
            }
        }

        public float GetRelativeLevelOpacity(int relativeLevel) => _levelRenderer.GetRelativeLevelOpacity(relativeLevel);
        
        private void GameManagerOnRenderSettingsChanged()
        {
            RefreshAllTiles();
        }

        private void RecalculateHeights()
        {
            _heightTracker.RecalculateHeights();
        }

        private void OnDestroy()
        {
            _mapRenderSettingsRetriever.Changed -= GameManagerOnRenderSettingsChanged;
            _heightTracker.ClearCurrentMap();
            _roofCalculator.ClearCurrentMap();
        }
    }
}
