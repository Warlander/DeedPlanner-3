using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class MaterialLoader : IMaterialLoader
    {
        private readonly ITextureReferenceFactory _textureReferenceFactory;
        private readonly Material _womDefaultMaterial;

        public MaterialLoader(ITextureReferenceFactory textureReferenceFactory)
        {
            _textureReferenceFactory = textureReferenceFactory;
            _womDefaultMaterial = CreateWomDefaultMaterial();
        }

        public async Task<Material> CreateMaterialAsync(MaterialMetadata materialMetadata)
        {
            Material material = new Material(_womDefaultMaterial);
            material.name = materialMetadata.MaterialName;

            TextureReference textureReference = _textureReferenceFactory.GetTextureReference(materialMetadata.TextureLocation);

            if (textureReference != null)
            {
                var texture = await textureReference.LoadOrGetTextureAsync();
                if (texture)
                {
                    material.SetTexture("_BaseMap", texture);
                }
                else
                {
                    material.color = new Color(1, 1, 1, 0);
                }
            }

            material.SetFloat(ShaderPropertyIds.Glossiness, materialMetadata.Glossiness);

            return material;
        }

        private static Material CreateWomDefaultMaterial()
        {
            Shader shader = Shader.Find("Warlander/ModelShader");
            Material mat = new Material(shader);
            mat.renderQueue = 2450;
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.enableInstancing = true;
            return mat;
        }
    }
}
