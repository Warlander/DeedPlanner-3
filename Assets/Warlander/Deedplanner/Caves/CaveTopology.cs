using System.Collections.Generic;
using UnityEngine;

namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveTopology
    {
        internal readonly List<Vector3> Vertices = new List<Vector3>();
        internal readonly List<Vector3> Normals = new List<Vector3>();
        internal readonly List<Vector2> TextureCoordinates = new List<Vector2>();
        internal readonly List<Vector2> TextureIndices = new List<Vector2>();
        internal readonly List<int> Triangles = new List<int>();
        internal readonly List<CaveFace> Faces = new List<CaveFace>();

        public int TriangleCount => Faces.Count;

        public CaveFace GetFace(int triangleIndex)
        {
            return Faces[triangleIndex];
        }

        public Vector3 GetTriangleNormal(int triangleIndex)
        {
            return Normals[triangleIndex * 3];
        }

        public int GetTriangleTextureIndex(int triangleIndex)
        {
            return Mathf.RoundToInt(TextureIndices[triangleIndex * 3].x);
        }

        public Vector3 GetTriangleVertex(int triangleIndex, int vertexIndex)
        {
            return Vertices[triangleIndex * 3 + vertexIndex];
        }

        internal void ApplyTo(Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = Vertices.Count > ushort.MaxValue
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, TextureCoordinates);
            mesh.SetUVs(1, TextureIndices);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}
