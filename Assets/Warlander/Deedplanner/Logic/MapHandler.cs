using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.InputSystem;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Utils;
using Zenject;

namespace Warlander.Deedplanner.Logic
{
    public class MapHandler : IInitializable
    {
        private readonly IInstantiator _instantiator;
        private readonly DPInput _input;

        public event Action MapInitialized;

        public Map Map { get; private set; }

        public MapHandler(IInstantiator instantiator, DPInput input)
        {
            _instantiator = instantiator;
            _input = input;
        }

        void IInitializable.Initialize()
        {
            _input.EditingControls.Undo.performed += UndoOnperformed;
            _input.EditingControls.Redo.performed += RedoOnperformed;
        }

        private void UndoOnperformed(InputAction.CallbackContext obj)
        {
            Map.CommandManager.Undo();
        }

        private void RedoOnperformed(InputAction.CallbackContext obj)
        {
            Map.CommandManager.Redo();
        }

        public void CreateNewMap(int width, int height)
        {
            if (Map)
            {
                UnityEngine.Object.Destroy(Map.gameObject);
            }

            Map = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            Map.Initialize(width, height);
            MapInitialized?.Invoke();
        }

        public void ResizeMap(int left, int right, int bottom, int top)
        {
            Map.gameObject.SetActive(false);
            Map newMap = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            newMap.Initialize(Map, left, right, bottom, top);
            UnityEngine.Object.Destroy(Map.gameObject);
            Map = newMap;
            MapInitialized?.Invoke();
        }

        public void ClearMap()
        {
            int width = Map.Width;
            int height = Map.Height;

            if (Map)
            {
                UnityEngine.Object.Destroy(Map.gameObject);
            }

            Map = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            Map.Initialize(width, height);
            MapInitialized?.Invoke();
        }

        public async Task LoadMapAsync(Uri mapUri)
        {
            byte[] mapData = await WebUtils.ReadUrlToByteArrayAsync(mapUri);

            if (mapData == null)
            {
                Debug.LogError("Failed to download map from: " + mapUri);
                return;
            }

            Debug.Log("Map downloaded, checking if compressed");
            string requestText = Encoding.UTF8.GetString(mapData);

            try
            {
                byte[] requestBytes = Convert.FromBase64String(requestText);
                byte[] decompressedBytes = await DecompressGzipAsync(requestBytes);
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
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(mapString);
            if (Map)
            {
                GameObject oldMapObject = Map.gameObject;
                oldMapObject.SetActive(false);
                UnityEngine.Object.Destroy(oldMapObject);
            }

            Map = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            Map.Initialize(doc);
            MapInitialized?.Invoke();
        }

        private async Task<byte[]> DecompressGzipAsync(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    await stream.CopyToAsync(memory);
                    return memory.ToArray();
                }
            }
        }
    }
}
