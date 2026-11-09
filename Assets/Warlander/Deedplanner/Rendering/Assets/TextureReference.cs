using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public class TextureReference
    {
        private readonly ITextureLoader _textureLoader;
        private readonly bool _normalMap;
        private readonly bool _linearData;
        private readonly TextureReference _occlusionReference;
        private readonly TextureReference _normalReference;

        private Texture2D texture;
        private Sprite sprite;
        private Task<Texture2D> textureLoadTask;
        private bool _renderUsage;
        private bool _spriteUsage;

        public string Location { get; }
        public Vector2 SpecularRange { get; }
        public float HeightScale { get; }

        public TextureReference(ITextureLoader textureLoader, string location, bool normalMap = false, TextureReference normalReference = null, Vector2? specularRange = null, TextureReference occlusionReference = null, bool linearData = false, float heightScale = 0)
        {
            _textureLoader = textureLoader;
            Location = location;
            _normalMap = normalMap;
            _linearData = linearData;
            _occlusionReference = occlusionReference;
            _normalReference = normalReference;
            SpecularRange = specularRange ?? new Vector2(0, 1);
            HeightScale = heightScale;
        }

        public Task<Texture2D> LoadOrGetTextureAsync()
        {
            if (texture)
            {
                return Task.FromResult(texture);
            }

            // Share pending requests to avoid exhausting WebGL’s browser connection pool.
            if (textureLoadTask == null || textureLoadTask.IsCompleted)
            {
                textureLoadTask = LoadTextureInternalAsync();
            }
            return textureLoadTask;
        }

        private async Task<Texture2D> LoadTextureInternalAsync()
        {
            string location = Path.IsPathRooted(Location)
                ? Location
                : Application.streamingAssetsPath + "/" + Location;

            texture = await _textureLoader.LoadTextureAsync(location, false, _normalMap, _linearData,
                () => _renderUsage && !_spriteUsage);
            if (texture)
            {
                texture.name = Location;
            }
            return texture;
        }

        public Task<Texture2D> LoadForRenderingAsync()
        {
            _renderUsage = true;
            return LoadOrGetTextureAsync();
        }

        public void PreserveSpriteData()
        {
            _spriteUsage = true;
        }
        
        public Task<Texture2D> LoadOrGetNormalAsync()
        {
            return _normalReference != null ? _normalReference.LoadForRenderingAsync() : Task.FromResult<Texture2D>(null);
        }

        public Task<Texture2D> LoadOrGetOcclusionAsync()
        {
            return _occlusionReference != null ? _occlusionReference.LoadForRenderingAsync() : Task.FromResult<Texture2D>(null);
        }

        public async Task<Sprite> LoadOrGetSpriteAsync()
        {
            PreserveSpriteData();
            if (sprite)
            {
                return sprite;
            }

            if (!texture)
            {
                await LoadOrGetTextureAsync();
            }

            if (!sprite && texture)
            {
                sprite = CreateSprite(texture);
            }

            return sprite;
        }

        private static Sprite CreateSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
