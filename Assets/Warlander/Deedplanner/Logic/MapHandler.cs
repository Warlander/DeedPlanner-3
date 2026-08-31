using System;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Logic.Compression;
using VContainer;
using VContainer.Unity;

namespace Warlander.Deedplanner.Logic
{
    public class MapHandler
    {
        private readonly MapRegistry _registry;
        private readonly MapFactory _factory;
        private readonly MapLoader _loader;

        public Map Map
        {
            get { return _registry.CurrentMap; }
        }

        public event Action MapInitialized
        {
            add => _registry.MapInitialized += value;
            remove => _registry.MapInitialized -= value;
        }

        public ICategoryLogger Logger => _loader.Logger;

        public MapHandler(IObjectResolver resolver, IByteCompressor compressor, ILoggerSource loggerSource)
        {
            _registry = new MapRegistry();
            _factory = new MapFactory(resolver);
            _loader = new MapLoader(_factory, compressor, loggerSource);
        }

        public void CreateNewMap(int width, int height)
        {
            if (_registry.CurrentMap)
            {
                UnityEngine.Object.Destroy(_registry.CurrentMap.gameObject);
            }

            Map newMap = _factory.CreateNewMap(width, height);
            _registry.SetMap(newMap);
        }

        public void ResizeMap(int left, int right, int bottom, int top)
        {
            Map oldMap = _registry.CurrentMap;
            oldMap.gameObject.SetActive(false);
            Map newMap = _factory.ResizeMap(oldMap, left, right, bottom, top);
            UnityEngine.Object.Destroy(oldMap.gameObject);
            _registry.SetMap(newMap);
            newMap.MarkDirty();
        }

        public void ClearMap()
        {
            Map oldMap = _registry.CurrentMap;
            oldMap.gameObject.SetActive(false);
            Map newMap = _factory.ClearMap(oldMap);
            UnityEngine.Object.Destroy(oldMap.gameObject);
            _registry.SetMap(newMap);
            newMap.MarkDirty();
        }

        public void LoadMap(string mapString)
        {
            Map oldMap = _registry.CurrentMap;
            Map newMap;
            try
            {
                newMap = _loader.LoadMap(mapString);
            }
            catch
            {
                Map currentMap = _registry.CurrentMap;
                if (currentMap)
                {
                    currentMap.RestoreAsCurrentMap();
                }

                throw;
            }

            if (oldMap)
            {
                GameObject oldMapObject = oldMap.gameObject;
                oldMapObject.SetActive(false);
                UnityEngine.Object.Destroy(oldMapObject);
            }

            newMap.gameObject.SetActive(true);
            _registry.SetMap(newMap);
        }

        public async Task LoadMapAsync(Uri mapUri)
        {
            Map oldMap = _registry.CurrentMap;
            Map newMap;
            try
            {
                newMap = await _loader.LoadMapAsync(mapUri);
            }
            catch
            {
                Map currentMap = _registry.CurrentMap;
                if (currentMap)
                {
                    currentMap.RestoreAsCurrentMap();
                }

                throw;
            }

            if (newMap == null)
            {
                return;
            }

            if (_registry.CurrentMap != oldMap)
            {
                UnityEngine.Object.Destroy(newMap.gameObject);
                if (_registry.CurrentMap)
                {
                    _registry.CurrentMap.RestoreAsCurrentMap();
                }

                return;
            }

            if (oldMap)
            {
                GameObject oldMapObject = oldMap.gameObject;
                oldMapObject.SetActive(false);
                UnityEngine.Object.Destroy(oldMapObject);
            }

            newMap.gameObject.SetActive(true);
            _registry.SetMap(newMap);
        }
    }
}
