using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class MaterialCache : IMaterialCache
    {
        private readonly Dictionary<MaterialKey, Material> _cachedMaterials = new Dictionary<MaterialKey, Material>();
        private readonly Dictionary<MaterialKey, Task<Material>> _pendingMaterials = new Dictionary<MaterialKey, Task<Material>>();
        private readonly IMaterialLoader _materialLoader;

        public MaterialCache(IMaterialLoader materialLoader)
        {
            _materialLoader = materialLoader;
        }

        public async Task<Material> GetOrCreateMaterial(MaterialMetadata materialMetadata)
        {
            MaterialKey materialKey = new MaterialKey(materialMetadata.MaterialName, materialMetadata.TextureLocation);

            if (_cachedMaterials.TryGetValue(materialKey, out Material cachedMaterial))
            {
                return cachedMaterial;
            }

            if (_pendingMaterials.TryGetValue(materialKey, out Task<Material> pendingTask))
            {
                return await pendingTask;
            }

            Task<Material> materialTask = _materialLoader.CreateMaterial(materialMetadata);
            _pendingMaterials[materialKey] = materialTask;
            
            Material material = await materialTask;
            
            _cachedMaterials[materialKey] = material;
            _pendingMaterials.Remove(materialKey);
            
            return material;
        }

        private struct MaterialKey
        {
            public string Name { get; }
            public string TextureLocation { get; }

            public MaterialKey(string name, string textureLocation)
            {
                Name = name;
                TextureLocation = textureLocation;
            }
        }
    }
}
