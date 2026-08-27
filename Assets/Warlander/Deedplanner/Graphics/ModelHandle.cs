using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Graphics
{
    /// <summary>
    /// Descriptor of a single Wurm model (location, scale, layer, texture overrides)
    /// plus its loaded runtime state. Created exclusively by <see cref="WurmAssetFacade"/>;
    /// instancing requests go through CreateOrGetModel.
    /// </summary>
    public class ModelHandle
    {
        private readonly WurmAssetFacade _facade;
        private readonly string _location;
        private readonly Dictionary<string, string> _textureOverrides = new Dictionary<string, string>();

        private GameObject _modelRoot;
        private GameObject _originalModel;
        private readonly Dictionary<ModelProperties, GameObject> _modifiedModels = new Dictionary<ModelProperties, GameObject>();
        private readonly List<ModelRequest> _modelRequests = new List<ModelRequest>();
        private bool _loadingOriginalModel = false;

        public string Tag { get; private set; }
        public Vector3 Scale { get; private set; }
        public int Layer { get; }
        /// <summary>
        /// Can be null if model isn't loaded yet. After any variation of the model is loaded, it will be always non-null.
        /// </summary>
        public GameObject OriginalModel => _originalModel;

        internal ModelHandle(WurmAssetFacade facade, XmlElement element, Vector3 scale, int layer)
            : this(facade, element, layer)
        {
            Scale = scale;
        }

        internal ModelHandle(WurmAssetFacade facade, XmlElement element, int layer)
        {
            _facade = facade;
            Layer = layer;

            Tag = element.GetAttribute("tag");
            _location = element.GetAttribute("location");
            string scaleStr = element.GetAttribute("scale");
            float scaleFloat;
            if (!float.TryParse(scaleStr, out scaleFloat))
            {
                scaleFloat = 1;
            }
            Scale = new Vector3(-scaleFloat, scaleFloat, scaleFloat);

            foreach (XmlElement over in element.GetElementsByTagName("override"))
            {
                string mesh = over.GetAttribute("mesh");
                string texture = over.GetAttribute("texture");
                _textureOverrides[mesh] = texture;
            }

            if (element.GetElementsByTagName("include").Count > 1)
            {
                throw new ArgumentException("Only one include per model allowed for now");
            }
        }

        internal ModelHandle(WurmAssetFacade facade, string location, Vector3 scale, int layer)
            : this(facade, location, layer)
        {
            Scale = scale;
        }

        internal ModelHandle(WurmAssetFacade facade, string location, int layer)
        {
            _facade = facade;
            _location = location;
            Layer = layer;

            Tag = "";
            Scale = new Vector3(-1, 1, 1);
        }

        public void AddTextureOverride(string mesh, string texture)
        {
            if (_modifiedModels.Count != 0)
            {
                throw new InvalidOperationException("Model is already initialized, cannot add texture override");
            }

            _textureOverrides[mesh] = texture;
        }

        private void CreateOrGetModel(ModelProperties properties, Action<GameObject> callback)
        {
            InitializeModel(() =>
            {
                if (_loadingOriginalModel)
                {
                    _modelRequests.Add(new ModelRequest(callback, properties));
                }
                else if (_originalModel)
                {
                    CreateModelInstance(properties, callback);
                }
            });
        }

        private void CreateModelInstance(ModelProperties properties, Action<GameObject> callback)
        {
            if (properties.CustomMaterial)
            {
                InitializeModifiedModel(properties);
                callback.Invoke(Object.Instantiate(_modifiedModels[properties]));
            }
            else
            {
                GameObject instance = Object.Instantiate(_originalModel);
                ApplySkewToInstance(instance, properties.Skew);
                callback.Invoke(instance);
            }
        }

        private void ApplySkewToInstance(GameObject instance, Vector2 skew)
        {
            float skewXPerUnit = skew.x * 0.1f * 0.25f;
            float skewZPerUnit = skew.y * 0.1f * 0.25f;
            float reduction = (skew.x - skew.y) * 0.1f;

            Vector3 pos = instance.transform.localPosition;
            instance.transform.localPosition = new Vector3(pos.x, pos.y - reduction, pos.z);

            if (skew == Vector2.zero)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            block.SetVector(ShaderPropertyIds.ShearY, new Vector4(skewXPerUnit, skewZPerUnit, 0, 0));
            foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.SetPropertyBlock(block);

                // The shader shifts each vertex's Y by (x * skewXPerUnit + z * skewZPerUnit).
                // Unity's frustum culling uses the original localBounds, which no longer encloses
                // the skewed geometry, causing models to disappear prematurely. Recompute the
                // bounds to cover the actual rendered positions.
                Bounds b = renderer.localBounds;
                float centerYShift = b.center.x * skewXPerUnit + b.center.z * skewZPerUnit;
                float extraYExtent = Mathf.Abs(skewXPerUnit) * b.extents.x
                                   + Mathf.Abs(skewZPerUnit) * b.extents.z;
                renderer.localBounds = new Bounds(
                    new Vector3(b.center.x, b.center.y + centerYShift, b.center.z),
                    new Vector3(b.size.x, b.size.y + 2f * extraYExtent, b.size.z)
                );
            }
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

        private void InitializeModel(Action onDone)
        {
            if (!_modelRoot)
            {
                _modelRoot = new GameObject(_location);
                _modelRoot.transform.SetParent(_facade.ModelsRoot.transform);
            }
            if (!_loadingOriginalModel && !_originalModel)
            {
                _loadingOriginalModel = true;
                string fullLocation = Application.streamingAssetsPath + "/" + _location;
                LoadMasterModelAsync(fullLocation, onDone);
            }
            else
            {
                onDone();
            }
        }

        private async void LoadMasterModelAsync(string fullLocation, Action onDone)
        {
            GameObject model = await _facade.ModelLoader.LoadModelAsync(fullLocation, Scale);
            OnMasterModelLoaded(model);
            onDone();
        }

        private void OnMasterModelLoaded(GameObject masterModel)
        {
            _loadingOriginalModel = false;
            if (!masterModel)
            {
                _facade.Logger.Error("Model failed to load: " + _location);
                return;
            }

            _originalModel = masterModel;
            _originalModel.layer = Layer;

            foreach (Transform child in _originalModel.transform)
            {
                child.gameObject.layer = Layer;
                string textureOverride;
                _textureOverrides.TryGetValue(child.name, out textureOverride);
                if (textureOverride == null)
                {
                    _textureOverrides.TryGetValue("*", out textureOverride);
                }
                if (textureOverride != null)
                {
                    MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                    TextureReference texture = _facade.TextureReferenceFactory.GetTextureReference(textureOverride);
                    Material newMaterial = new Material(renderer.sharedMaterial);
                    renderer.sharedMaterial = newMaterial;

                    ApplyTextureOverrideAsync(texture, newMaterial);
                }
            }
            _originalModel.transform.SetParent(_modelRoot.transform);
            ModelProperties originalProperties = new ModelProperties(Vector2.zero, null);
            _modifiedModels[originalProperties] = _originalModel;

            foreach (ModelRequest modelRequest in _modelRequests)
            {
                CreateModelInstance(modelRequest.ModelProperties, modelRequest.Callback);
            }
            _modelRequests.Clear();
        }

        private static async void ApplyTextureOverrideAsync(TextureReference texture, Material material)
        {
            Texture2D loadedTexture = await texture.LoadOrGetTextureAsync();
            material.SetTexture(ShaderPropertyIds.BaseMap, loadedTexture);
        }

        private void InitializeModifiedModel(ModelProperties modelProperties)
        {
            if (!_originalModel || _modifiedModels.ContainsKey(modelProperties))
            {
                return;
            }

            GameObject skewedModel = CreateModel(modelProperties);
            skewedModel.name = _originalModel.name;
            if (modelProperties.Skew != Vector2.zero)
            {
                skewedModel.name += " " + modelProperties.Skew;
            }

            skewedModel.transform.SetParent(_modelRoot.transform);
            _modifiedModels[modelProperties] = skewedModel;
        }

        private GameObject CreateModel(ModelProperties modelProperties)
        {
            GameObject clone = Object.Instantiate(_originalModel);

            if (modelProperties.CustomMaterial)
            {
                MeshRenderer[] renderers = clone.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer renderer in renderers)
                {
                    Material oldMaterial = renderer.sharedMaterial;
                    Material customMaterial = new Material(modelProperties.CustomMaterial);
                    if (!customMaterial.GetTexture(ShaderPropertyIds.BaseMap))
                    {
                        customMaterial.SetTexture(ShaderPropertyIds.BaseMap, oldMaterial.GetTexture(ShaderPropertyIds.BaseMap));
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
