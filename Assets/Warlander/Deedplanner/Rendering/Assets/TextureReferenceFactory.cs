using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public class TextureReferenceFactory : ITextureReferenceFactory
    {
        private readonly ITextureLoader _textureLoader;
        private readonly Dictionary<string, float> _heightScales = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _occlusionLocations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TextureReference> _occlusionReferences = new Dictionary<string, TextureReference>();
        private readonly Dictionary<string, Vector2> _specularRanges = new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase);
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
            foreach (XmlElement element in document.SelectNodes("//tex[@normal or @occlusion or @heightScale] | //override[@normal or @occlusion or @heightScale] | //rock[@normal or @occlusion or @heightScale] | //roof[@normal or @occlusion or @heightScale]"))
            {
                string location = element.Name == "tex" ? element.GetAttribute("location")
                    : element.Name == "override" ? element.GetAttribute("texture") : element.GetAttribute("tex");
                if (element.HasAttribute("normal")) _normalLocations[location] = element.GetAttribute("normal");
                if (element.HasAttribute("occlusion")) _occlusionLocations[location] = element.GetAttribute("occlusion");
                if (element.HasAttribute("heightScale"))
                {
                    float heightScale = float.Parse(element.GetAttribute("heightScale"), CultureInfo.InvariantCulture);
                    if (float.IsNaN(heightScale) || float.IsInfinity(heightScale) || heightScale < 0
                        || (heightScale > 0 && (!_occlusionLocations.TryGetValue(location, out string heightLocation) || string.IsNullOrEmpty(heightLocation))))
                        throw new FormatException("heightScale requires a non-negative finite value and an occlusion texture containing red-channel height.");
                    _heightScales[location] = heightScale;
                }
                Vector2 previousRange = _specularRanges.TryGetValue(location, out Vector2 existingRange) ? existingRange : new Vector2(0, 1);
                float min = element.HasAttribute("specularMin") ? float.Parse(element.GetAttribute("specularMin"), CultureInfo.InvariantCulture) : previousRange.x;
                float max = element.HasAttribute("specularMax") ? float.Parse(element.GetAttribute("specularMax"), CultureInfo.InvariantCulture) : previousRange.y;
                _specularRanges[location] = new Vector2(min, max);
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
            TextureReference occlusionReference = null;
            if (_occlusionLocations.TryGetValue(location, out string occlusionLocation)
                && !_occlusionReferences.TryGetValue(occlusionLocation, out occlusionReference))
            {
                occlusionReference = new TextureReference(_textureLoader, occlusionLocation, linearData: true);
                _occlusionReferences.Add(occlusionLocation, occlusionReference);
            }
            Vector2? specularRange = _specularRanges.TryGetValue(location, out Vector2 range) ? range : (Vector2?)null;
            float heightScale = _heightScales.TryGetValue(location, out float scale) ? scale : 0;
            TextureReference reference = new TextureReference(_textureLoader, location, normalReference: normalReference, specularRange: specularRange, occlusionReference: occlusionReference, heightScale: heightScale);
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
