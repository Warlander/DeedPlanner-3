using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public class TextureReferenceFactory : ITextureReferenceFactory
    {
        private readonly ITextureLoader _textureLoader;
        private readonly Dictionary<string, TextureReference> _references = new Dictionary<string, TextureReference>();
        private readonly ICategoryLogger _logger;

        public TextureReferenceFactory(ITextureLoader textureLoader, ICategoryLogger logger)
        {
            _textureLoader = textureLoader;
            _logger = logger;
        }

        public TextureReference GetTextureReference(string location)
        {
            location = location.Replace(Application.streamingAssetsPath + "/", "");

            if (string.IsNullOrEmpty(Path.GetExtension(location)))
            {
                _logger.Message("Attempting to load invalid texture from " + location);
                return null;
            }
            
            if (_references.ContainsKey(location))
            {
                return _references[location];
            }

            TextureReference reference = new TextureReference(_textureLoader, location);
            _references[location] = reference;
            return reference;
        }

        public TextureReference GetTextureReference(XmlElement element)
        {
            string location = element.GetAttribute("location");
            return GetTextureReference(location);
        }
    }
}
