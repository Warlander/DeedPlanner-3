using System;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Graphics
{
    public class GenericTextureLoader : ITextureLoader
    {
        public void LoadTexture(string location, bool readable, Action<Texture2D> onLoaded)
        {
            WebUtils.ReadUrlToByteArray(location, data =>
            {
                Texture2D texture = new Texture2D(4, 4, TextureFormat.DXT1, true);
                texture.LoadImage(data, !readable);
                onLoaded.Invoke(texture);
            });
        }
    }
}
