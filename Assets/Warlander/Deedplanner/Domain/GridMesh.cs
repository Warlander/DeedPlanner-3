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
        private const double HandleSpacing = 4;
        private const double HandleHalfSize = 0.6f * 0.5f;
        private const double FloatRelativePrecision = 1.1920928955078125e-7;

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
            int width = heightmapHandles.GetLength(0);
            int height = heightmapHandles.GetLength(1);
            Vector3 origin = ray.origin;
            Vector3 direction = ray.direction;
            if (!IsFinite(origin.x) || !IsFinite(origin.y) || !IsFinite(origin.z) ||
                !IsFinite(direction.x) || !IsFinite(direction.y) || !IsFinite(direction.z) ||
                direction.Equals(Vector3.zero))
                return RaycastAllHandles(ray);

            double dx = direction.x;
            double dz = direction.z;
            double padding = 64 * FloatRelativePrecision *
                (Math.Abs((double)origin.x) + Math.Abs((double)origin.z) + HandleSpacing * ((double)width + height) + 1);
            double majorDirection = Math.Max(Math.Abs(dx), Math.Abs(dz));
            double maximumTravel = majorDirection == 0 ? 0 :
                (Math.Max(Math.Abs((double)origin.x), Math.Abs((double)origin.z)) + HandleSpacing * Math.Max(width, height) + 4) / majorDirection;
            if (padding >= HandleSpacing * Math.Max(width, height) + 8 || maximumTravel > float.MaxValue / 32.0)
                return RaycastAllHandles(ray);

            float closestDistance = float.MaxValue;
            HeightmapHandle closestHandle = null;
            long closestOrdinal = long.MaxValue;
            if (majorDirection == 0)
            {
                if (GetHandleBandRange(origin.x, HandleSpacing * 0.5 + HandleHalfSize + padding, width, out int firstX, out int lastX) &&
                    GetHandleBandRange(origin.z, HandleSpacing * 0.5 + HandleHalfSize + padding, height, out int firstZ, out int lastZ))
                    for (int x = firstX; x <= lastX; x++)
                        for (int z = firstZ; z <= lastZ; z++)
                            TestHandle(ray, x, z, height, ref closestDistance, ref closestHandle, ref closestOrdinal);
            }
            else
            {
                bool xMajor = Math.Abs(dx) >= Math.Abs(dz);
                double ratio = xMajor ? dz / dx : dx / dz;
                double majorOrigin = xMajor ? origin.x : origin.z;
                double minorOrigin = xMajor ? origin.z : origin.x;
                int majorLength = xMajor ? width : height;
                int minorLength = xMajor ? height : width;
                double radius = HandleSpacing * 0.5 * Math.Abs(ratio) + HandleHalfSize + padding;
                for (int major = 0; major < majorLength; major++)
                {
                    double projected = minorOrigin + ratio * (HandleSpacing * major - majorOrigin);
                    if (!GetHandleBandRange(projected, radius, minorLength, out int first, out int last)) continue;
                    for (int minor = first; minor <= last; minor++)
                        TestHandle(ray, xMajor ? major : minor, xMajor ? minor : major, height,
                            ref closestDistance, ref closestHandle, ref closestOrdinal);
                }
            }
            return closestHandle;
        }

        private void TestHandle(Ray ray, int x, int z, int height, ref float closestDistance,
            ref HeightmapHandle closestHandle, ref long closestOrdinal)
        {
            HeightmapHandle handle = heightmapHandles[x, z];
            float distance = handle.Raycast(ray);
            if (distance < 0) return;
            long ordinal = (long)x * height + z;
            if (distance < closestDistance || (closestHandle != null && distance == closestDistance && ordinal < closestOrdinal))
            {
                closestDistance = distance;
                closestHandle = handle;
                closestOrdinal = ordinal;
            }
        }

        private static bool GetHandleBandRange(double center, double radius, int length, out int first, out int last)
        {
            first = 0;
            last = -1;
            if (length == 0 || center + radius < 0 || center - radius > HandleSpacing * (length - 1)) return false;
            // Outward rounding includes an extra row on both sides of the geometric band.
            first = (int)Math.Max(0, Math.Min(length - 1, Math.Floor((center - radius) / HandleSpacing)));
            last = (int)Math.Max(0, Math.Min(length - 1, Math.Ceiling((center + radius) / HandleSpacing)));
            return first <= last;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private HeightmapHandle RaycastAllHandles(Ray ray)
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
