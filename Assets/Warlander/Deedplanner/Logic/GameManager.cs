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

        [SerializeField]
        private HeightmapHandle heightmapHandlePrefab = null;

        [SerializeField]
        private GroundUpdater groundUpdater = null;
        [SerializeField]
        private CaveUpdater caveUpdater = null;
        [SerializeField]
        private HeightUpdater heightUpdater = null;
        [SerializeField]
        private FloorUpdater floorUpdater = null;
        [SerializeField]
        private WallUpdater wallUpdater = null;
        [SerializeField]
        private RoofUpdater roofUpdater = null;
        [SerializeField]
        private ObjectUpdater objectUpdater = null;
        [SerializeField]
        private LabelUpdater labelUpdater = null;
        [SerializeField]
        private BorderUpdater borderUpdater = null;
        [SerializeField]
        private BridgesUpdater bridgeUpdater = null;
        [SerializeField]
        private MirrorUpdater mirrorUpdater = null;

        public HeightmapHandle HeightmapHandlePrefab => heightmapHandlePrefab;

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
            groundUpdater.gameObject.SetActive(true);
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
            GameObject mapObject = new GameObject("Map", typeof(Map));
            Map = mapObject.GetComponent<Map>();
            Map.Initialize(width, height);
        }
        
        public IEnumerator LoadMap(Uri mapUri)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(mapUri);
            yield return webRequest.SendWebRequest();
            if (webRequest.isHttpError || webRequest.isNetworkError)
            {
                Debug.LogError(webRequest.error);
            }
            
            string requestText = webRequest.downloadHandler.text;
            if (!mapUri.IsFile)
            {
                byte[] requestBytes = Convert.FromBase64String(requestText);
                byte[] decompressedBytes = DecompressGzip(requestBytes);
                requestText = Encoding.UTF8.GetString(decompressedBytes, 0, decompressedBytes.Length);
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(requestText);
            if (Map)
            {
                Destroy(Map.gameObject);
            }
            GameObject mapObject = new GameObject("Map", typeof(Map));
            Map = mapObject.GetComponent<Map>();
            Map.Initialize(doc);
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

        private void OnTabChange(Tab tab)
        {
            MonoBehaviour newUpdater = GetUpdaterForTab(tab);

            CheckUpdater(groundUpdater, newUpdater);
            CheckUpdater(caveUpdater, newUpdater);
            CheckUpdater(heightUpdater, newUpdater);
            CheckUpdater(floorUpdater, newUpdater);
            CheckUpdater(roofUpdater, newUpdater);
            CheckUpdater(wallUpdater, newUpdater);
            CheckUpdater(objectUpdater, newUpdater);
            CheckUpdater(labelUpdater, newUpdater);
            CheckUpdater(borderUpdater, newUpdater);
            CheckUpdater(bridgeUpdater, newUpdater);
            CheckUpdater(mirrorUpdater, newUpdater);
        }

        private void CheckUpdater(MonoBehaviour updater, MonoBehaviour check)
        {
            updater.gameObject.SetActive(updater == check);
        }

        private MonoBehaviour GetUpdaterForTab(Tab tab)
        {
            switch (tab)
            {
                case Tab.Ground:
                    return groundUpdater;
                case Tab.Caves:
                    return caveUpdater;
                case Tab.Height:
                    return heightUpdater;
                case Tab.Floors:
                    return floorUpdater;
                case Tab.Roofs:
                    return roofUpdater;
                case Tab.Walls:
                    return wallUpdater;
                case Tab.Objects:
                    return objectUpdater;
                case Tab.Labels:
                    return labelUpdater;
                case Tab.Borders:
                    return borderUpdater;
                case Tab.Bridges:
                    return bridgeUpdater;
                case Tab.Mirror:
                    return mirrorUpdater;
                default:
                    return null;
            }
        }

    }

}