using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Graphics
{
    public class Model
    {
        private readonly IWurmModelLoader _wurmModelLoader;
        private readonly ITextureReferenceFactory _textureReferenceFactory;

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
        /// <summary>
        /// Can be null if model isn't loaded yet. After any variation of the model is loaded, it will be always non-null.
        /// </summary>
        public GameObject OriginalModel => originalModel;

        public Model(IWurmModelLoader wurmModelLoader, ITextureReferenceFactory textureReferenceFactory, XmlElement element, Vector3 scale, int layer = int.MaxValue)
            : this(wurmModelLoader, textureReferenceFactory, element, layer)
        {
            Scale = scale;
        }
        
        public Model(IWurmModelLoader wurmModelLoader, ITextureReferenceFactory textureReferenceFactory, XmlElement element, int layer = int.MaxValue)
        {
            _wurmModelLoader = wurmModelLoader;
            _textureReferenceFactory = textureReferenceFactory;
            modifiedModels = new Dictionary<ModelProperties, GameObject>();

            Tag = element.GetAttribute("tag");
            location = element.GetAttribute("location");
            string scaleStr = element.GetAttribute("scale");
            float scaleFloat;
            if (!float.TryParse(scaleStr, out scaleFloat))
            {
                scaleFloat = 1;
            }
            Scale = new Vector3(-scaleFloat, scaleFloat, scaleFloat);
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

            Layer = layer;
        }

        public Model(IWurmModelLoader wurmModelLoader, ITextureReferenceFactory textureReferenceFactory, string location, Vector3 scale, int layer = int.MaxValue)
            : this(wurmModelLoader, textureReferenceFactory, location, layer)
        {
            Scale = scale;
        }

        public Model(IWurmModelLoader wurmModelLoader, ITextureReferenceFactory textureReferenceFactory, string newLocation, int layer = int.MaxValue)
        {
            _wurmModelLoader = wurmModelLoader;
            _textureReferenceFactory = textureReferenceFactory;
            location = newLocation;
            Layer = layer;

            Tag = "";
            Scale = new Vector3(-1, 1, 1);
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

        private void CreateOrGetModel(ModelProperties properties, Action<GameObject> callback)
        {
            InitializeModel(properties, () =>
            {
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
            });
        }

        public void CreateOrGetModel(Material customMaterial, Action<GameObject> callback)
        {
            ModelProperties properties = new ModelProperties(Vector2.zero, customMaterial);
            CreateOrGetModel(properties, callback);
        }

        public void CreateOrGetModel(Vector2 skew, Action<GameObject> callback)
        {
            ModelProperties properties = new ModelProperties(skew, null);
            CreateOrGetModel(properties, callback);
        }
        
        public void CreateOrGetModel(int skew, Action<GameObject> callback)
        {
            ModelProperties properties = new ModelProperties(new Vector2(skew, 0), null);
            CreateOrGetModel(properties, callback);
        }

        public void CreateOrGetModel(Action<GameObject> callback)
        {
            ModelProperties properties = new ModelProperties(Vector2.zero, null);
            CreateOrGetModel(properties, callback);
        }

        private void InitializeModel(ModelProperties modelProperties, Action onDone)
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
                _wurmModelLoader.LoadModel(fullLocation, Scale, model =>
                {
                    OnMasterModelLoaded(model);
                    InitializeModifiedModel(modelProperties);
                    onDone();
                });
            }
            else
            {
                InitializeModifiedModel(modelProperties);
                onDone();
            }
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
                    TextureReference texture = _textureReferenceFactory.GetTextureReference(textureOverride);
                    Material newMaterial = new Material(renderer.sharedMaterial);
                    renderer.sharedMaterial = newMaterial;

                    texture.LoadOrGetTexture(loadedTexture => newMaterial.mainTexture = loadedTexture);
                }
            }
            originalModel.transform.SetParent(modelRoot.transform);
            ModelProperties originalProperties = new ModelProperties(Vector2.zero, null);
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
            if (modelProperties.Skew != Vector2.zero)
            {
                skewedModel.name += " " + modelProperties.Skew;
            }

            skewedModel.transform.SetParent(modelRoot.transform);
            modifiedModels[modelProperties] = skewedModel;
        }

        private GameObject CreateModel(ModelProperties modelProperties)
        {
            Vector2 skew = modelProperties.Skew;
            // skew is in Wurm units that are 4 Unity units long and 0.1 units high
            float skewXPerUnit = skew.x * 0.1f * 0.25f;
            float skewZPerUnit = skew.y * 0.1f * 0.25f;
            
            GameObject clone = Object.Instantiate(originalModel);

            MeshFilter[] filters = clone.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter filter in filters)
            {
                Mesh mesh = filter.mesh;
                Mesh newMesh = new Mesh();
                newMesh.name = mesh.name;
                Vector3[] originalVertices = mesh.vertices;
                Vector3[] newVertices = new Vector3[originalVertices.Length];
                float reduction = (skew.x - skew.y) * .1f;
                for (int i = 0; i < originalVertices.Length; i++)
                {
                    Vector3 vec = originalVertices[i];
                    newVertices[i] = new Vector3(vec.x, vec.y - reduction + (skewXPerUnit * vec.x + skewZPerUnit * vec.z), vec.z);
                }
                newMesh.vertices = newVertices;
                newMesh.uv = mesh.uv;
                newMesh.triangles = mesh.triangles;
                newMesh.normals = mesh.normals;
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
            public readonly Vector2 Skew;
            public readonly Material CustomMaterial;

            public ModelProperties(Vector2 skew, Material customMaterial)
            {
                Skew = skew;
                CustomMaterial = customMaterial;
            }

            public bool IsOriginalModel()
            {
                return Skew == Vector2.zero && !CustomMaterial;
            }
        }
    }
}
