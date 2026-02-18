using System;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Zenject;

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

        public MapHandler(IInstantiator instantiator)
        {
            _registry = new MapRegistry();
            _factory = new MapFactory(instantiator);
            _loader = new MapLoader(_factory);
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
        }

        public void ClearMap()
        {
            Map oldMap = _registry.CurrentMap;
            oldMap.gameObject.SetActive(false);
            Map newMap = _factory.ClearMap(oldMap);
            UnityEngine.Object.Destroy(oldMap.gameObject);
            _registry.SetMap(newMap);
        }

        public void LoadMap(string mapString)
        {
            if (_registry.CurrentMap)
            {
                GameObject oldMapObject = _registry.CurrentMap.gameObject;
                oldMapObject.SetActive(false);
                UnityEngine.Object.Destroy(oldMapObject);
            }

            Map newMap = _loader.LoadMap(mapString);
            _registry.SetMap(newMap);
        }

        public async Task LoadMapAsync(Uri mapUri)
        {
            Map newMap = await _loader.LoadMapAsync(mapUri);

            if (newMap == null)
            {
                return;
            }

            if (_registry.CurrentMap)
            {
                GameObject oldMapObject = _registry.CurrentMap.gameObject;
                oldMapObject.SetActive(false);
                UnityEngine.Object.Destroy(oldMapObject);
            }

            _registry.SetMap(newMap);
        }
    }
}
