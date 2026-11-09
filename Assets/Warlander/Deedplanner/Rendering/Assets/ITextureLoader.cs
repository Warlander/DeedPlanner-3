using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public interface ITextureLoader
    {
        // DDS CPU data is released only when readable is false and this predicate permits it.
        Task<Texture2D> LoadTextureAsync(string location, bool readable, bool normalMap = false, bool linearData = false,
            Func<bool> canDiscardReadableData = null);
    }
}
