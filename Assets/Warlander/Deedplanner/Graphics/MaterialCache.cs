using System.Collections.Generic;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class MaterialCache : IMaterialCache
    {
        private readonly Dictionary<MaterialKey, Material> _cachedMaterials = new Dictionary<MaterialKey, Material>();
        private readonly IMaterialLoader _materialLoader;

        public MaterialCache(IMaterialLoader materialLoader)
        {
            _materialLoader = materialLoader;
        }

        public Material GetOrCreateMaterial(MaterialMetadata materialMetadata)
        {
            MaterialKey materialKey = new MaterialKey(materialMetadata.MaterialName, materialMetadata.TextureLocation);

            if (_cachedMaterials.TryGetValue(materialKey, out Material cachedMaterial))
            {
                return cachedMaterial;
            }

            Material material = _materialLoader.CreateMaterial(materialMetadata);
            _cachedMaterials[materialKey] = material;
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
