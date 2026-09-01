using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public interface IWurmModelLoader
    {
        Task<GameObject> LoadModelAsync(string path);
        Task<GameObject> LoadModelAsync(string path, Vector3 scale);
    }
}
