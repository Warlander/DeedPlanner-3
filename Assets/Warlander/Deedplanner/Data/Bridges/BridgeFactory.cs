using System.Xml;
using Warlander.Deedplanner.Logic;
using Zenject;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgeFactory
    {
        [Inject] private OutlineCoordinator _outlineCoordinator;

        public Bridge CreateBridge(Map map, XmlElement element)
        {
            return new Bridge(map, element, _outlineCoordinator);
        }
    }
}