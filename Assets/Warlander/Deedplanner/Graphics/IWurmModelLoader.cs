using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IWurmModelLoader
    {
        Task<GameObject> LoadModel(string path);
        Task<GameObject> LoadModel(string path, Vector3 scale);
    }
}
