using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public static class ModelMaterialTextures
    {
        public static async Task ApplyAsync(Material material, TextureReference reference)
        {
            material.EnableKeyword("_SPECULAR_SETUP");
            material.SetFloat(ShaderPropertyIds.NormalStrength, 0);
            material.SetVector(ShaderPropertyIds.SpecularRange, new Vector4(0, 1, 0, 0));
            material.SetTexture(ShaderPropertyIds.NormalMap, null);
            material.SetTexture(ShaderPropertyIds.OcclusionMap, null);
            material.SetFloat(ShaderPropertyIds.OcclusionStrength, 0);
            Texture2D texture = await reference.LoadOrGetTextureAsync();
            Texture2D normal = await reference.LoadOrGetNormalAsync();
            Texture2D occlusion = await reference.LoadOrGetOcclusionAsync();
            if (!material) return;
            material.SetTexture(ShaderPropertyIds.BaseMap, texture);
            material.SetTexture(ShaderPropertyIds.OcclusionMap, occlusion);
            material.SetFloat(ShaderPropertyIds.OcclusionStrength, occlusion ? 0.35f : 0);
            material.SetTexture(ShaderPropertyIds.NormalMap, normal);
            material.SetFloat(ShaderPropertyIds.NormalStrength, normal ? 1 : 0);
            material.SetVector(ShaderPropertyIds.SpecularRange, new Vector4(reference.SpecularRange.x, reference.SpecularRange.y, 0, 0));
        }
    }
}
