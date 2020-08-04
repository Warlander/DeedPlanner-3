using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Updaters;

namespace Warlander.Deedplanner.Logic
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public Map Map { get; private set; }

        [SerializeField] private OverlayMesh overlayMeshPrefab = null;
        [SerializeField] private Mesh heightmapHandleMesh = null;
        [SerializeField] private PlaneLine planeLinePrefab = null;

        [SerializeField] private AbstractUpdater[] updaters = null;

        public OverlayMesh OverlayMeshPrefab => overlayMeshPrefab;
        public Mesh HeightmapHandleMesh => heightmapHandleMesh;
        public PlaneLine PlaneLinePrefab => planeLinePrefab;

        private bool renderDecorations = true;
        private bool renderTrees = true;
        private bool renderBushes = true;
        private bool renderShips = true;

        public GameManager()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            updaters[0].gameObject.SetActive(true);
            LayoutManager.Instance.TabChanged += OnTabChange;
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
            {
                Map.CommandManager.Undo();
            }
            else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y))
            {
                Map.CommandManager.Redo();
            }
        }

        public void CreateNewMap(int width, int height)
        {
            if (Map)
            {
                Destroy(Map.gameObject);
            }
            
            GameObject mapObject = new GameObject("Map", typeof(Map));
            Map = mapObject.GetComponent<Map>();
            Map.Initialize(width, height);
            ApplyPropertiesToMap(Map);
        }

        public void ResizeMap(int left, int right, int bottom, int top)
        {
            Map.gameObject.SetActive(false);
            GameObject mapObject = new GameObject("Map", typeof(Map));
            Map newMap = mapObject.GetComponent<Map>();
            newMap.Initialize(Map, left, right, bottom, top);
            Destroy(Map.gameObject);
            Map = newMap;
            ApplyPropertiesToMap(Map);
        }

        public void ClearMap()
        {
            int width = Map.Width;
            int height = Map.Height;
            
            if (Map)
            {
                Destroy(Map.gameObject);
            }
            
            GameObject mapObject = new GameObject("Map", typeof(Map));
            Map = mapObject.GetComponent<Map>();
            Map.Initialize(width, height);
            ApplyPropertiesToMap(Map);
        }

        public IEnumerator LoadMap(Uri mapUri)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(mapUri);
            yield return webRequest.SendWebRequest();
            if (webRequest.isHttpError || webRequest.isNetworkError)
            {
                Debug.LogError(webRequest.error);
                webRequest.Dispose();
                yield break;
            }
            
            Debug.Log("Map downloaded, checking if compressed");
            string requestText = webRequest.downloadHandler.text;
            webRequest.Dispose();
            try
            {
                byte[] requestBytes = Convert.FromBase64String(requestText);
                byte[] decompressedBytes = DecompressGzip(requestBytes);
                requestText = Encoding.UTF8.GetString(decompressedBytes, 0, decompressedBytes.Length);
                Debug.Log("Compressed map, decompressed");
            }
            catch
            {
                Debug.Log("Not compressed map");
            }

            LoadMap(requestText);
        }
        
        public void LoadMap(string mapString)
        {
            CoroutineManager.Instance.BlockInteractionUntilFinished();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(mapString);
            if (Map)
            {
                GameObject oldMapObject = Map.gameObject;
                oldMapObject.SetActive(false);
                Destroy(oldMapObject);
            }
            
            GameObject mapObject = new GameObject("Map", typeof(Map));
            Map = mapObject.GetComponent<Map>();
            Map.Initialize(doc);
            ApplyPropertiesToMap(Map);
        }

        private void ApplyPropertiesToMap(Map map)
        {
            map.RenderDecorations = renderDecorations;
            map.RenderTrees = renderTrees;
            map.RenderBushes = renderBushes;
            map.RenderShips = renderShips;
        }

        private byte[] DecompressGzip(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        private void CheckUpdater(AbstractUpdater updater, Tab tab)
        {
            updater.gameObject.SetActive(updater.TargetTab == tab);
        }
        
        private void OnTabChange(Tab tab)
        { 
            foreach (AbstractUpdater updater in updaters)
            {
                CheckUpdater(updater, tab);
            }

            Map.RenderGrid = LayoutManager.Instance.CurrentTab != Tab.Menu;
        }

        public void OnDecorationsVisibilityChange(bool enable)
        {
            renderDecorations = enable;
            Map.RenderDecorations = renderDecorations;
        }

        public void OnTreeVisibilityChange(bool enable)
        {
            renderTrees = enable;
            Map.RenderTrees = renderTrees;
        }

        public void OnBushVisibilityChange(bool enable)
        {
            renderBushes = enable;
            Map.RenderBushes = renderBushes;
        }

        public void OnShipsVisibilityChange(bool enable)
        {
            renderShips = enable;
            Map.RenderShips = renderShips;
        }
    }
}