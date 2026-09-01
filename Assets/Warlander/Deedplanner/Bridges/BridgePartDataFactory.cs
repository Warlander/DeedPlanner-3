using Warlander.Deedplanner.Domain;
using System.Xml;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Bridges
{
    public class BridgePartDataFactory
    {
        private readonly IWurmAssetFacade _assetFacade;

        public BridgePartDataFactory(IWurmAssetFacade assetFacade)
        {
            _assetFacade = assetFacade;
        }

        public BridgePartData Create(XmlElement element)
        {
            return new BridgePartData(_assetFacade, element);
        }
    }
}
