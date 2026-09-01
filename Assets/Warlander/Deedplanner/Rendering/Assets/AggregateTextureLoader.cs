using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public class AggregateTextureLoader : ITextureLoader
    {
        private readonly DDSTextureLoader _ddsTextureLoader;
        private readonly GenericTextureLoader _genericTextureLoader;
        private readonly ICategoryLogger _logger;

        public AggregateTextureLoader(ICategoryLogger logger)
        {
            _ddsTextureLoader = new DDSTextureLoader(logger);
            _genericTextureLoader = new GenericTextureLoader(logger);
            _logger = logger;
        }

        public async Task<Texture2D> LoadTextureAsync(string location, bool readable)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(location)))
            {
                _logger.Warning("Attempting to load texture from empty location: " + location);
                return null;
            }

            _logger.Message("Loading texture at " + location);

            if (location.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            {
                return await _ddsTextureLoader.LoadTextureAsync(location, readable);
            }
            else
            {
                return await _genericTextureLoader.LoadTextureAsync(location, readable);
            }
        }
    }
}
