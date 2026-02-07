using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Utils;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Graphics
{
    public class WurmModelLoader : IWurmModelLoader
    {
        private readonly IWurmMeshLoader _meshLoader;
        private readonly IWurmMaterialLoader _materialLoader;

        public WurmModelLoader(IWurmMeshLoader meshLoader, IWurmMaterialLoader materialLoader)
        {
            _meshLoader = meshLoader;
            _materialLoader = materialLoader;
        }

        public async Task<GameObject> LoadModel(string path)
        {
            return await LoadModel(path, Vector3.one);
        }

        public async Task<GameObject> LoadModel(string path, Vector3 scale)
        {
            Debug.Log("Loading model at " + path);

            var data = await WebUtils.ReadUrlToByteArrayAsync(path);
            using BinaryReader source = new BinaryReader(new MemoryStream(data));
            string fileFolder = path.Substring(0, path.LastIndexOf("/", StringComparison.Ordinal));

            GameObject modelGameObject = new GameObject(Path.GetFileNameWithoutExtension(path));

            int meshCount = source.ReadInt32();
            int loadedMeshes = 0;
            for (int i = 0; i < meshCount; i++)
            {
                var loadedMesh = await LoadMeshObject(source, fileFolder, scale);
                if (loadedMesh)
                {
                    loadedMesh.transform.SetParent(modelGameObject.transform);
                }

                loadedMeshes++;

                if (loadedMeshes == meshCount)
                {
                    return modelGameObject;
                }
            }
            
            return modelGameObject;
        }

        private async Task<GameObject> LoadMeshObject(BinaryReader source, string fileFolder, Vector3 scale)
        {
            Mesh loadedMesh = _meshLoader.LoadMesh(source, scale);
            string meshName = loadedMesh.name;
            
            string meshNameLowercase = meshName.ToLower();
            bool discardMesh = meshNameLowercase.Contains("boundingbox")
                               || meshNameLowercase.Contains("pickingbox")
                               || (meshNameLowercase.Contains("lod") && !meshNameLowercase.Contains("lod0"));
            
            int materialsCount = source.ReadInt32(); // there is always only one material per mesh, but we need to load this int anyway
            if (materialsCount != 1)
            {
                throw new NotImplementedException("Only one material per mesh is supported");
            }
            
            if (!discardMesh)
            {
                Debug.Log("Loading mesh " + meshName);
                var mat = await _materialLoader.LoadMaterial(source, fileFolder);
                GameObject meshObject = new GameObject(meshName);

                MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
                MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = loadedMesh;
                meshRenderer.sharedMaterial = mat;

                return meshObject;
            }
            else
            {
                Debug.Log("Discarding mesh " + meshName);
                // We need to load material metadata to advance file read to the next valid position.
                _materialLoader.LoadMaterialMetadata(source, fileFolder);
                Object.Destroy(loadedMesh);
                return null;
            }
        }
    }
}
