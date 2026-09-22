using Warlander.Deedplanner.Platform.Web;
using System;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public class GenericTextureLoader : ITextureLoader
    {
        private readonly ICategoryLogger _logger;

        public GenericTextureLoader(ICategoryLogger logger)
        {
            _logger = logger;
        }

        public async Task<Texture2D> LoadTextureAsync(string location, bool readable, bool normalMap = false, bool linearData = false)
        {
            var data = await WebUtils.ReadUrlToByteArrayAsync(location);
            if (data == null)
            {
                _logger.Warning("Unable to load texture: " + location);
                return null;
            }

            string name = location.Substring(location.LastIndexOf("/", StringComparison.Ordinal) + 1);
            
            Texture2D texture = new Texture2D(4, 4, normalMap || linearData ? TextureFormat.RGBA32 : TextureFormat.DXT1, true, normalMap || linearData);
            texture.LoadImage(data, !readable && !normalMap);
            if (normalMap)
            {
                Color32[] pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i].g = (byte)(255 - pixels[i].g);
                }
                texture.SetPixels32(pixels);
                texture.Apply(true, !readable);
            }
            texture.name = name;
            
            return texture;
        }
    }
}
