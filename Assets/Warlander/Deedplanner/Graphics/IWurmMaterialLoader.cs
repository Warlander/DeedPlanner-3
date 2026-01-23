using System;
using System.IO;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IWurmMaterialLoader
    {
        void LoadMaterial(BinaryReader source, string modelFolder, Action<Material> onLoaded);
        MaterialMetadata LoadMaterialMetadata(BinaryReader source, string modelFolder);
    }
}
