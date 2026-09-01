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

        public async Task<Texture2D> LoadTextureAsync(string location, bool readable)
        {
            var data = await WebUtils.ReadUrlToByteArrayAsync(location);
            if (data == null)
            {
                _logger.Warning("Unable to load texture: " + location);
                return null;
            }

            string name = location.Substring(location.LastIndexOf("/", StringComparison.Ordinal) + 1);
            
            Texture2D texture = new Texture2D(4, 4, TextureFormat.DXT1, true);
            texture.LoadImage(data, !readable);
            texture.name = name;
            
            return texture;
        }
    }
}
