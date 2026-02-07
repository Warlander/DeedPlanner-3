using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class MaterialLoader : IMaterialLoader
    {
        private readonly ITextureReferenceFactory _textureReferenceFactory;

        public MaterialLoader(ITextureReferenceFactory textureReferenceFactory)
        {
            _textureReferenceFactory = textureReferenceFactory;
        }

        public async Task<Material> CreateMaterial(MaterialMetadata materialMetadata)
        {
            Material material = new Material(GraphicsManager.Instance.WomDefaultMaterial);
            material.name = materialMetadata.MaterialName;
            
            TextureReference textureReference = _textureReferenceFactory.GetTextureReference(materialMetadata.TextureLocation);

            if (textureReference != null)
            {
                var texture = await textureReference.LoadOrGetTexture();
                if (texture)
                {
                    material.SetTexture(ShaderPropertyIds.MainTex, texture);
                }
                else
                {
                    material.SetColor(ShaderPropertyIds.Color, new Color(1, 1, 1, 0));
                }
            }

            material.SetFloat(ShaderPropertyIds.Glossiness, materialMetadata.Glossiness);
            
            return material;
        }
    }
}
