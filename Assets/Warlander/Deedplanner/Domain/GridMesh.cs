using System;
using Warlander.Deedplanner.Editing;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Deedplanner.Ui;
using Warlander.ExtensionUtils;
using VContainer;

namespace Warlander.Deedplanner.Domain
{
    public class GridMesh : MonoBehaviour
    {
        [Inject] private HeightmapHandleMeshLoader _heightmapHandleMeshLoader;
        [Inject] private ISharedMaterials _sharedMaterials;
        
        private Map map;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private Mesh mesh;
        private Vector3[] vertices;
        private Color[] uniformColors;
        private Color[] heightColors;
        private int[] displayValues;
        private float _alphaMultiplier;
        private bool renderHeightColors = false;
        private bool verticesChanged = false;
        private bool dirty = false;

        private HeightmapHandle[,] heightmapHandles;
        private Dictionary<Color, List<Matrix4x4>> heightmapRenderCache;
        private Dictionary<Color, MaterialPropertyBlock> heightmapPropertiesCache;

        public bool HandlesVisible { get; set; }

        public IDisposable PrepareForCamera(bool showHeightColors, float alphaMultiplier, Material material)
        {
            var scope = new CameraScope(this, HandlesVisible, renderHeightColors, _alphaMultiplier,
                meshRenderer.sharedMaterial);
            HandlesVisible = showHeightColors;
            SetRenderHeightColors(showHeightColors);
            SetAlphaMultiplier(alphaMultiplier);
            SetMaterial(material);
            ApplyAllChanges();
            return scope;
        }

        public void Initialize(Map map)
        {
            meshFilter = GetComponent<MeshFilter>();
            if (!meshFilter)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            meshRenderer = GetComponent<MeshRenderer>();
            if (!meshRenderer)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            this.map = map;
            heightmapHandles = new HeightmapHandle[map.Width + 1, map.Height + 1];
            heightmapRenderCache = new Dictionary<Color, List<Matrix4x4>>();
            heightmapPropertiesCache = new Dictionary<Color, MaterialPropertyBlock>();

            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            vertices = new Vector3[(map.Width + 1) * (map.Height + 1)];
            uniformColors = new Color[(map.Width + 1) * (map.Height + 1)];
            heightColors = new Color[(map.Width + 1) * (map.Height + 1)];
            displayValues = new int[(map.Width + 1) * (map.Height + 1)];

            for (int i = 0; i <= map.Width; i++)
            {
                for (int i2 = 0; i2 <= map.Height; i2++)
                {
                    int index = map.CoordinateToIndex(i, i2);
                    vertices[index] = new Vector3(i * 4, 0, i2 * 4);
                    uniformColors[index] = new Color(1, 1, 1, _alphaMultiplier);
                    heightColors[index] = new Color(1, 1, 1, _alphaMultiplier);

                    HeightmapHandle newHandle = new HeightmapHandle(new Vector2Int(i, i2), 0);

                    heightmapHandles[i, i2] = newHandle;
                }
            }

            mesh.vertices = vertices;
            mesh.colors = uniformColors;

            int[] indices = new int[(map.Width + 1) * (map.Height + 1) * 4];
            for (int i = 0; i < map.Width; i++)
            {
                for (int i2 = 0; i2 < map.Height; i2++)
                {
                    int verticeIndex = map.CoordinateToIndex(i, i2);
                    int arrayIndex = verticeIndex * 4;
                    indices[arrayIndex] = verticeIndex;
                    indices[arrayIndex + 1] = verticeIndex + 1;
                    indices[arrayIndex + 2] = verticeIndex;
                    indices[arrayIndex + 3] = verticeIndex + map.Height + 1;
                }
            }

            mesh.SetIndices(indices, MeshTopology.Lines, 0, true);

            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = _sharedMaterials.SimpleSubtleDrawingMaterial;

            verticesChanged = false;
            dirty = false;
        }

        public void RenderHandles(Camera targetCamera)
        {
            if (!HandlesVisible)
            {
                return;
            }

            foreach (HeightmapHandle heightmapHandle in heightmapHandles)
            {
                Color color = heightmapHandle.Color;

                if (!heightmapRenderCache.ContainsKey(color))
                {
                    heightmapRenderCache[color] = new List<Matrix4x4>();

                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                    propertyBlock.SetColor(ShaderPropertyIds.Color, color);
                    heightmapPropertiesCache[color] = propertyBlock;

                }

                heightmapRenderCache[color].Add(heightmapHandle.TransformMatrix);
            }

            Material drawMaterial = _sharedMaterials.SimpleDrawingMaterial;
            
            foreach (KeyValuePair<Color, List<Matrix4x4>> pair in heightmapRenderCache)
            {
                Color drawColor = pair.Key;
                MaterialPropertyBlock drawPropertyBlock = heightmapPropertiesCache[drawColor];

                List<Matrix4x4> matrices = pair.Value;

                Mesh heightmapMesh = _heightmapHandleMeshLoader.GetMesh();

                const int batchSize = 1023; // max allowed batch size for DrawMeshInstanced
                for (int i = 0; i < matrices.Count; i += batchSize)
                {
                    int currentBatchSize = Math.Min(matrices.Count - i, batchSize);
                    List<Matrix4x4> currentBatch = matrices.GetRange(i, currentBatchSize);
                    UnityEngine.Graphics.DrawMeshInstanced(heightmapMesh, 0, drawMaterial, currentBatch,
                        drawPropertyBlock, ShadowCastingMode.Off, false, gameObject.layer, targetCamera);
                }
            }

            foreach (KeyValuePair<Color, List<Matrix4x4>> pair in heightmapRenderCache)
            {
                pair.Value.Clear();
            }
        }

