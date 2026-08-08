using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public class TextureReference
    {
        private readonly ITextureLoader _textureLoader;

        private Texture2D texture;
        private Sprite sprite;
        private Task<Texture2D> textureLoadTask;

        public string Location { get; }

        public TextureReference(ITextureLoader textureLoader, string location)
        {
            _textureLoader = textureLoader;
            Location = location;
        }

        public Task<Texture2D> LoadOrGetTextureAsync()
        {
            if (texture)
            {
                return Task.FromResult(texture);
            }

            // concurrent callers must share the in-flight load - on WebGL thousands of
            // duplicate requests exhaust the browser socket pool (ERR_INSUFFICIENT_RESOURCES)
            textureLoadTask ??= LoadTextureInternalAsync();
            return textureLoadTask;
        }

        private async Task<Texture2D> LoadTextureInternalAsync()
        {
            string location = Path.IsPathRooted(Location)
                ? Location
                : Application.streamingAssetsPath + "/" + Location;

            texture = await _textureLoader.LoadTextureAsync(location, false);

            if (texture)
            {
                texture.name = Location;
            }

            return texture;
        }
        
        public async Task<Sprite> LoadOrGetSpriteAsync()
        {
            if (sprite)
            {
                return sprite;
            }

            if (!texture)
            {
                await LoadOrGetTextureAsync();
            }

            if (texture)
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
