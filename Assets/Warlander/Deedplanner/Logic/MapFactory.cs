using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Zenject;

namespace Warlander.Deedplanner.Logic
{
    public class MapFactory
    {
        private readonly IInstantiator _instantiator;

        public MapFactory(IInstantiator instantiator)
        {
            _instantiator = instantiator;
        }

        public Map CreateNewMap(int width, int height)
        {
            Map map = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            map.Initialize(width, height);
            return map;
        }

        public Map ResizeMap(Map currentMap, int left, int right, int bottom, int top)
        {
            Map newMap = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            newMap.Initialize(currentMap, left, right, bottom, top);
            return newMap;
        }

        public Map ClearMap(Map currentMap)
        {
            Map map = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            map.Initialize(currentMap.Width, currentMap.Height);
            return map;
        }

        public Map LoadFromXml(XmlDocument doc)
        {
            Map map = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            map.Initialize(doc);
            return map;
        }
    }
}
