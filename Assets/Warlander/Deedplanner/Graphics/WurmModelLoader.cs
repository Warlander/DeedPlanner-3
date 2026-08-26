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

        public async Task<GameObject> LoadModelAsync(string path)
        {
            return await LoadModelAsync(path, Vector3.one);
        }

        public async Task<GameObject> LoadModelAsync(string path, Vector3 scale)
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
                var loadedMesh = await LoadMeshObjectAsync(source, fileFolder, scale);
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

        private async Task<GameObject> LoadMeshObjectAsync(BinaryReader source, string fileFolder, Vector3 scale)
        {
            Mesh loadedMesh = _meshLoader.LoadMesh(source, scale);
            string meshName = loadedMesh.name;
            
            string meshNameLowercase = meshName.ToLower();
            bool discardMesh = meshNameLowercase.Contains("boundingbox")
                               || meshNameLowercase.Contains("pickingbox")
                               || (meshNameLowercase.Contains("lod") && !meshNameLowercase.Contains("lod0"));
            
            int materialsCount = source.ReadInt32();
            if (materialsCount < 1)
            {
                throw new InvalidDataException("Mesh has no materials: " + meshName);
            }

            if (!discardMesh)
            {
                Debug.Log("Loading mesh " + meshName);
                var mat = await _materialLoader.LoadMaterialAsync(source, fileFolder);

                // WOM stores no triangle-to-material grouping, so multi-material meshes cannot be
                // split into submeshes. Extra materials are parsed only to keep the stream aligned;
                // the first material is used for the whole mesh (same as DeedPlanner 2).
                for (int i = 1; i < materialsCount; i++)
                {
                    _materialLoader.LoadMaterialMetadata(source, fileFolder);
                }

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
                for (int i = 0; i < materialsCount; i++)
                {
                    _materialLoader.LoadMaterialMetadata(source, fileFolder);
                }
                if (Application.isPlaying)
                {
                    Object.Destroy(loadedMesh);
                }
                else
                {
                    Object.DestroyImmediate(loadedMesh);
                }
                return null;
            }
        }
    }
}
