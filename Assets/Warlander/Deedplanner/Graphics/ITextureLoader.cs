using System;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface ITextureLoader
    {
        void LoadTexture(string location, bool readable, Action<Texture2D> onLoaded);
    }
}
