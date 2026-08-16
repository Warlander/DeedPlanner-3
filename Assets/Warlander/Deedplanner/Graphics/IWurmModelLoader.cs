using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IWurmModelLoader
    {
        Task<GameObject> LoadModelAsync(string path);
        Task<GameObject> LoadModelAsync(string path, Vector3 scale);
    }
}
