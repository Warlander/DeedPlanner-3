using System;
 using System.Collections;
 using System.Collections.Generic;
using System.IO;
 using System.Text;
 using UnityEngine;
 using UnityEngine.Networking;
 using Warlander.Deedplanner.Utils;
 using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Graphics
{
    public static class WurmAssetsLoader
    {
        private static readonly Dictionary<MaterialKey, Material> cachedMaterials = new Dictionary<MaterialKey, Material>();

        public static void LoadModel(string path, Action<GameObject> onLoaded)
        {
            LoadModel(path, Vector3.one, onLoaded);
        }

        public static void LoadModel(string path, Vector3 scale, Action<GameObject> onLoaded)
        {
            Debug.Log("Loading model at " + path);
            if (path.Contains("RopeBridge"))
            {
                Debug.Log("");
            }
            
            WebUtils.ReadUrlToByteArray(path, data =>
            {
                using BinaryReader source = new BinaryReader(new MemoryStream(data));
                string fileFolder = path.Substring(0, path.LastIndexOf("/", StringComparison.Ordinal));

                GameObject modelGameObject = new GameObject(Path.GetFileNameWithoutExtension(path));

                int meshCount = source.ReadInt32();
                int loadedMeshes = 0;
                for (int i = 0; i < meshCount; i++)
                {
                    LoadMeshObject(source, fileFolder, scale, loadedMesh =>
                    {
                        if (loadedMesh)
                        {
                            loadedMesh.transform.SetParent(modelGameObject.transform);
                        }

                        loadedMeshes++;

                        if (loadedMeshes == meshCount)
                        {
                            onLoaded.Invoke(modelGameObject);
                        }
                    });
                }

                if (meshCount == 0)
                {
                    onLoaded.Invoke(modelGameObject);
                }
            });
        }

        private static void LoadMeshObject(BinaryReader source, string fileFolder, Vector3 scale, Action<GameObject> onLoaded)
        {
            Mesh loadedMesh = LoadMesh(source, scale);
            string meshName = loadedMesh.name;
            
            string meshNameLowercase = meshName.ToLower();
            bool discardMesh = meshNameLowercase.Contains("boundingbox") || meshNameLowercase.Contains("pickingbox") ||
                               (meshNameLowercase.Contains("lod") && !meshNameLowercase.Contains("lod0"));

            int materialsCount = source.ReadInt32(); // there is always only one material per mesh, but we need to load this int anyway
            LoadMaterial(source, fileFolder, mat =>
            {
                if (!discardMesh)
                {
                    GameObject meshObject = new GameObject(meshName);

                    MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
                    MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = loadedMesh;
                    meshRenderer.sharedMaterial = mat;
            
                    onLoaded.Invoke(meshObject);
                }
            });
            
            if (discardMesh)
            {
                Object.Destroy(loadedMesh);
                onLoaded.Invoke(null);
                return;
            }
        }

        private static Mesh LoadMesh(BinaryReader source, Vector3 scale)
        {
            bool hasTangents = source.ReadBoolean();
            bool hasBinormal = source.ReadBoolean();
            bool hasVertexColor = source.ReadBoolean();
            string name = ReadString(source);
            int verticesCount = source.ReadInt32();

            List<Vector3> vertexList = new List<Vector3>();
            List<Vector3> normalList = new List<Vector3>();
            List<Vector2> uvList = new List<Vector2>();
            List<Color> colorList = new List<Color>();
            List<Vector4> tangentsList = new List<Vector4>();

            for (int i = 0; i < verticesCount; i++)
            {
                Vector3 vertex = new Vector3(source.ReadSingle(), source.ReadSingle(), source.ReadSingle());
                vertex.Scale(scale);
                vertexList.Add(vertex);
                Vector3 normal = new Vector3(source.ReadSingle(), source.ReadSingle(), source.ReadSingle());
                normal.Scale(scale);
                normalList.Add(normal);
                uvList.Add(new Vector2(source.ReadSingle(), 1 - source.ReadSingle()));

                if (hasVertexColor)
                {
                    colorList.Add(new Color(source.ReadSingle(), source.ReadSingle(), source.ReadSingle()));
                }
                if (hasTangents)
                {
                    tangentsList.Add(new Vector4(source.ReadSingle(), source.ReadSingle(), source.ReadSingle()));
                }
                if (hasBinormal)
                {
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                }
            }

            int trianglesCount = source.ReadInt32();
            int[] triangles = new int[trianglesCount];

            for (int i = 0; i < trianglesCount; ++i)
            {
                triangles[i] = source.ReadInt16();
            }

            if (scale.x * scale.y * scale.z < 0)
            {
                for (int i = 0; i < trianglesCount; i += 3)
                {
                    int temp = triangles[i];
                    triangles[i] = triangles[i + 2];
                    triangles[i + 2] = temp;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.SetVertices(vertexList);
            mesh.SetNormals(normalList);
            mesh.SetUVs(0, uvList);
            if (colorList.Count != 0)
            {
                mesh.SetColors(colorList);
            }
            if (tangentsList.Count != 0)
            {
                mesh.SetTangents(tangentsList);
            }
            mesh.SetTriangles(triangles, 0);

            return mesh;
        }

        private static void LoadMaterial(BinaryReader source, string modelFolder, Action<Material> onLoaded)
        {
            string texName = ReadString(source);
            string texLocation = Path.Combine(modelFolder, texName);
            string matName = ReadString(source);
            
            MaterialKey materialKey = new MaterialKey(matName, texLocation);

            float glossiness = 0;

            bool hasMaterialProperties = source.ReadBoolean();
            if (hasMaterialProperties)
            {
                bool hasEmissive = source.ReadBoolean();
                if (hasEmissive)
                {
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                }

                bool hasShininess = source.ReadBoolean();
                if (hasShininess)
                {
                    glossiness = source.ReadSingle() / 100;
                    if (glossiness > 1)
                    {
                        glossiness = 0;
                    }
                }

                bool hasSpecular = source.ReadBoolean();
                if (hasSpecular)
                {
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                }

                bool hasTransparencyColor = source.ReadBoolean();
                if (hasTransparencyColor)
                {
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
                }
            }
            
            if (cachedMaterials.ContainsKey(materialKey))
            {
                Debug.Log("Loading material from cache");
                Material cachedMaterial = cachedMaterials[materialKey];
                onLoaded.Invoke(cachedMaterial);
            }
            else
            {
                Material material = new Material(GraphicsManager.Instance.WomDefaultMaterial);
                material.name = matName;
                
                TextureReference textureReference = TextureReference.GetTextureReference(texLocation);

                textureReference?.LoadOrGetTexture(texture =>
                {
                    if (texture)
                    {
                        material.SetTexture(ShaderPropertyIds.MainTex, texture);
                    }
                    else
                    {
                        material.SetColor(ShaderPropertyIds.Color, new Color(1, 1, 1, 0));
                    }
                
                    material.SetFloat(ShaderPropertyIds.Glossiness, glossiness);
                
                    cachedMaterials[materialKey] = material;
                    onLoaded.Invoke(material);
                });
            }
        }

        public static void LoadTexture(string location, bool readable, Action<Texture2D> onLoaded)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(location)))
            {
                Debug.LogWarning("Attempting to load texture from empty location: " + location);
                onLoaded.Invoke(null);
                return;
            }

            Debug.Log("Loading texture at " + location);
            WebUtils.ReadUrlToByteArray(location, data =>
            {
                Texture2D texture;
                if (location.Substring(location.LastIndexOf(".", StringComparison.Ordinal) + 1) == "dds")
                {
                    texture = LoadTextureDxt(data);
                }
                else
                {
                    texture = new Texture2D(0, 0, TextureFormat.DXT1, true);
                    texture.LoadImage(data, !readable);
                }
            
                onLoaded.Invoke(texture);
            });
        }

        private static Texture2D LoadTextureDxt(byte[] ddsBytes)
        {
            byte ddsSizeCheck = ddsBytes[4];
            if (ddsSizeCheck != 124)
                throw new Exception("Invalid DDS DXTn texture. Unable to read");  //this header byte should be 124 for DDS image files

            int height = ddsBytes[13] * 256 + ddsBytes[12];
            int width = ddsBytes[17] * 256 + ddsBytes[16];
            int totalSize = width * height;
            int pitch = ddsBytes[23] * 256 * 256 * 256 + ddsBytes[22] * 256 * 256 + ddsBytes[21] * 256 + ddsBytes[20];

            const int ddsHeaderSize = 128;
            
            byte[] dxtBytes = new byte[ddsBytes.Length - ddsHeaderSize];
            Buffer.BlockCopy(ddsBytes, ddsHeaderSize, dxtBytes, 0, ddsBytes.Length - ddsHeaderSize);

            TextureFormat textureFormat;
            if (pitch == totalSize)
            {
                textureFormat = TextureFormat.DXT5;
            }
            else
            {
                textureFormat = TextureFormat.DXT1;
            }

            Texture2D dxtTexture = new Texture2D(width, height, textureFormat, false);
            dxtTexture.LoadRawTextureData(dxtBytes);

            Texture2D finalTexture = new Texture2D(dxtTexture.width, dxtTexture.height);
            Color32[] pixelBuffer = dxtTexture.GetPixels32();
            Object.Destroy(dxtTexture);

            int yScanSize = finalTexture.height / 2;
            int xScanSize = finalTexture.width;
            for (int y = 0; y < yScanSize; y++)
            {
                for (int x = 0; x < xScanSize; x++)
                {
                    int originalIndex = y * finalTexture.width + x;
                    int flippedIndex = (finalTexture.height - y - 1) * finalTexture.width + x;
                    Color32 temp = pixelBuffer[flippedIndex];
                    pixelBuffer[flippedIndex] = pixelBuffer[originalIndex];
                    pixelBuffer[originalIndex] = temp;
                }
            }
            finalTexture.SetPixels32(pixelBuffer);
            finalTexture.Apply();
            finalTexture.Compress(true);

            return finalTexture;
        }

        private static string ReadString(BinaryReader source)
        {
            int size = source.ReadInt32();
            byte[] bytes = source.ReadBytes(size);
            return Encoding.ASCII.GetString(bytes);
        }

        private struct MaterialKey
        {
            public string Name { get; }
            public string TextureLocation { get; }

            public MaterialKey(string name, string textureLocation)
            {
                Name = name;
                TextureLocation = textureLocation;
            }
        }
    }
}
