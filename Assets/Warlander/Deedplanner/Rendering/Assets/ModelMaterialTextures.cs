using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public static class ModelMaterialTextures
    {
        public static async Task ApplyAsync(Material material, TextureReference reference)
        {
            material.SetFloat(ShaderPropertyIds.NormalStrength, 0);
            material.SetTexture(ShaderPropertyIds.NormalMap, null);
            Texture2D texture = await reference.LoadOrGetTextureAsync();
            Texture2D normal = await reference.LoadOrGetNormalAsync();
            if (!material) return;
            material.SetTexture(ShaderPropertyIds.BaseMap, texture);
            material.SetTexture(ShaderPropertyIds.NormalMap, normal);
            material.SetFloat(ShaderPropertyIds.NormalStrength, normal ? 1 : 0);
        }
    }
}
