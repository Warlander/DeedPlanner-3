using System.IO;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IWurmMeshLoader
    {
        Mesh LoadMesh(BinaryReader source, Vector3 scale);
    }
}
