using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    public class Model
    {

        private static GameObject modelsRoot;

        private readonly string location;
        private readonly bool loadTextures;
        private readonly string oneIncludedMesh;
        private Dictionary<string, string> textureOverrides;

        private GameObject modelRoot;
        private GameObject originalModel;
        private readonly Dictionary<int, GameObject> skewedModels;

        public string Tag { get; private set; }
        public Vector3 Scale { get; private set; }
        public int Layer { get; private set; }

        public Bounds Bounds {
            get {
                InitializeModel();
                Bounds bounds = new Bounds();
                MeshFilter[] filters = originalModel.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter filter in filters)
                {
                    Mesh mesh = filter.mesh;
                    bounds.Encapsulate(mesh.bounds);
                }
                return bounds;
            }
        }

        public Model(XmlElement element, int layer = int.MaxValue)
        {
            skewedModels = new Dictionary<int, GameObject>();

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

            this.Layer = layer;
        }

        public Model(string location, Vector3 scale, int layer = int.MaxValue) : this(location, layer)
        {
            Scale = scale;
        }

        public Model(string location, int layer = int.MaxValue)
        {
            this.location = location;
            this.Layer = layer;

            Tag = "";
            Scale = new Vector3(1, 1, 1);
        }

        public GameObject CreateOrGetModel(int skew = 0)
        {
            InitializeModel(skew);
            return GameObject.Instantiate(skewedModels[skew]);
        }

        private void InitializeModel(int skew = 0)
        {
            if (!modelsRoot)
            {
                modelsRoot = new GameObject("Models");
                modelsRoot.SetActive(false);
            }
            if (!modelRoot)
            {
                modelRoot = new GameObject(location);
                modelRoot.transform.SetParent(modelsRoot.transform);
            }
            if (!originalModel)
            {
                string fullLocation = Path.Combine(Application.streamingAssetsPath, location);
                originalModel = WomModelLoader.LoadModel(fullLocation);
                originalModel.layer = Layer;
                foreach (Transform child in originalModel.transform)
                {
                    child.gameObject.layer = Layer;
                    string textureOverride = null;
                    textureOverrides.TryGetValue(child.name, out textureOverride);
                    if (textureOverride != null)
                    {
                        MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                        TextureReference texture = TextureReference.GetTextureReference(textureOverride);
                        renderer.material = texture.Material;
                    }
                }
                originalModel.transform.SetParent(modelRoot.transform);
                skewedModels[0] = originalModel;
            }
            if (!skewedModels.ContainsKey(skew))
            {
                GameObject skewedModel = CreateSkewedModel(skew);
                skewedModel.name = originalModel.name + " " + skew;
                skewedModel.transform.SetParent(modelRoot.transform);
                skewedModels[skew] = skewedModel;
            }
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
                newMesh.name = mesh.name;
                Vector3[] originalVertices = mesh.vertices;
                Vector3[] newVertices = new Vector3[originalVertices.Length];
                for (int i = 0; i < originalVertices.Length; i++)
                {
                    Vector3 vec = originalVertices[i];
                    newVertices[i] = new Vector3(vec.x, vec.y + skewPerUnit * vec.x, vec.z);
                }
                newMesh.vertices = newVertices;
                newMesh.uv = mesh.uv;
                newMesh.triangles = mesh.triangles;
                newMesh.normals = mesh.normals;
                newMesh.tangents = mesh.tangents;
                newMesh.RecalculateBounds();
                filter.sharedMesh = newMesh;
            }

            return clone;
        }

    }
}
