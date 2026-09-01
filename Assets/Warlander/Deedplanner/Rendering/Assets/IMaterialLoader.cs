using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public interface IMaterialLoader
    {
        Task<Material> CreateMaterialAsync(MaterialMetadata materialMetadata);
    }
}
