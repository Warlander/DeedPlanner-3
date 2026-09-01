using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public interface IWurmMaterialLoader
    {
        Task<Material> LoadMaterialAsync(BinaryReader source, string modelFolder);
        MaterialMetadata LoadMaterialMetadata(BinaryReader source, string modelFolder);
    }
}
