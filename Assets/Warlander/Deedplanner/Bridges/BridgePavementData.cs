using Warlander.Deedplanner.Data;
using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Bridges
{
    public class BridgePavementData
    {
        public string Name { get; }
        public string ShortName { get; }
        public string Token => ShortName;
        public TextureReference Tex { get; }
        public Materials Materials { get; }

        private Material _overlayMaterial;

        public BridgePavementData(string name, string shortName, TextureReference tex, Materials materials)
        {
            Name = name;
            ShortName = shortName;
            Tex = tex;
            Materials = materials ?? new Materials();
        }

        // Cutout variant of the model shader, same as loaded models, so level-opacity fades
        // (driven through _BaseColor alpha) affect the overlay exactly like the deck under it.
        public Material GetOrCreateOverlayMaterial()
        {
            if (_overlayMaterial)
            {
                return _overlayMaterial;
            }

            _overlayMaterial = new Material(Shader.Find("Warlander/ModelShader"));
            _overlayMaterial.name = "Bridge Paving " + Name;
            _overlayMaterial.renderQueue = 2450;
            _overlayMaterial.SetOverrideTag("RenderType", "TransparentCutout");
            _overlayMaterial.EnableKeyword("_ALPHATEST_ON");
            _overlayMaterial.enableInstancing = true;
            LoadOverlayTextureAsync();
            return _overlayMaterial;
        }

        private async void LoadOverlayTextureAsync()
        {
            Texture2D texture = await Tex.LoadOrGetTextureAsync();
            if (_overlayMaterial && texture)
            {
                _overlayMaterial.SetTexture(ShaderPropertyIds.BaseMap, texture);
            }
        }
    }
}
