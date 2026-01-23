using System;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IWurmModelLoader
    {
        void LoadModel(string path, Action<GameObject> onLoaded);
        void LoadModel(string path, Vector3 scale, Action<GameObject> onLoaded);
    }
}
