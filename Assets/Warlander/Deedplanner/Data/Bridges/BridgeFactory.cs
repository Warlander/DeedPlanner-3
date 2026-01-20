using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;
using Zenject;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgeFactory
    {
        [Inject] private IOutlineCoordinator _outlineCoordinator;

        public Bridge CreateBridge(Map map, XmlElement element)
        {
            return new Bridge(map, element, _outlineCoordinator);
        }

        /// <summary>
        /// Used for moving (previously) existing bridges around the map.
        /// </summary>
        public Bridge CreateBridge(Map map, Bridge originalBridge, Vector2Int tileShift)
        {
            return new Bridge(map, originalBridge, tileShift, _outlineCoordinator);
        }
    }
}