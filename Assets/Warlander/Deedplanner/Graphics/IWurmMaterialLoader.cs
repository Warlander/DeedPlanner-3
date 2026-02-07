using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IWurmMaterialLoader
    {
        Task<Material> LoadMaterial(BinaryReader source, string modelFolder);
        MaterialMetadata LoadMaterialMetadata(BinaryReader source, string modelFolder);
    }
}
