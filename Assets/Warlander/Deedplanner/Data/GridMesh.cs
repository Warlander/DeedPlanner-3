using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Data
{
    public class GridMesh : MonoBehaviour
    {
        [Inject] private CameraCoordinator _cameraCoordinator;
        [Inject] private DiContainer _container;
        
        private Map map;
        private bool cave;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private Mesh mesh;
        private Vector3[] vertices;
        private Color[] uniformColors;
        private Color[] heightColors;
        private bool renderHeightColors = false;
        private bool verticesChanged = false;
        private bool dirty = false;

        private HeightmapHandle[,] heightmapHandles;
        private Dictionary<Color, List<Matrix4x4>> heightmapRenderCache;
        private Dictionary<Color, MaterialPropertyBlock> heightmapPropertiesCache;

        public bool HandlesVisible { get; set; }

        public void Initialize(Map map, bool cave)
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
            this.cave = cave;
            heightmapHandles = new HeightmapHandle[map.Width + 1, map.Height + 1];
            heightmapRenderCache = new Dictionary<Color, List<Matrix4x4>>();
            heightmapPropertiesCache = new Dictionary<Color, MaterialPropertyBlock>();

            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            vertices = new Vector3[(map.Width + 1) * (map.Height + 1)];
            uniformColors = new Color[(map.Width + 1) * (map.Height + 1)];
            heightColors = new Color[(map.Width + 1) * (map.Height + 1)];

            for (int i = 0; i <= map.Width; i++)
            {
                for (int i2 = 0; i2 <= map.Height; i2++)
                {
                    int index = map.CoordinateToIndex(i, i2);
                    vertices[index] = new Vector3(i * 4, 0, i2 * 4);
                    uniformColors[index] = new Color(1, 1, 1);
                    heightColors[index] = new Color(1, 1, 1);

                    HeightmapHandle newHandle = new HeightmapHandle(new Vector2Int(i, i2), 0);
                    _container.Inject(newHandle);
                    
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
            meshRenderer.sharedMaterial = GraphicsManager.Instance.SimpleDrawingMaterial;

            verticesChanged = false;
            dirty = false;
        }

        private void Update()
        {
            if (HandlesVisible)
            {
                RenderHandles();
            }
        }

        private void RenderHandles()
        {
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

            Material drawMaterial = GraphicsManager.Instance.SimpleDrawingMaterial;
            
            foreach (KeyValuePair<Color, List<Matrix4x4>> pair in heightmapRenderCache)
            {
                Color drawColor = pair.Key;
                MaterialPropertyBlock drawPropertyBlock = heightmapPropertiesCache[drawColor];

                List<Matrix4x4> matrices = pair.Value;

                Mesh heightmapMesh = GameManager.Instance.HeightmapHandleMesh;

                const int batchSize = 1023; // max allowed batch size for DrawMeshInstanced
                for (int i = 0; i < matrices.Count; i += batchSize)
                {
                    int currentBatchSize = Math.Min(matrices.Count - i, batchSize);
                    List<Matrix4x4> currentBatch = matrices.GetRange(i, currentBatchSize);
                    UnityEngine.Graphics.DrawMeshInstanced(heightmapMesh, 0, drawMaterial, currentBatch, drawPropertyBlock, ShadowCastingMode.Off, false);
                }
            }

            foreach (KeyValuePair<Color, List<Matrix4x4>> pair in heightmapRenderCache)
            {
                pair.Value.Clear();
            }
        }

        public HeightmapHandle RaycastHandles()
        {
            Ray ray = _cameraCoordinator.Current.CreateMouseRay();

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
            int pos = map.CoordinateToIndex(x, y);
            Vector3 newVector = new Vector3(x * 4, height * 0.1f, y * 4);
            if (newVector != vertices[pos])
            {
                vertices[pos] = newVector;

                HeightmapHandle handle = heightmapHandles[x, y];
                handle.Slope = height;

                verticesChanged = true;
                dirty = true;
            }
        }

        public HeightmapHandle GetHandle(int x, int y)
        {
            return heightmapHandles[x, y];
        }

        public void ApplyAllChanges()
        {
            if (!dirty)
            {
                return;
            }

            if (verticesChanged)
            {
                float highestHeight = cave ? map.HighestCaveHeight : map.HighestSurfaceHeight;
                float lowestHeight = cave ? map.LowestCaveHeight : map.LowestSurfaceHeight;
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
                        float cornerHeight = cave ? map[i, i2].CaveHeight : map[i, i2].SurfaceHeight;
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
    }
}