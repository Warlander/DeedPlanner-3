using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Render;

namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveTextureArray : ICaveTextureIndex, IDisposable
    {
        private const int TextureWidth = 512;
        private const int TextureHeight = 512;

        private readonly CaveData _defaultTerrain;
        private readonly List<TextureReference> _references;
        private readonly Dictionary<TextureReference, int> _indices = new Dictionary<TextureReference, int>();
        private readonly IndexedTextureArray<TextureReference> _textures;
        private Task _loadTask;
        private int _defaultIndex;

        public CaveTextureArray(IDataCatalog dataCatalog)
        {
            _defaultTerrain = dataCatalog.DefaultCaveData;
            var uniqueReferences = new HashSet<TextureReference>();
            foreach (CaveData terrain in dataCatalog.GetAllCaves())
            {
                if (!terrain.Entrance && terrain.Texture != null)
                {
                    uniqueReferences.Add(terrain.Texture);
                }
            }

            uniqueReferences.Add(_defaultTerrain.Texture);
            _references = new List<TextureReference>(uniqueReferences);
            _references.Remove(_defaultTerrain.Texture);
            _textures = new IndexedTextureArray<TextureReference>(TextureWidth, TextureHeight, uniqueReferences.Count);
        }

        public Task LoadAsync()
        {
            _loadTask ??= LoadAllAsync();
            return _loadTask;
        }

        public void ApplyTo(Material material)
        {
            material.SetTexture("_MainTex", _textures.TextureArray);
        }

        public int GetIndex(CaveData terrain)
        {
            if (terrain?.Texture != null && _indices.TryGetValue(terrain.Texture, out int index))
            {
                return index;
            }

            return _defaultIndex;
        }

        private async Task LoadAllAsync()
        {
            _defaultIndex = await LoadAsync(_defaultTerrain.Texture, 0);
            foreach (TextureReference reference in _references)
            {
                _indices[reference] = await LoadAsync(reference, _defaultIndex);
            }
        }

        private async Task<int> LoadAsync(TextureReference reference, int fallbackIndex)
        {
            try
            {
                Texture2D texture = await reference.LoadOrGetTextureAsync();
                if (texture && _textures.IsValid)
                {
                    int index = _textures.PutOrGetTexture(reference, texture);
                    if (index >= 0)
                    {
                        _indices[reference] = index;
                        return index;
                    }
                }
            }
            catch
            {
                // Missing optional Wurm assets use the default stone texture.
            }

            _indices[reference] = fallbackIndex;
            return fallbackIndex;
        }

        public void Dispose()
        {
            _textures.Dispose();
        }
    }
}
