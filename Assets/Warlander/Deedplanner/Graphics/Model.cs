using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Graphics
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
        private readonly Dictionary<ModelProperties, GameObject> modifiedModels;
        private readonly List<ModelRequest> modelRequests = new List<ModelRequest>();
        private bool loadingOriginalModel = false;

        public string Tag { get; private set; }
        public Vector3 Scale { get; private set; }
        public int Layer { get; private set; }

        public Model(XmlElement element, int layer = int.MaxValue)
        {
            modifiedModels = new Dictionary<ModelProperties, GameObject>();

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
            textureOverrides = new Dictionary<string, string>();
            modifiedModels = new Dictionary<ModelProperties, GameObject>();
        }

        public void AddTextureOverride(string mesh, string texture)
        {
            if (modifiedModels.Count != 0)
            {
                throw new InvalidOperationException("Model is already initialized, cannot add texture override");
            }

            textureOverrides[mesh] = texture;
        }

        private IEnumerator CreateOrGetModel(ModelProperties properties, Action<GameObject> callback)
        {
            yield return InitializeModel(properties);
            if (loadingOriginalModel)
            {
                modelRequests.Add(new ModelRequest(callback, properties));
            }
            else if (originalModel)
            {
                InitializeModifiedModel(properties);
                GameObject instance = Object.Instantiate(modifiedModels[properties]);
                callback.Invoke(instance);
            }
        }

        public IEnumerator CreateOrGetModel(Material customMaterial, Action<GameObject> callback)
        {
            ModelProperties properties = new ModelProperties(0, false, customMaterial);
            yield return CreateOrGetModel(properties, callback);
        }
        
        public IEnumerator CreateOrGetModel(int skew, bool mirrorZ, Action<GameObject> callback)
        {
            ModelProperties properties = new ModelProperties(skew, mirrorZ, null);
            yield return CreateOrGetModel(properties, callback);
        }

        public IEnumerator CreateOrGetModel(int skew, Action<GameObject> callback)
        {
            ModelProperties properties = new ModelProperties(skew, false, null);
            yield return CreateOrGetModel(properties, callback);
        }

        public IEnumerator CreateOrGetModel(Action<GameObject> callback)
        {
            ModelProperties properties = new ModelProperties(0, false, null);
            yield return CreateOrGetModel(properties, callback);
        }

        private IEnumerator InitializeModel(ModelProperties modelProperties)
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
            if (!loadingOriginalModel && !originalModel)
            {
                loadingOriginalModel = true;
                string fullLocation = Application.streamingAssetsPath + "/" + location;
                yield return WurmAssetsLoader.LoadModel(fullLocation, Scale, OnMasterModelLoaded);
            }
            
            InitializeModifiedModel(modelProperties);
        }

        private void OnMasterModelLoaded(GameObject masterModel)
        {
            loadingOriginalModel = false;
            if (!masterModel)
            {
                Debug.LogError("Model failed to load!");
                return;
            }
            
            originalModel = masterModel;
            originalModel.layer = Layer;
            foreach (Transform child in originalModel.transform)
            {
                child.gameObject.layer = Layer;
                string textureOverride;
                textureOverrides.TryGetValue(child.name, out textureOverride);
                if (textureOverride == null)
                {
                    textureOverrides.TryGetValue("*", out textureOverride);
                }
                if (textureOverride != null)
                {
                    MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                    TextureReference texture = TextureReference.GetTextureReference(textureOverride);
                    Material newMaterial = new Material(renderer.sharedMaterial);
                    renderer.sharedMaterial = newMaterial;
                    
                    CoroutineManager.Instance.QueueCoroutine(texture.LoadOrGetTexture(loadedTexture => newMaterial.mainTexture = loadedTexture));
                }
            }
            originalModel.transform.SetParent(modelRoot.transform);
            ModelProperties originalProperties = new ModelProperties(0, false, null);
            modifiedModels[originalProperties] = originalModel;

            foreach (ModelRequest modelRequest in modelRequests)
            {
                InitializeModifiedModel(modelRequest.ModelProperties);
                GameObject instance = Object.Instantiate(modifiedModels[modelRequest.ModelProperties]);
                modelRequest.Callback.Invoke(instance);
            }
            modelRequests.Clear();
        }

        private void InitializeModifiedModel(ModelProperties modelProperties)
        {
            if (!originalModel || modifiedModels.ContainsKey(modelProperties))
            {
                return;
            }

            GameObject skewedModel = CreateModel(modelProperties);
            skewedModel.name = originalModel.name;
            if (modelProperties.Skew != 0)
            {
                skewedModel.name += " " + modelProperties.Skew;
            }

            if (modelProperties.MirrorZ)
            {
                skewedModel.name += " ZMirrored";
            }

            skewedModel.transform.SetParent(modelRoot.transform);
            modifiedModels[modelProperties] = skewedModel;
        }

        private GameObject CreateModel(ModelProperties modelProperties)
        {
            int skew = modelProperties.Skew;
            bool mirrorZ = modelProperties.MirrorZ;
            float mirrorZFactor = mirrorZ ? -1f : 1f;
            // skew is in Wurm units that are 4 Unity units long and 0.1 units high
            float skewPerUnit = skew * 0.1f * 0.25f;

            GameObject clone = Object.Instantiate(originalModel);

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
                    newVertices[i] = new Vector3(vec.x, vec.y + skewPerUnit * vec.x, vec.z * mirrorZFactor);
                }
                newMesh.vertices = newVertices;
                newMesh.uv = mesh.uv;
                if (mirrorZ)
                {
                    int[] oldTriangles = mesh.triangles;
                    int[] newTriangles = new int[oldTriangles.Length];
                    for (int i = 0; i < oldTriangles.Length; i += 3)
                    {
                        newTriangles[i] = oldTriangles[i + 2];
                        newTriangles[i + 1] = oldTriangles[i + 1];
                        newTriangles[i + 2] = oldTriangles[i];
                    }
                    newMesh.triangles = newTriangles;

                    Vector3[] oldNormals = mesh.normals;
                    Vector3[] newNormals = new Vector3[oldNormals.Length];
                    for (int i = 0; i < oldNormals.Length; i++)
                    {
                        Vector3 normal = oldNormals[i];
                        newNormals[i] = new Vector3(normal.x, normal.y, normal.z * mirrorZFactor);
                    }
                    newMesh.normals = newNormals;
                }
                else
                {
                    newMesh.triangles = mesh.triangles;
                    newMesh.normals = mesh.normals;
                }
                
                newMesh.tangents = mesh.tangents;
                newMesh.RecalculateBounds();
                if (!modelProperties.IsOriginalModel())
                {
                    newMesh.UploadMeshData(true);
                }
                filter.sharedMesh = newMesh;
            }

            if (modelProperties.CustomMaterial)
            {
                MeshRenderer[] renderers = clone.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer renderer in renderers)
                {
                    Material oldMaterial = renderer.sharedMaterial;
                    Material customMaterial = new Material(modelProperties.CustomMaterial);
                    if (!customMaterial.mainTexture)
                    {
                        customMaterial.mainTexture = oldMaterial.mainTexture;
                    }
                    renderer.sharedMaterial = customMaterial;
                }
            }

            return clone;
        }

        private struct ModelRequest
        {
            public readonly Action<GameObject> Callback;
            public readonly ModelProperties ModelProperties;

            public ModelRequest(Action<GameObject> callback, ModelProperties modelProperties)
            {
                Callback = callback;
                ModelProperties = modelProperties;
            }
        }
        
        private struct ModelProperties
        {
            public readonly int Skew;
            public readonly bool MirrorZ;
            public readonly Material CustomMaterial;

            public ModelProperties(int skew, bool mirrorZ, Material customMaterial)
            {
                Skew = skew;
                MirrorZ = mirrorZ;
                CustomMaterial = customMaterial;
            }
            
            public bool IsOriginalModel()
            {
                return Skew == 0 && !MirrorZ && !CustomMaterial;
            }
            
        }

    }
}
