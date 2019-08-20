﻿using System;
using System.Collections.Generic;
using System.IO;
 using System.Text;
 using UnityEngine;
 using Warlander.Deedplanner.Utils;
 using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Graphics
{

    public static class WomModelLoader
    {
        private static readonly int Mode = Shader.PropertyToID("_Mode");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int Color = Shader.PropertyToID("_Color");
        private static readonly int Glossiness = Shader.PropertyToID("_Glossiness");

        public static GameObject LoadModel(string path)
        {
            return LoadModel(path, Vector3.one);
        }
        
        public static GameObject LoadModel(string path, Vector3 scale)
        {
            Debug.Log("Loading model at " + path);
            byte[] requestData = WebUtils.ReadUrlToByteArray(path);
            
            using (MemoryStream memoryStream = new MemoryStream(requestData))
            {
                BinaryReader source = new BinaryReader(memoryStream);
                
                string fileFolder = path.Substring(0, path.LastIndexOf("/", StringComparison.Ordinal));

                GameObject modelGameObject = new GameObject(Path.GetFileNameWithoutExtension(path));

                try
                {
                    int meshCount = source.ReadInt32();
                    for (int i = 0; i < meshCount; i++)
                    {
                        GameObject meshObject = LoadMeshObject(source, fileFolder, scale);
                        if (meshObject)
                        {
                            meshObject.transform.SetParent(modelGameObject.transform);
                        }
                    }

                    return modelGameObject;
                }
                catch (Exception ex)
                {
                    Object.Destroy(modelGameObject);
                    throw ex;
                }
                finally
                {
                    source.Close();
                }
            }
        }

        private static GameObject LoadMeshObject(BinaryReader source, string fileFolder, Vector3 scale)
        {
            Mesh loadedMesh = LoadMesh(source, scale);
            string meshName = loadedMesh.name;
            
            Material loadedMaterial;
            try
            {
                int materialsCount = source.ReadInt32();
                loadedMaterial = LoadMaterial(source, fileFolder);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            string meshNameLowercase = meshName.ToLower();
            if (meshNameLowercase.Contains("boundingbox") || meshNameLowercase.Contains("pickingbox") || (meshNameLowercase.Contains("lod") && !meshNameLowercase.Contains("lod0")))
            {
                Object.Destroy(loadedMesh);
                Object.Destroy(loadedMaterial);
                return null;
            }

            GameObject meshObject = new GameObject(meshName);

            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            meshFilter.mesh = loadedMesh;
            meshRenderer.material = loadedMaterial;
            
            return meshObject;
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

        private static Material LoadMaterial(BinaryReader source, string modelFolder)
        {
            string texName = ReadString(source);
            string matName = ReadString(source);

            Material material = new Material(GraphicsManager.Instance.WomDefaultMaterial);
            material.name = matName;

            string texLocation = Path.Combine(modelFolder, texName);
            TextureReference textureReference = TextureReference.GetTextureReference(texLocation);
            Texture2D texture = textureReference.Texture;

            if (texture)
            {
                material.SetTexture(MainTex, texture);
            }
            else
            {
                material.SetColor(Color, new Color(1, 1, 1, 0));
            }

            
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
                    material.SetFloat(Glossiness, source.ReadSingle() / 100);
                    
                }
                else
                {
                    material.SetFloat(Glossiness, 0);
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
            
            return material;
        }

        public static Texture2D LoadTexture(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                Debug.LogWarning("Attempting to load texture from empty location");
                return null;
            }

            Debug.Log("Loading texture at " + location);
            byte[] texBytes = WebUtils.ReadUrlToByteArray(location);

            Texture2D texture;
            if (location.Substring(location.LastIndexOf(".", StringComparison.Ordinal) + 1) == "dds")
            {
                texture = LoadTextureDxt(texBytes);
            }
            else
            {
                texture = new Texture2D(0, 0, TextureFormat.DXT1, true);
                texture.LoadImage(texBytes, true);
            }

            return texture;
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

            int DDS_HEADER_SIZE = 128;
            byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
            Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

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
            dxtTexture.Apply();

            Texture2D finalTexture = new Texture2D(dxtTexture.width, dxtTexture.height);
            Color[] pixelBuffer = dxtTexture.GetPixels(0, 0, dxtTexture.width, dxtTexture.height);
            Color[] flippedPixelBuffer = new Color[pixelBuffer.Length];
            for (int y = 0; y < dxtTexture.height; y++)
            {
                for (int x = 0; x < dxtTexture.width; x++)
                {
                    int originalIndex = y * dxtTexture.width + x;
                    int flippedIndex = (dxtTexture.height - y - 1) * dxtTexture.width + x;
                    flippedPixelBuffer[flippedIndex] = pixelBuffer[originalIndex];
                }
            }
            finalTexture.SetPixels(flippedPixelBuffer);
            finalTexture.Apply();
            
            Object.Destroy(dxtTexture);

            return finalTexture;
        }

        private static string ReadString(BinaryReader source)
        {
            int size = source.ReadInt32();
            byte[] bytes = source.ReadBytes(size);
            return Encoding.ASCII.GetString(bytes);
        }

    }

}
