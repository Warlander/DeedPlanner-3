using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Graphics
{
    public class GenericTextureLoader : ITextureLoader
    {
        public async Task<Texture2D> LoadTexture(string location, bool readable)
        {
            var data = await WebUtils.ReadUrlToByteArrayAsync(location);
            Texture2D texture = new Texture2D(4, 4, TextureFormat.DXT1, true);
            texture.LoadImage(data, !readable);
            return texture;
        }
    }
}
