using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IMaterialLoader
    {
        Material CreateMaterial(MaterialMetadata materialMetadata);
    }
}