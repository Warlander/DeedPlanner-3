using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    public class Model
    {

        private readonly string location;
        private readonly bool loadTextures;
        private readonly string oneIncludedMesh;
        private Dictionary<string, string> textureOverrides;

        private GameObject originalModel;
        private Dictionary<int, GameObject> skewedModels;

        public string Tag { get; private set; }
        public Vector3 Scale { get; private set; }

        public Model(XmlElement element)
        {
            Tag = element.GetAttribute("tag");
            location = element.GetAttribute("location");
            string scaleStr = element.GetAttribute("scale");
            float scaleFloat;
            if (!float.TryParse(scaleStr, out scaleFloat))
            {
                scaleFloat = 1;
            }
            Scale = new Vector3(scaleFloat, scaleFloat, scaleFloat);
            loadTextures = element.GetAttribute("loadTextures") != "false";

            textureOverrides = new Dictionary<string, string>();
            foreach (XmlElement over in element.GetElementsByTagName("override"))
            {
                string mesh = over.GetAttribute("mesh");
                string texture = over.GetAttribute("texture");
                textureOverrides[mesh] = texture;
            }

            XmlNodeList includesList = element.GetElementsByTagName("include");
            if (includesList.Count == 1)
            {
                XmlElement include = (XmlElement) includesList[0];
                oneIncludedMesh = include.GetAttribute("mesh");
            }
            else if (includesList.Count > 1)
            {
                throw new ArgumentException("Only one include per model allowed for now");
            }
            else
            {
                oneIncludedMesh = null;
            }
        }

        public Model(string location)
        {
            this.location = location;
        }

        public GameObject CreateOrGetModel(int skew = 0)
        {
            if (!originalModel)
            {
                originalModel = WomModelLoader.LoadModel(location);
                skewedModels[0] = originalModel;
            }

            if (!skewedModels.ContainsKey(skew))
            {
                skewedModels[skew] = CreateSkewedModel(skew);
            }

            return skewedModels[skew];
        }

        private GameObject CreateSkewedModel(int skew)
        {
            // skew is in Wurm units that are 4 Unity units long and 0.1 units high
            float skewPerUnit = skew * 0.1f * 0.25f;

            GameObject clone = GameObject.Instantiate(originalModel);

            MeshFilter[] filters = clone.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter filter in filters)
            {
                Mesh mesh = filter.mesh;
                Mesh newMesh = new Mesh();
                Vector3[] originalVertices = mesh.vertices;
                Vector3[] newVertices = new Vector3[originalVertices.Length];
                for (int i = 0; i < originalVertices.Length; i++)
                {
                    Vector3 vec = originalVertices[i];
                    newVertices[i] = new Vector3(vec.x, vec.y + skewPerUnit * vec.x, vec.z);
                }
                newMesh.vertices = newVertices;
                newMesh.uv = mesh.uv;
                newMesh.triangles = newMesh.triangles;
                newMesh.normals = mesh.normals;
                newMesh.tangents = mesh.tangents;
                newMesh.RecalculateBounds();
                filter.mesh = newMesh;
            }

            return clone;
        }

    }
}
