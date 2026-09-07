using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveRenderer : MonoBehaviour
    {
        public const int ChunkSize = 16;

        public int RenderRebuildCount { get; private set; }
        public int ColliderRebuildCount { get; private set; }
        public int ChunkCount => _chunks.Count;

        private readonly List<CaveChunk> _chunks = new List<CaveChunk>();
        private readonly HashSet<CaveChunk> _renderDirty = new HashSet<CaveChunk>();
        private readonly HashSet<CaveChunk> _colliderDirty = new HashSet<CaveChunk>();
        private ICaveEditor _editor;
        private CaveTopologyBuilder _builder;
        private CaveTextureArray _textures;
        private Material _material;
        private ICaveRenderOptions _renderOptions;

        public void Initialize(int width, int height, ICaveMap map, ICaveEditor editor, CaveData defaultTerrain,
            Material material, CaveTextureArray textures, ICaveRenderOptions renderOptions)
        {
            _editor = editor;
            _textures = textures;
            _renderOptions = renderOptions;
            _material = new Material(material) { name = "Cave Shell Material" };
            _textures.ApplyTo(_material);
            ApplyCulling();
            _builder = new CaveTopologyBuilder(map, textures, defaultTerrain, width, height);

            for (int x = 0; x < width; x += ChunkSize)
            {
                for (int y = 0; y < height; y += ChunkSize)
                {
                    int chunkWidth = Mathf.Min(ChunkSize, width - x);
                    int chunkHeight = Mathf.Min(ChunkSize, height - y);
                    GameObject chunkObject = new GameObject($"Cave Chunk {x / ChunkSize},{y / ChunkSize}");
                    chunkObject.transform.SetParent(transform, false);
                    chunkObject.transform.localPosition = new Vector3(x * 4f, 0f, y * 4f);
                    CaveChunk chunk = chunkObject.AddComponent<CaveChunk>();
                    chunk.Initialize(x, y, chunkWidth, chunkHeight, _material);
                    _chunks.Add(chunk);
                    _renderDirty.Add(chunk);
                    _colliderDirty.Add(chunk);
                }
            }

            _editor.Changed += OnCavesChanged;
            _editor.EditCompleted += OnCaveEditCompleted;
            _renderOptions.Changed += ApplyCulling;
            LoadTexturesAsync();
        }

        private void ApplyCulling()
        {
            _material.SetFloat("_Cull", _renderOptions.IsCullingEnabled()
                ? (float)UnityEngine.Rendering.CullMode.Back
                : (float)UnityEngine.Rendering.CullMode.Off);
        }

        public CaveChunk GetChunk(int index)
        {
            return _chunks[index];
        }

        private async void LoadTexturesAsync()
        {
            await _textures.LoadAsync();
            if (this)
            {
                MarkAllRenderDirty();
            }
        }

        private void OnCavesChanged(CaveDirtyRegion region)
        {
            foreach (CaveChunk chunk in _chunks)
            {
                if (region.TouchesChunk(chunk.MinimumX, chunk.MinimumY, chunk.Width, chunk.Height))
                {
                    _renderDirty.Add(chunk);
                }
            }
        }

        private void OnCaveEditCompleted(CaveDirtyRegion region)
        {
            foreach (CaveChunk chunk in _chunks)
            {
                if (region.TouchesChunk(chunk.MinimumX, chunk.MinimumY, chunk.Width, chunk.Height))
                {
                    _colliderDirty.Add(chunk);
                }
            }
        }

        private void LateUpdate()
        {
            if (_renderDirty.Count > 0)
            {
                foreach (CaveChunk chunk in _renderDirty)
                {
                    chunk.RebuildRender(_builder);
                    RenderRebuildCount++;
                }
                _renderDirty.Clear();
            }

            if (_colliderDirty.Count > 0)
            {
                foreach (CaveChunk chunk in _colliderDirty)
                {
                    chunk.RebuildCollider(_builder);
                    ColliderRebuildCount++;
                }
                _colliderDirty.Clear();
            }
        }

        private void MarkAllRenderDirty()
        {
            foreach (CaveChunk chunk in _chunks)
            {
                _renderDirty.Add(chunk);
            }
        }

        private void OnDestroy()
        {
            if (_editor != null)
            {
                _editor.Changed -= OnCavesChanged;
                _editor.EditCompleted -= OnCaveEditCompleted;
            }

            if (_renderOptions != null)
            {
                _renderOptions.Changed -= ApplyCulling;
            }

            if (_material)
            {
                if (Application.isPlaying)
                {
                    Destroy(_material);
                }
                else
                {
                    DestroyImmediate(_material);
                }
            }
        }
    }
}
