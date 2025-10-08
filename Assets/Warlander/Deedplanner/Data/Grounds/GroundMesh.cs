using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logic;
using Warlander.Render;

namespace Warlander.Deedplanner.Data.Grounds
{
    public class GroundMesh : MonoBehaviour
    {
        private const int GroundTexturesWidth = 512;
        private const int GroundTexturesHeight = 512;
        
        private static IndexedTextureArray<TextureReference> groundTexturesArray;
        
        private const int VerticesPerRenderTile = 12;
        private const int TrianglesPerTile = 12; // 4 triangles, 4*3
        private const float TileSize = 4f;
        private const float SlopeToHeight = 0.1f;

        private Material material;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        private OverlayMesh overlayMesh;

        public Mesh RenderMesh { get; private set; }
        public Mesh ColliderMesh { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private int[,] slopesArray;
        private GroundData[,] dataArray;
        private RoadDirection[,] directionsArray;
        
        private Vector3[] renderVertices;
        private Vector2[] uv2;
        private Vector3[] colliderVertices;

        private bool needsVerticesUpdate = false;
        private bool needsUvUpdate = false;

        public void Initialize(int width, int height, OverlayMesh newOverlayMesh)
        {
            gameObject.layer = LayerMasks.GroundLayer;
            if (groundTexturesArray == null)
            {
                int groundTexturesCount = CalculateGroundTexturesCount();
                groundTexturesArray = new IndexedTextureArray<TextureReference>(GroundTexturesWidth, GroundTexturesHeight, groundTexturesCount);
            }
            
            gameObject.layer = LayerMasks.GroundLayer;

            if (!material)
            {
                material = new Material(GraphicsManager.Instance.TerrainMaterial);
                material.mainTexture = groundTexturesArray.TextureArray;
            }

            if (!meshRenderer)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (!meshFilter)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            if (!meshCollider)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            
            meshRenderer.sharedMaterial = material;
            
            Width = width;
            Height = height;
            overlayMesh = newOverlayMesh;
            slopesArray = new int[Width + 1, Height + 1];
            dataArray = new GroundData[Width, Height];
            directionsArray = new RoadDirection[Width, Height];

            RenderMesh = new Mesh();
            RenderMesh.name = "ground render mesh";
            meshFilter.sharedMesh = RenderMesh;
            ColliderMesh = new Mesh();
            ColliderMesh.name = "ground collider mesh";
            meshCollider.sharedMesh = ColliderMesh;
            
            InitializeRenderMesh();
            InitializeColliderMesh();
            
            needsVerticesUpdate = true;
            needsUvUpdate = true;
        }

        private int CalculateGroundTexturesCount()
        {
            HashSet<TextureReference> textures = new HashSet<TextureReference>();

            foreach (KeyValuePair<string, GroundData> pair in Database.Grounds)
            {
                textures.Add(pair.Value.Tex3d);
            }

            return textures.Count;
        }

        private void InitializeRenderMesh()
        {
            int totalRenderVertices = Width * Height * VerticesPerRenderTile;
            bool use32BitIndexing = totalRenderVertices >= short.MaxValue;
            RenderMesh.indexFormat = use32BitIndexing ? IndexFormat.UInt32 : IndexFormat.UInt16;
            
            renderVertices = InitializeRenderVertices(Width, Height);
            RenderMesh.vertices = renderVertices;
            
            Vector2[] uv = InitializeRenderUV(Width, Height);
            RenderMesh.uv = uv;
            
            uv2 = InitializeRenderUV2(Width, Height);
            
            int[] triangles = InitializeRenderTriangles(Width, Height);
            RenderMesh.triangles = triangles;
        }

        private static Vector3[] InitializeRenderVertices(int width, int height)
        {
            int totalVertices = width * height * VerticesPerRenderTile;
            Vector3[] newVertices = new Vector3[totalVertices];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int index = x * height + y;
                    int vertexIndex = index * VerticesPerRenderTile;

                    Vector3 v00 = VertexVector(x, y, false);
                    Vector3 v10 = VertexVector(x + 1, y, false);
                    Vector3 v01 = VertexVector(x, y + 1, false);
                    Vector3 v11 = VertexVector(x + 1, y + 1, false);
                    Vector3 vCenter = VertexVector(x, y, true);

                    newVertices[vertexIndex] = v00;
                    newVertices[vertexIndex + 1] = v01;
                    newVertices[vertexIndex + 2] = vCenter;
                
                    newVertices[vertexIndex + 3] = v01;
                    newVertices[vertexIndex + 4] = v11;
                    newVertices[vertexIndex + 5] = vCenter;
                
                    newVertices[vertexIndex + 6] = v11;
                    newVertices[vertexIndex + 7] = v10;
                    newVertices[vertexIndex + 8] = vCenter;
                
                    newVertices[vertexIndex + 9] = v10;
                    newVertices[vertexIndex + 10] = v00;
                    newVertices[vertexIndex + 11] = vCenter;
                }
            }

