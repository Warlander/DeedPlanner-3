using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IMaterialCache
    {
        Material GetOrCreateMaterial(MaterialMetadata materialMetadata);
    }
}