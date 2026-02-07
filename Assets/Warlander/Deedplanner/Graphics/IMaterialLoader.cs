using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface IMaterialLoader
    {
        Task<Material> CreateMaterialAsync(MaterialMetadata materialMetadata);
    }
}