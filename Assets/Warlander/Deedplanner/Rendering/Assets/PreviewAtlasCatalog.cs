using System;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Logging;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public enum PreviewAtlasCategory
    {
        Floors,
        Walls,
        Objects,
        Grounds
    }

    public sealed class PreviewAtlasCatalog : IDisposable
    {
        private const string ResourceFolder = "Previews/";
        private readonly Dictionary<PreviewAtlasCategory, Atlas> _atlases = new Dictionary<PreviewAtlasCategory, Atlas>();
        private readonly ICategoryLogger _logger;

        public static readonly LogCategory Category = new LogCategory("PreviewAtlases");

        private sealed class Atlas
        {
            public readonly Texture2D Texture;
            public readonly PreviewAtlasManifest Manifest;
            public readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
            public readonly Dictionary<string, PreviewAtlasEntry> Entries = new Dictionary<string, PreviewAtlasEntry>();

            public Atlas(Texture2D texture, PreviewAtlasManifest manifest)
            {
                Texture = texture;
                Manifest = manifest;
            }
        }

        public PreviewAtlasCatalog(ILoggerSource loggerSource)
        {
            _logger = loggerSource.Create(Category);
            Load(PreviewAtlasCategory.Floors);
            Load(PreviewAtlasCategory.Walls);
            Load(PreviewAtlasCategory.Objects);
            Load(PreviewAtlasCategory.Grounds);
        }

        public bool TryGetSprite(PreviewAtlasCategory category, string shortName, out Sprite sprite)
        {
            sprite = null;
            if (!_atlases.TryGetValue(category, out Atlas atlas) ||
                !atlas.Entries.TryGetValue(shortName, out PreviewAtlasEntry entry))
            {
                return false;
            }

            if (atlas.Sprites.TryGetValue(shortName, out sprite))
            {
                return true;
            }

            int row = entry.index / atlas.Manifest.columns;
            int column = entry.index % atlas.Manifest.columns;
            Rect rect = new Rect(column * atlas.Manifest.cellSize, row * atlas.Manifest.cellSize,
                atlas.Manifest.cellSize, atlas.Manifest.cellSize);
            if (rect.xMax > atlas.Texture.width || rect.yMax > atlas.Texture.height)
            {
                return false;
            }

            sprite = Sprite.Create(atlas.Texture, rect, new Vector2(0.5f, 0.5f));
            sprite.name = category + "/" + shortName;
            atlas.Sprites.Add(shortName, sprite);
            return true;
        }

        public void Dispose()
        {
            foreach (Atlas atlas in _atlases.Values)
            {
                foreach (Sprite sprite in atlas.Sprites.Values)
                {
                    Object.Destroy(sprite);
                }
            }
        }

        private void Load(PreviewAtlasCategory category)
        {
            string name = category.ToString().ToLowerInvariant();
            Texture2D texture = Resources.Load<Texture2D>(ResourceFolder + name);
            TextAsset manifestAsset = Resources.Load<TextAsset>(ResourceFolder + name);
            if (!texture || !manifestAsset)
            {
                _logger.Warning("Preview atlas unavailable for " + name + "; entries will be text-only");
                return;
            }

            PreviewAtlasManifest manifest;
            try
            {
                manifest = JsonUtility.FromJson<PreviewAtlasManifest>(manifestAsset.text);
            }
            catch (Exception exception)
            {
                _logger.Warning("Preview atlas manifest is invalid for " + name + ": " + exception.Message);
                return;
            }

            if (manifest == null || manifest.category != name || manifest.cellSize <= 0 || manifest.columns <= 0)
            {
                _logger.Warning("Preview atlas manifest has invalid metadata for " + name);
                return;
            }

            Atlas atlas = new Atlas(texture, manifest);
            foreach (PreviewAtlasEntry entry in manifest.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.shortName) || entry.index < 0 ||
                    !atlas.Entries.TryAdd(entry.shortName, entry))
                {
                    _logger.Warning("Preview atlas manifest has an invalid or duplicate entry for " + name);
                    return;
                }
            }

            _atlases.Add(category, atlas);
        }
    }
}
