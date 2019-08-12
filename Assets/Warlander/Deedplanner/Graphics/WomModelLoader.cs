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
                        GameObject meshObject = LoadMeshObject(source, fileFolder);
                        meshObject.transform.SetParent(modelGameObject.transform);
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

        private static GameObject LoadMeshObject(BinaryReader source, string fileFolder)
        {
            Mesh loadedMesh = LoadMesh(source);
            GameObject meshObject = new GameObject(loadedMesh.name);

            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            meshFilter.mesh = loadedMesh;

            try
            {
                int materialsCount = source.ReadInt32();
                Material loadedMaterial = LoadMaterial(source, fileFolder);
                meshRenderer.material = loadedMaterial;
            }
            catch (Exception ex)
            {
                Object.Destroy(meshObject);
                throw ex;
            }

            return meshObject;
        }

        private static Mesh LoadMesh(BinaryReader source)
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
                vertexList.Add(new Vector3(source.ReadSingle(), source.ReadSingle(), source.ReadSingle()));
                normalList.Add(new Vector3(source.ReadSingle(), source.ReadSingle(), source.ReadSingle()));
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
            Texture2D texture = LoadTexture(texLocation);
            texture.name = texName;

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
                texture = LoadTextureDXT(texBytes);
            }
            else
            {
                texture = new Texture2D(0, 0);
                texture.LoadImage(texBytes, true);
            }

            return texture;
        }

        private static Texture2D LoadTextureDXT(byte[] ddsBytes)
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
            finalTexture.SetPixels(pixelBuffer);
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
