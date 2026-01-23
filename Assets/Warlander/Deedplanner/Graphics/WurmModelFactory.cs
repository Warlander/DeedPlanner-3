using System.Xml;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class WurmModelFactory : IWurmModelFactory
    {
        private readonly IWurmModelLoader _wurmModelLoader;
        private readonly ITextureReferenceFactory _textureReferenceFactory;

        public WurmModelFactory(IWurmModelLoader wurmModelLoader, ITextureReferenceFactory textureReferenceFactory)
        {
            _wurmModelLoader = wurmModelLoader;
            _textureReferenceFactory = textureReferenceFactory;
        }

        public Model CreateModel(XmlElement element, Vector3 scale, int layer = int.MaxValue)
        {
            return new Model(_wurmModelLoader, _textureReferenceFactory, element, scale, layer);
        }

        public Model CreateModel(XmlElement element, int layer = int.MaxValue)
        {
            return new Model(_wurmModelLoader, _textureReferenceFactory, element, layer);
        }

        public Model CreateModel(string location, Vector3 scale, int layer = int.MaxValue)
        {
            return new Model(_wurmModelLoader, _textureReferenceFactory, location, scale, layer);
        }

        public Model CreateModel(string newLocation, int layer = int.MaxValue)
        {
            return new Model(_wurmModelLoader, _textureReferenceFactory, newLocation, layer);
        }
    }
}
