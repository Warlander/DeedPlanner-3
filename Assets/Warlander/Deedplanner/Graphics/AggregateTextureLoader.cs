using System;
using System.IO;
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

        public void LoadTexture(string location, bool readable, Action<Texture2D> onLoaded)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(location)))
            {
                Debug.LogWarning("Attempting to load texture from empty location: " + location);
                onLoaded.Invoke(null);
                return;
            }

            Debug.Log("Loading texture at " + location);

            if (location.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            {
                _ddsTextureLoader.LoadTexture(location, readable, onLoaded);
            }
            else
            {
                _genericTextureLoader.LoadTexture(location, readable, onLoaded);
            }
        }
    }
}
