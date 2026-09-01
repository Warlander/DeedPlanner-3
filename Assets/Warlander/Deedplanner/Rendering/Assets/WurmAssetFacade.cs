using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Rendering.Assets
{
    /// <summary>
    /// Single entry point and composition root for the Wurm asset loading stack
    /// (models, meshes, materials, textures). Registered as a singleton at runtime;
    /// editor tools construct their own instance directly.
    /// </summary>
    public class WurmAssetFacade : IWurmAssetFacade
    {
        public static readonly LogCategory Category = new LogCategory("WurmAssets");

        private readonly IWurmModelLoader _modelLoader;
        private readonly ITextureReferenceFactory _textureReferenceFactory;
        private readonly ICategoryLogger _logger;
        private GameObject _modelsRoot;

        public WurmAssetFacade(ILoggerSource loggerSource)
        {
            _logger = loggerSource.Create(Category);
            ITextureLoader textureLoader = new AggregateTextureLoader(_logger);
            _textureReferenceFactory = new TextureReferenceFactory(textureLoader, _logger);
            IMaterialLoader materialLoader = new MaterialLoader(_textureReferenceFactory);
            IMaterialCache materialCache = new MaterialCache(materialLoader);
            IWurmMaterialLoader wurmMaterialLoader = new WurmMaterialLoader(materialCache);
            _modelLoader = new WurmModelLoader(new WurmMeshLoader(), wurmMaterialLoader, _logger);
        }

        internal IWurmModelLoader ModelLoader => _modelLoader;
        internal ITextureReferenceFactory TextureReferenceFactory => _textureReferenceFactory;
        internal ICategoryLogger Logger => _logger;

        internal GameObject ModelsRoot
        {
            get
            {
                if (!_modelsRoot)
                {
                    _modelsRoot = new GameObject("Models");
                    _modelsRoot.SetActive(false);
                }

                return _modelsRoot;
            }
        }

        public ModelHandle GetModel(XmlElement element, int layer = int.MaxValue)
        {
            return new ModelHandle(this, element, layer);
        }

        public ModelHandle GetModel(XmlElement element, Vector3 scale, int layer = int.MaxValue)
        {
            return new ModelHandle(this, element, scale, layer);
        }

        public ModelHandle GetModel(string location, int layer = int.MaxValue)
        {
            return new ModelHandle(this, location, layer);
        }

        public ModelHandle GetModel(string location, Vector3 scale, int layer = int.MaxValue)
        {
            return new ModelHandle(this, location, scale, layer);
        }

        public TextureReference GetTextureReference(XmlElement element)
        {
            return _textureReferenceFactory.GetTextureReference(element);
        }

        public TextureReference GetTextureReference(string location)
        {
            return _textureReferenceFactory.GetTextureReference(location);
        }
    }
}
