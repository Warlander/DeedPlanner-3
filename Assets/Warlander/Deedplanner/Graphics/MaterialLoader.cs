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
                    material.mainTexture = texture;
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
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material mat = new Material(shader);
            mat.renderQueue = 2450;
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_AlphaToMask", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_SpecularHighlights", 0f);
            mat.SetFloat("_EnvironmentReflections", 0f);
            mat.enableInstancing = true;
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            return mat;
        }
    }
}
