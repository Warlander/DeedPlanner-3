using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public class WurmMeshLoader : IWurmMeshLoader
    {
        public Mesh LoadMesh(BinaryReader source, Vector3 scale)
        {
            bool hasTangents = source.ReadBoolean();
            bool hasBinormal = source.ReadBoolean();
            bool hasVertexColor = source.ReadBoolean();
            string name = WurmFileUtility.ReadString(source);
            int verticesCount = source.ReadInt32();

            List<Vector3> vertexList = new List<Vector3>();
            List<Vector3> normalList = new List<Vector3>();
            List<Vector2> uvList = new List<Vector2>();
            List<Color> colorList = new List<Color>();

            for (int i = 0; i < verticesCount; i++)
            {
                Vector3 vertex = new Vector3(source.ReadSingle(), source.ReadSingle(), source.ReadSingle());
                vertex.Scale(scale);
                vertexList.Add(vertex);
                Vector3 normal = new Vector3(source.ReadSingle(), source.ReadSingle(), source.ReadSingle());
                normal = new Vector3(normal.x / scale.x, normal.y / scale.y, normal.z / scale.z).normalized;
                normalList.Add(normal);
                uvList.Add(new Vector2(source.ReadSingle(), 1 - source.ReadSingle()));

                if (hasVertexColor)
                {
                    colorList.Add(new Color(source.ReadSingle(), source.ReadSingle(), source.ReadSingle()));
                }
                if (hasTangents)
                {
                    source.ReadSingle();
                    source.ReadSingle();
                    source.ReadSingle();
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
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();

            return mesh;
        }
    }
}
