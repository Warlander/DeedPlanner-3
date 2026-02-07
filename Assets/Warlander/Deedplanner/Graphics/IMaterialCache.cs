using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IMaterialCache
    {
        Task<Material> GetOrCreateMaterial(MaterialMetadata materialMetadata);
    }
}