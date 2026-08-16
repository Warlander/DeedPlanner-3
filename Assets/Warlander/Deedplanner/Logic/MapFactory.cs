using System.Xml;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic
{
    public class MapFactory
    {
        private readonly IObjectResolver _resolver;

        public MapFactory(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public Map CreateNewMap(int width, int height)
        {
            Map map = CreateMapGameObject();
            map.Initialize(width, height);
            return map;
        }

        public Map ResizeMap(Map currentMap, int left, int right, int bottom, int top)
        {
            Map newMap = CreateMapGameObject();
            newMap.Initialize(currentMap, left, right, bottom, top);
            return newMap;
        }

        public Map ClearMap(Map currentMap)
        {
            Map map = CreateMapGameObject();
            map.Initialize(currentMap.Width, currentMap.Height);
            return map;
        }

        public Map LoadFromXml(XmlDocument doc)
        {
            Map map = CreateMapGameObject();
            map.Initialize(doc);
            return map;
        }

        private Map CreateMapGameObject()
        {
            var go = new GameObject("Map");
            var map = go.AddComponent<Map>();
            _resolver.InjectGameObject(go);
            return map;
        }
    }
}
