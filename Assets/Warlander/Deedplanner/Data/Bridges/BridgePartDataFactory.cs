using System.Xml;
using Warlander.Deedplanner.Graphics;
using Zenject;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgePartDataFactory
    {
        private readonly IWurmModelFactory _modelFactory;
        private readonly ITextureReferenceFactory _textureReferenceFactory;

        [Inject]
        public BridgePartDataFactory(IWurmModelFactory modelFactory, ITextureReferenceFactory textureReferenceFactory)
        {
            _modelFactory = modelFactory;
            _textureReferenceFactory = textureReferenceFactory;
        }

        public BridgePartData Create(XmlElement element)
        {
            return new BridgePartData(_modelFactory, _textureReferenceFactory, element);
        }
    }
}