            return newVertices;
        }
        
        private static int[] InitializeRenderTriangles(int width, int height)
        {
            int totalTriangles = width * height * TrianglesPerTile;
            int[] newTriangles = new int[totalTriangles];

            for (int i = 0; i < totalTriangles; i++)
            {
                newTriangles[i] = i;
            }
            
            return newTriangles;
        }
        
        private static Vector2[] InitializeRenderUV(int width, int height)
        {
            int totalVertices = width * height * VerticesPerRenderTile;
            Vector2[] newUV = new Vector2[totalVertices];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int index = x * height + y;
                    int vertexIndex = index * VerticesPerRenderTile;

                    Vector2 uv00 = new Vector2(0, 0);
                    Vector2 uv10 = new Vector2(1, 0);
                    Vector2 uv01 = new Vector2(0, 1);
                    Vector2 uv11 = new Vector2(1, 1);
                    Vector2 uvCenter = new Vector2(0.5f, 0.5f);

                    newUV[vertexIndex] = uv00;
                    newUV[vertexIndex + 1] = uv01;
                    newUV[vertexIndex + 2] = uvCenter;
                
                    newUV[vertexIndex + 3] = uv01;
                    newUV[vertexIndex + 4] = uv11;
                    newUV[vertexIndex + 5] = uvCenter;
                
                    newUV[vertexIndex + 6] = uv11;
                    newUV[vertexIndex + 7] = uv10;
                    newUV[vertexIndex + 8] = uvCenter;
                
                    newUV[vertexIndex + 9] = uv10;
                    newUV[vertexIndex + 10] = uv00;
                    newUV[vertexIndex + 11] = uvCenter;
                }
            }

            return newUV;
        }
        
        private static Vector2[] InitializeRenderUV2(int width, int height)
        {
            int totalVertices = width * height * VerticesPerRenderTile;
            Vector2[] newUV2 = new Vector2[totalVertices];
            Vector2 defaultUV2 = new Vector2(-1, 0);
            
            for (int i = 0; i < totalVertices; i++)
            {
                newUV2[i] = defaultUV2;
            }
            
            return newUV2;
        }
        
        private void InitializeColliderMesh()
        {
            int totalColliderVertices = CalculateColliderVerticesCount(Width, Height);
            bool use32BitIndexing = totalColliderVertices >= short.MaxValue;
            ColliderMesh.indexFormat = use32BitIndexing ? IndexFormat.UInt32 : IndexFormat.UInt16;
            
            colliderVertices = InitializeColliderVertices(Width, Height);
            ColliderMesh.vertices = colliderVertices;
            
            int[] triangles = InitializeColliderTriangles(Width, Height);
            ColliderMesh.triangles = triangles;
        }
        
        private static Vector3[] InitializeColliderVertices(int width, int height)
        {
            int totalVertices = CalculateColliderVerticesCount(width, height);
            Vector3[] newVertices = new Vector3[totalVertices];

            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    int index = ColliderVertexIndex(x, y, width, height, false);
                    newVertices[index] = VertexVector(x, y, false);
                }
            }
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int index = ColliderVertexIndex(x, y, width, height, true);
                    newVertices[index] = VertexVector(x, y, true);
                }
            }

            return newVertices;
        }

        private static int[] InitializeColliderTriangles(int width, int height)
        {
            int totalTriangles = width * height * TrianglesPerTile;
            int[] newTriangles = new int[totalTriangles];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int triangleIndex = (x * height + y) * TrianglesPerTile;
                    int i00 = ColliderVertexIndex(x, y, width, height, false);
                    int i10 = ColliderVertexIndex(x + 1, y, width, height, false);
                    int i01 = ColliderVertexIndex(x, y + 1, width, height, false);
                    int i11 = ColliderVertexIndex(x + 1, y + 1, width, height, false);
                    int iCenter = ColliderVertexIndex(x, y, width, height, true);
                    
                    newTriangles[triangleIndex] = i00;
                    newTriangles[triangleIndex + 1] = i01;
                    newTriangles[triangleIndex + 2] = iCenter;
                
                    newTriangles[triangleIndex + 3] = i01;
                    newTriangles[triangleIndex + 4] = i11;
                    newTriangles[triangleIndex + 5] = iCenter;
                
                    newTriangles[triangleIndex + 6] = i11;
                    newTriangles[triangleIndex + 7] = i10;
                    newTriangles[triangleIndex + 8] = iCenter;
                
                    newTriangles[triangleIndex + 9] = i10;
                    newTriangles[triangleIndex + 10] = i00;
                    newTriangles[triangleIndex + 11] = iCenter;
                }
            }

            return newTriangles;
        }

        private static int ColliderVertexIndex(int x, int y, int width, int height, bool center)
        {
            if (!center)
            {
                return x * (height + 1) + y;
            }
            else
            {
                int offset = (width + 1) * (height + 1);
                return offset + x * height + y;
            }
        }

        private static int CalculateColliderVerticesCount(int width, int height)
        {
            int gridVerticesCount = (width + 1) * (height + 1);
            int centerVerticesCount = width * height; // vertices in center of grid coordinates
            return gridVerticesCount + centerVerticesCount;
        }

        private static Vector3 VertexVector(int x, int y, bool center)
        {
            if (!center)
            {
                return new Vector3(x * TileSize, 0, y * TileSize);
            }
            else
            {
                return new Vector3((x + 0.5f) * TileSize, 0, (y + 0.5f) * TileSize);
            }
        }
        
        private void LateUpdate()
        {
            UpdateNow();
        }

        public void UpdateNow()
        {
            if (needsVerticesUpdate)
            {
                RenderMesh.vertices = renderVertices;
                RenderMesh.RecalculateNormals();
                RenderMesh.RecalculateBounds();

                ColliderMesh.vertices = colliderVertices;
                ColliderMesh.RecalculateBounds();
                // turning collider off and on to force it to update
                meshCollider.enabled = false;
                // ReSharper disable once Unity.InefficientPropertyAccess
                meshCollider.enabled = true;
                
                overlayMesh.UpdateCollider();
                
                needsVerticesUpdate = false;
            }

            if (needsUvUpdate)
            {
                RenderMesh.uv2 = uv2;
                needsUvUpdate = false;
            }
        }

        public int GetSlope(int x, int y)
        {
            return this[x, y];
        }

        public void SetSlope(int x, int y, int slope)
        {
            this[x, y] = slope;
        }
        
        public int this[int x, int y]
        {
            get => slopesArray[x, y];
            set
            {
                if (slopesArray[x, y] == value)
                {
                    return;
                }

                slopesArray[x, y] = value;
                float height = value * SlopeToHeight;

                UpdateRenderVertices(x, y, height);
                UpdateColliderVertices(x, y, height);
                
                needsVerticesUpdate = true;
            }
        }

        private void UpdateRenderVertices(int x, int y, float newHeight)
        {
            UpdateSouthWestRenderVertice(x, y, newHeight);
            UpdateSouthEashRenderVertice(x - 1, y, newHeight);
            UpdateNorthWestRenderVertice(x, y - 1, newHeight);
            UpdateNorthEastRenderVertice(x - 1, y - 1, newHeight);

            UpdateCentralRenderVertice(x - 1, y - 1);
            UpdateCentralRenderVertice(x, y - 1);
            UpdateCentralRenderVertice(x - 1, y);
            UpdateCentralRenderVertice(x, y);
        }

        private void UpdateSouthWestRenderVertice(int x, int y, float newHeight)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return;
            }
            
            int index = x * Height + y;
            int vertexIndex = index * VerticesPerRenderTile;
            
            //v00 pair of vertices
            renderVertices[vertexIndex].y = newHeight;
            renderVertices[vertexIndex + 10].y = newHeight;
        }
        
        private void UpdateSouthEashRenderVertice(int x, int y, float newHeight)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return;
            }
            
            int index = x * Height + y;
            int vertexIndex = index * VerticesPerRenderTile;

            //v10 pair of vertices
            renderVertices[vertexIndex + 7].y = newHeight;
            renderVertices[vertexIndex + 9].y = newHeight;
        }
        
        private void UpdateNorthWestRenderVertice(int x, int y, float newHeight)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return;
            }
            
            int index = x * Height + y;
            int vertexIndex = index * VerticesPerRenderTile;

            //v01 pair of vertices
            renderVertices[vertexIndex + 1].y = newHeight;
            renderVertices[vertexIndex + 3].y = newHeight;
        }
        
        private void UpdateNorthEastRenderVertice(int x, int y, float newHeight)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return;
            }
            
            int index = x * Height + y;
            int vertexIndex = index * VerticesPerRenderTile;

            //v11 pair of vertices
            renderVertices[vertexIndex + 4].y = newHeight;
            renderVertices[vertexIndex + 6].y = newHeight;
        }

        private void UpdateCentralRenderVertice(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return;
            }

            int s00 = slopesArray[x, y];
            int s10 = slopesArray[x + 1, y];
            int s01 = slopesArray[x, y + 1];
            int s11 = slopesArray[x + 1, y + 1];
            float height = (s00 + s10 + s01 + s11) * 0.25f * SlopeToHeight;
            
            int index = x * Height + y;
            int vertexIndex = index * VerticesPerRenderTile;

            //all central vertices of render mesh, due to multiple UV's
            renderVertices[vertexIndex + 2].y = height;
            renderVertices[vertexIndex + 5].y = height;
            renderVertices[vertexIndex + 8].y = height;
            renderVertices[vertexIndex + 11].y = height;
        }
        
        private void UpdateColliderVertices(int x, int y, float newHeight)
        {
            colliderVertices[ColliderVertexIndex(x, y, Width, Height, false)].y = newHeight;
            UpdateCentralColliderVertice(x - 1, y - 1);
            UpdateCentralColliderVertice(x, y - 1);
            UpdateCentralColliderVertice(x - 1, y);
            UpdateCentralColliderVertice(x, y);
        }

        private void UpdateCentralColliderVertice(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return;
            }

            float h00 = colliderVertices[ColliderVertexIndex(x, y, Width, Height, false)].y;
            float h10 = colliderVertices[ColliderVertexIndex(x + 1, y, Width, Height, false)].y;
            float h01 = colliderVertices[ColliderVertexIndex(x, y + 1, Width, Height, false)].y;
            float h11 = colliderVertices[ColliderVertexIndex(x + 1, y + 1, Width, Height, false)].y;
            float hCenter = (h00 + h10 + h01 + h11) * 0.25f;
            colliderVertices[ColliderVertexIndex(x, y, Width, Height, true)].y = hCenter;
        }

        public GroundData GetGroundData(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return null;
            }

            return dataArray[x, y];
        }
        
        public RoadDirection GetRoadDirection(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return RoadDirection.Center;
            }

            return directionsArray[x, y];
        }
        
        public void SetGroundData(int x, int y, GroundData data, RoadDirection direction)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return;
            }
            
            if (dataArray[x, y] == data && directionsArray[x, y] == direction)
            {
                return;
            }

            dataArray[x, y] = data;
            directionsArray[x, y] = direction;
            
            UpdateUV2(x, y, true);
            UpdateUV2(x - 1, y, false);
            UpdateUV2(x + 1, y, false);
            UpdateUV2(x, y - 1, false);
            UpdateUV2(x, y + 1, false);
        }

        private void UpdateUV2(int x, int y, bool primaryChangedTile)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return;
            }
            
            GroundData data = dataArray[x, y];
            if (data == null)
            {
                return;
            }

            // if tile cannot be diagonal and is neighbor of changed tile, no operations need to be done
            if (!primaryChangedTile && (!data.Diagonal || directionsArray[x, y] == RoadDirection.Center))
            {
                return;
            }
            
            int index = x * Height + y;
            int vertexIndex = index * VerticesPerRenderTile;

            RoadDirection roadDirection = directionsArray[x, y];
            Vector2Int selfCoords = new Vector2Int(x, y);
            
            bool forceSelfDataWest = roadDirection.IsCenter() || roadDirection.IsWest();
            UpdateUV2Triangle(selfCoords, new Vector2Int(x - 1, y), vertexIndex, primaryChangedTile, forceSelfDataWest);
            bool forceSelfDataNorth = roadDirection.IsCenter() || roadDirection.IsNorth();
            UpdateUV2Triangle(selfCoords, new Vector2Int(x, y + 1), vertexIndex + 3, primaryChangedTile, forceSelfDataNorth);
            bool forceSelfDataEast = roadDirection.IsCenter() || roadDirection.IsEast();
            UpdateUV2Triangle(selfCoords, new Vector2Int(x + 1, y), vertexIndex + 6, primaryChangedTile, forceSelfDataEast);
            bool forceSelfDataSouth = roadDirection.IsCenter() || roadDirection.IsSouth();
            UpdateUV2Triangle(selfCoords, new Vector2Int(x, y - 1), vertexIndex + 9, primaryChangedTile, forceSelfDataSouth);
        }

        private void UpdateUV2Triangle(Vector2Int selfCoords, Vector2Int diagonalCoords, int uvIndex, bool primaryChangedTile, bool forceSelfData)
        {
            GroundData selfData = dataArray[selfCoords.x, selfCoords.y];
            GroundData diagonalData = GetGroundData(diagonalCoords.x, diagonalCoords.y);

            // tile cannot change if using self data for triangle is forced and it's not primary tile being changed
            if (!primaryChangedTile && forceSelfData)
            {
                return;
            }
            
            if (forceSelfData || diagonalData == null)
            {
                UpdateUV2(selfData, uvIndex, selfCoords);
            }
            else
            {
                UpdateUV2(diagonalData, uvIndex, diagonalCoords);
            }
        }

        private void UpdateUV2(GroundData data, int uvIndex, Vector2Int tileCoords)
        {
            if (dataArray[tileCoords.x, tileCoords.y] != data)
            {
                return;
            }
            
            data.Tex3d.LoadOrGetTexture(loadedTexture =>
            {
                if (!loadedTexture)
                {
                    return;
                }
            
                int texIndex = groundTexturesArray.PutOrGetTexture(data.Tex3d, loadedTexture);
                Vector2 texVector = new Vector2(texIndex, 0);
                uv2[uvIndex] = texVector;
                uv2[uvIndex + 1] = texVector;
                uv2[uvIndex + 2] = texVector;
            
                needsUvUpdate = true;
            });
        }
    }
}