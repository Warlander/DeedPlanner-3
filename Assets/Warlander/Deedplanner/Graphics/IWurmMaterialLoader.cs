using System;
using System.IO;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IWurmMaterialLoader
    {
        Material LoadMaterial(BinaryReader source, string modelFolder);
        MaterialMetadata LoadMaterialMetadata(BinaryReader source, string modelFolder);
    }
}
