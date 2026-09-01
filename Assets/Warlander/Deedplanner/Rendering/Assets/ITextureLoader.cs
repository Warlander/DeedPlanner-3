using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public interface ITextureLoader
    {
        Task<Texture2D> LoadTextureAsync(string location, bool readable);
    }
}
