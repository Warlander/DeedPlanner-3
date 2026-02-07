using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class AggregateTextureLoader : ITextureLoader
    {
        private readonly DDSTextureLoader _ddsTextureLoader;
        private readonly GenericTextureLoader _genericTextureLoader;

        public AggregateTextureLoader()
        {
            _ddsTextureLoader = new DDSTextureLoader();
            _genericTextureLoader = new GenericTextureLoader();
        }

        public async Task<Texture2D> LoadTextureAsync(string location, bool readable)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(location)))
            {
                Debug.LogWarning("Attempting to load texture from empty location: " + location);
                return null;
            }

            Debug.Log("Loading texture at " + location);

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
