using System.IO;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public interface IWurmMeshLoader
    {
        Mesh LoadMesh(BinaryReader source, Vector3 scale);
    }
}
