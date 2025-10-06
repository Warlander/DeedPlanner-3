﻿using System;
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
        [Inject] private GameManager _gameManager;
        [Inject] private TileFactory _tileFactory;
        [Inject] private BridgeFactory _bridgeFactory;

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

        public bool RenderDecorations => _gameManager.RenderDecorations;
        public bool RenderTrees => _gameManager.RenderTrees;
        public bool RenderBushes => _gameManager.RenderBushes;
        public bool RenderShips => _gameManager.RenderShips;

        public CommandManager CommandManager { get; set; } = new CommandManager(100);
        
        public Transform PlaneLineRoot { get; private set; }

        private Tile[,] _tiles;
        private List<Bridge> _bridges = new List<Bridge>();

        private Transform[] _surfaceLevelRoots;
        private Transform[] _caveLevelRoots;
        private Transform _surfaceGridRoot;
        private Transform _caveGridRoot;

        private int _renderedLevel;
        private bool _renderEntireMap = true;
        private bool _renderGrid = true;
        
        private bool _needsRoofUpdate = false;

        public Tile this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0 || x > Width || y > Height)
                {
                    return null;
                }

                return _tiles[x, y];
            }
        }

        public Tile this[Vector2Int v] => this[v.x, v.y];

        public int RenderedLevel
        {
            get => _renderedLevel;
            set
            {
                _renderedLevel = value;
                UpdateLevelsRendering();
            }
        }

        public bool RenderEntireMap
        {
            get => _renderEntireMap;
            set
            {
                _renderEntireMap = value;
                UpdateLevelsRendering();
            }
        }

        public bool RenderGrid
        {
            get => _renderGrid;
            set
            {
                _renderGrid = value;
                UpdateLevelsRendering();
            }
        }

        private void Start()
        {
            _gameManager.RenderSettingsChanged += GameManagerOnRenderSettingsChanged;
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

            Vector2Int bridgeShift = new Vector2Int(addLeft, addBottom);
            
            foreach (Bridge originalMapBridge in originalMap._bridges)
            {
                Vector2Int firstTileAfterShift = originalMapBridge.FirstTile + bridgeShift;
                Vector2Int secondTileAfterShift = originalMapBridge.SecondTile + bridgeShift;

                if (IsWithinBounds(firstTileAfterShift) && IsWithinBounds(secondTileAfterShift))
                {
                    Bridge movedBridge = _bridgeFactory.CreateBridge(this, originalMapBridge, bridgeShift);
                    _bridges.Add(movedBridge);
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
            
            // XmlNodeList bridgesList = mapRoot.GetElementsByTagName("bridge");
            // foreach (XmlElement bridgeElement in bridgesList)
            // {
            //     Bridge bridge = _bridgeFactory.CreateBridge(this, bridgeElement);
            //     _bridges.Add(bridge);
            // }

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
            _tiles = new Tile[width + 1, height + 1];

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

            GameObject groundObject = new GameObject("Ground Mesh", typeof(GroundMesh));
            Ground = groundObject.GetComponent<GroundMesh>();
            Ground.Initialize(width, height, surfaceOverlayMesh);
            surfaceOverlayMesh.Initialize(Ground.ColliderMesh);
            AddEntityToMap(groundObject, 0);

            _caveGridRoot = new GameObject("Cave Grid").transform;
            _caveGridRoot.SetParent(transform);
            _caveGridRoot.gameObject.SetActive(false);
            PlaneLineRoot = new GameObject("Plane Lines").transform;

            for (int i = 0; i <= Width; i++)
            {
                for (int i2 = 0; i2 <= Height; i2++)
                {
                    Tile tile = _tileFactory.CreateTile(this, i, i2);
                    _tiles[i, i2] = tile;
                }
            }

            SurfaceGridMesh = PrepareGridMesh("Surface grid", _surfaceGridRoot, false);
            CaveGridMesh = PrepareGridMesh("Cave grid", _caveGridRoot, true);

            RenderGrid = LayoutManager.Instance.CurrentTab != Tab.Menu;
            CommandManager.ForgetAction();
        }

        private void LateUpdate()
        {
            if (_needsRoofUpdate)
            {
                _needsRoofUpdate = false;
                RecalculateRoofsInternal();
            }
        }

        private void RefreshAllTiles()
        {
            foreach (Tile tile in _tiles)
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
                    for (int i3 = 0; i3 < Constants.LevelLimit; i3++)
                    {
                        LevelEntity entity = this[i, i2].GetTileContent(i3);
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
                    for (int i3 = 0; i3 < Constants.LevelLimit; i3++)
                    {
                        LevelEntity entity = this[i, i2].GetTileContent(i3);
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

            foreach (Tile tile in _tiles)
            {
                mapMaterials.Add(tile.CalculateTileMaterials(TilePart.Everything));
            }

            return mapMaterials;
        }

        public int CoordinateToIndex(int x, int y)
        {
            return x * (Height + 1) + y;
        }

        public void AddEntityToMap(GameObject entity, int level)
        {
            bool cave = level < 0;
            int absoluteLevel = cave ? -level - 1 : level;
            if (cave)
            {
                entity.transform.SetParent(_caveLevelRoots[absoluteLevel]);
            }
            else
            {
                entity.transform.SetParent(_surfaceLevelRoots[absoluteLevel]);
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
            _needsRoofUpdate = true;
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
            return _tiles.Cast<Tile>().GetEnumerator();
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
                    _tiles[i, i2].Serialize(document, tile);
                    localRoot.AppendChild(tile);
                }
            }
        }

        public float GetRelativeLevelOpacity(int relativeLevel)
        {
            if (relativeLevel == 0)
            {
                return 1;
            }
            else if (relativeLevel == -1)
            {
                return 0.6f;
            }
            else if (relativeLevel == -2)
            {
                return 0.25f;
            }

            return 0;
        }

        private void UpdateLevelsRendering()
        {
            bool underground = _renderedLevel < 0;
            int absoluteLevel = underground ? -_renderedLevel + 1 : _renderedLevel;

            if (underground)
            {
                foreach (Transform root in _surfaceLevelRoots)
                {
                    root.gameObject.SetActive(false);
                }

                for (int i = 0; i < _caveLevelRoots.Length; i++)
                {
                    Transform root = _caveLevelRoots[i];
                    RefreshLevelRendering(root, i - absoluteLevel);
                }

                _surfaceGridRoot.gameObject.SetActive(false);
                _caveGridRoot.gameObject.SetActive(_renderGrid);

                _caveGridRoot.localPosition = new Vector3(0, absoluteLevel * 3, 0);
            }
            else
            {
                foreach (Transform root in _caveLevelRoots)
                {
                    root.gameObject.SetActive(false);
                }

                for (int i = 0; i < _surfaceLevelRoots.Length; i++)
                {
                    Transform root = _surfaceLevelRoots[i];
                    RefreshLevelRendering(root, i - absoluteLevel);
                }

                _surfaceGridRoot.gameObject.SetActive(_renderGrid);
                _caveGridRoot.gameObject.SetActive(false);

                _surfaceGridRoot.localPosition = new Vector3(0, absoluteLevel * 3 + 0.01f, 0);
            }

            RefreshBridgesRendering(absoluteLevel);
        }

        private void RefreshLevelRendering(Transform root, int relativeLevel)
        {
            float opacity = RenderEntireMap ? 1 : GetRelativeLevelOpacity(relativeLevel);
            bool renderLevel = opacity > 0;
            root.gameObject.SetActive(renderLevel);
            if (renderLevel)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor(ShaderPropertyIds.Color, new Color(opacity, opacity, opacity));
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }

        private void RefreshBridgesRendering(int absoluteLevel)
        {
            if (RenderEntireMap)
            {
                foreach (Bridge bridge in _bridges)
                {
                    bridge.SetVisible(true);
                }
            }
            else
            {
                foreach (Bridge bridge in _bridges)
                {
                    int lowerLevel = bridge.LowerLevel;
                    int higherLevel = bridge.HigherLevel;

                    float opacity;
                    if (higherLevel > absoluteLevel)
                    {
                        opacity = 0;
                    }
                    else if (higherLevel < absoluteLevel && lowerLevel > absoluteLevel)
                    {
                        opacity = 1;
                    }
                    else
                    {
                        opacity = GetRelativeLevelOpacity(higherLevel - absoluteLevel);
                    }

                    bool renderBridge = opacity > 0;
                    bridge.SetVisible(renderBridge);
                    if (renderBridge)
                    {
                        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                        propertyBlock.SetColor(ShaderPropertyIds.Color, new Color(opacity, opacity, opacity));
                        bridge.SetPropertyBlock(propertyBlock);
                    }
                }
            }
        }

        private bool IsWithinBounds(Vector2Int tile)
        {
            return tile.x >= 0 && tile.x < Width
                && tile.y >= 0 && tile.y < Height;
        }
        
        private void GameManagerOnRenderSettingsChanged()
        {
            RefreshAllTiles();
        }

        private void OnDestroy()
        {
            _gameManager.RenderSettingsChanged -= GameManagerOnRenderSettingsChanged;
        }
    }
}
