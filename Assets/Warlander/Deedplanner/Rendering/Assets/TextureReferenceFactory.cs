using System;
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
        private readonly Dictionary<string, string> _normalLocations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TextureReference> _normalReferences = new Dictionary<string, TextureReference>();
        private readonly Dictionary<string, TextureReference> _references = new Dictionary<string, TextureReference>();
        private readonly ICategoryLogger _logger;

        public TextureReferenceFactory(ITextureLoader textureLoader, ICategoryLogger logger)
        {
            _textureLoader = textureLoader;
            _logger = logger;
        }

        public void LoadTextureDefinitions(XmlDocument document)
        {
            foreach (XmlElement element in document.SelectNodes("//tex[@normal] | //override[@normal] | //rock[@normal] | //roof[@normal]"))
            {
                string location = element.Name == "tex" ? element.GetAttribute("location")
                    : element.Name == "override" ? element.GetAttribute("texture") : element.GetAttribute("tex");
                _normalLocations[location] = element.GetAttribute("normal");
            }
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

            TextureReference normalReference = null;
            _normalLocations.TryGetValue(location, out string normalLocation);
            if (normalLocation != null && !_normalReferences.TryGetValue(normalLocation, out normalReference))
            {
                normalReference = new TextureReference(_textureLoader, normalLocation, normalMap: true);
                _normalReferences.Add(normalLocation, normalReference);
            }
            TextureReference reference = new TextureReference(_textureLoader, location, normalReference: normalReference);
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