        public HeightmapHandle RaycastHandles(Ray ray)
        {
            float closestDistance = float.MaxValue;
            HeightmapHandle closestHandle = null;
            
            foreach (HeightmapHandle heightmapHandle in heightmapHandles)
            {
                float distance = heightmapHandle.Raycast(ray);
                if (distance < 0)
                {
                    continue;
                }
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestHandle = heightmapHandle;
                }
            }

            return closestHandle;
        }

        public void SetRenderHeightColors(bool renderHeightColors)
        {
            if (this.renderHeightColors != renderHeightColors)
            {
                this.renderHeightColors = renderHeightColors;
                dirty = true;
            }
        }

        public void SetHeight(int x, int y, int height)
        {
            SetDisplayHeight(x, y, height, height);
        }

        public void SetDisplayHeight(int x, int y, int worldHeight, int displayValue)
        {
            int pos = map.CoordinateToIndex(x, y);
            Vector3 newVector = new Vector3(x * 4, worldHeight * 0.1f, y * 4);
            bool valueChanged = displayValues[pos] != displayValue;
            if (newVector != vertices[pos] || valueChanged)
            {
                vertices[pos] = newVector;
                displayValues[pos] = displayValue;

                HeightmapHandle handle = heightmapHandles[x, y];
                handle.Slope = worldHeight;

                verticesChanged = true;
                dirty = true;
            }
        }

        public void SetMaterial(Material newMaterial)
        {
            meshRenderer.sharedMaterial = newMaterial;
        }

        public void SetAlphaMultiplier(float multiplier)
        {
            if (Math.Abs(multiplier - _alphaMultiplier) > 0.001f)
            {
                _alphaMultiplier = multiplier;
                
                for (int i = 0; i < uniformColors.Length; i++)
                {
                    uniformColors[i] = uniformColors[i].SetA(_alphaMultiplier);
                    heightColors[i] = heightColors[i].SetA(_alphaMultiplier);
                }
                
                dirty = true;
            }
        }

        public HeightmapHandle GetHandle(int x, int y)
        {
            return heightmapHandles[x, y];
        }

        public void WriteSlopeGridData(Vector2Int coordinates, int[] heightsBuffer)
        {
            int defaultValue = GetDisplayValue(coordinates.x, coordinates.y, 0);
            int index = 0;
            for (int y = 1; y >= -1; y--)
            {
                for (int x = -1; x <= 1; x++)
                {
                    heightsBuffer[index++] = GetDisplayValue(coordinates.x + x,
                        coordinates.y + y, defaultValue);
                }
            }
        }

        private int GetDisplayValue(int x, int y, int defaultValue)
        {
            if (x < 0 || y < 0 || x > map.Width || y > map.Height)
            {
                return defaultValue;
            }
            return displayValues[map.CoordinateToIndex(x, y)];
        }

        public void ApplyAllChanges()
        {
            if (!dirty)
            {
                return;
            }

            if (verticesChanged)
            {
                int lowestHeight = int.MaxValue;
                int highestHeight = int.MinValue;
                foreach (int value in displayValues)
                {
                    lowestHeight = Math.Min(lowestHeight, value);
                    highestHeight = Math.Max(highestHeight, value);
                }
                float heightDelta = highestHeight - lowestHeight;
                if (heightDelta == 0)
                {
                    heightDelta = 1;
                }

                for (int i = 0; i <= map.Width; i++)
                {
                    for (int i2 = 0; i2 <= map.Height; i2++)
                    {
                        int index = map.CoordinateToIndex(i, i2);
                        float cornerHeight = displayValues[index];
                        float cornerColorComponent = (cornerHeight - lowestHeight) / heightDelta;
                        heightColors[index] = new Color(cornerColorComponent, 1f - cornerColorComponent, 0, 1);
                    }
                }
            }

            if (verticesChanged)
            {
                mesh.vertices = vertices;
                mesh.RecalculateBounds();
            }

            if (renderHeightColors)
            {
                mesh.colors = heightColors;
            }
            else
            {
                mesh.colors = uniformColors;
            }

            mesh.UploadMeshData(false);

            dirty = false;
            verticesChanged = false;
        }

        private sealed class CameraScope : IDisposable
        {
            private GridMesh _grid;
            private readonly bool _handlesVisible;
            private readonly bool _renderHeightColors;
            private readonly float _alphaMultiplier;
            private readonly Material _material;

            public CameraScope(GridMesh grid, bool handlesVisible, bool renderHeightColors,
                float alphaMultiplier, Material material)
            {
                _grid = grid;
                _handlesVisible = handlesVisible;
                _renderHeightColors = renderHeightColors;
                _alphaMultiplier = alphaMultiplier;
                _material = material;
            }

            public void Dispose()
            {
                if (_grid == null)
                {
                    return;
                }

                _grid.HandlesVisible = _handlesVisible;
                _grid.SetRenderHeightColors(_renderHeightColors);
                _grid.SetAlphaMultiplier(_alphaMultiplier);
                _grid.SetMaterial(_material);
                _grid.ApplyAllChanges();
                _grid = null;
            }
        }
    }
}
