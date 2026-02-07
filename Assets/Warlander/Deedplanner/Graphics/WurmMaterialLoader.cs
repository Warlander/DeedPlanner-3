using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class WurmMaterialLoader : IWurmMaterialLoader
    {
        private readonly IMaterialCache _materialCache;

        public WurmMaterialLoader(IMaterialCache materialCache)
        {
            _materialCache = materialCache;
        }

        public async Task<Material> LoadMaterialAsync(BinaryReader source, string modelFolder)
        {
            var materialMetadata = LoadMaterialMetadata(source, modelFolder);
            Material material = await _materialCache.GetOrCreateMaterialAsync(materialMetadata);
            return material;
        }

        public MaterialMetadata LoadMaterialMetadata(BinaryReader source, string modelFolder)
        {
            string texName = WurmFileUtility.ReadString(source);
            string texLocation = Path.Combine(modelFolder, texName).Replace("\\", "/");
            string matName = WurmFileUtility.ReadString(source);
            
            float glossiness = 0;

            bool hasMaterialProperties = source.ReadBoolean();
            if (hasMaterialProperties)
            {
                bool hasEmissive = source.ReadBoolean();
                if (hasEmissive)
                {
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                }

                bool hasShininess = source.ReadBoolean();
                if (hasShininess)
                {
                    glossiness = source.ReadSingle() / 100;
                    if (glossiness > 1)
                    {
                        glossiness = 0;
                    }
                }

                bool hasSpecular = source.ReadBoolean();
                if (hasSpecular)
                {
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                }

                bool hasTransparencyColor = source.ReadBoolean();
                if (hasTransparencyColor)
                {
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                }
            }
            
            var materialMetadata = new MaterialMetadata()
            {
                TextureName = texName,
                TextureLocation = texLocation,
                MaterialName = matName,
                Glossiness = glossiness
            };
            
            return materialMetadata;
        }

    }
}
