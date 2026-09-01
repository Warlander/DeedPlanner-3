using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public interface IMaterialCache
    {
        Task<Material> GetOrCreateMaterialAsync(MaterialMetadata materialMetadata);
    }
}
