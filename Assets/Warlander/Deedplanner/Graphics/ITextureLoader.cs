using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface ITextureLoader
    {
        Task<Texture2D> LoadTexture(string location, bool readable);
    }
}
