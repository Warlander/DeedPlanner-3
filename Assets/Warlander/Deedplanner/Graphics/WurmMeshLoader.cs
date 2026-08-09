using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace Warlander.Deedplanner.Graphics
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

            // Some source meshes (e.g. stone bridge center parts) are authored fully inside-out:
            // winding and normals both reversed. DeedPlanner 2's renderer did not backface-cull,
            // so they rendered fine there; Unity culls them. Detect via centroid test and repair.
            int triangleTotal = trianglesCount / 3;
            if (triangleTotal > 0)
            {
                Vector3 centroid = Vector3.zero;
                foreach (Vector3 vertex in vertexList)
                {
                    centroid += vertex;
                }
                centroid /= vertexList.Count;

                int inward = 0;
                for (int i = 0; i < trianglesCount; i += 3)
                {
                    Vector3 v0 = vertexList[triangles[i]];
                    Vector3 v1 = vertexList[triangles[i + 1]];
                    Vector3 v2 = vertexList[triangles[i + 2]];
                    Vector3 geometricNormal = Vector3.Cross(v1 - v0, v2 - v0);
                    Vector3 triangleCenter = (v0 + v1 + v2) / 3f;
                    if (Vector3.Dot(geometricNormal, triangleCenter - centroid) < 0)
                    {
                        inward++;
                    }
                }

                if ((float)inward / triangleTotal > 0.95f)
                {
                    Debug.LogWarning($"Mesh {name} is authored inside-out, flipping winding and normals");
                    for (int i = 0; i < trianglesCount; i += 3)
                    {
                        int temp = triangles[i];
                        triangles[i] = triangles[i + 2];
                        triangles[i + 2] = temp;
                    }
                    for (int i = 0; i < normalList.Count; i++)
                    {
                        normalList[i] = -normalList[i];
                    }
                    for (int i = 0; i < tangentsList.Count; i++)
                    {
                        tangentsList[i] = new Vector4(tangentsList[i].x, tangentsList[i].y, tangentsList[i].z, -tangentsList[i].w);
                    }
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
    }
}
