using System;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Graphics
{
    public class GenericTextureLoader : ITextureLoader
    {
        public async Task<Texture2D> LoadTextureAsync(string location, bool readable)
        {
            var data = await WebUtils.ReadUrlToByteArrayAsync(location);
            string name = location.Substring(location.LastIndexOf("/", StringComparison.Ordinal) + 1);
            
            Texture2D texture = new Texture2D(4, 4, TextureFormat.DXT1, true);
            texture.LoadImage(data, !readable);
            texture.name = name;
            
            return texture;
        }
    }
}
