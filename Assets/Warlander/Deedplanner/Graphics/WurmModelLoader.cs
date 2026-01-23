using System;
using System.IO;
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

        public void LoadModel(string path, Action<GameObject> onLoaded)
        {
            LoadModel(path, Vector3.one, onLoaded);
        }

        public void LoadModel(string path, Vector3 scale, Action<GameObject> onLoaded)
        {
            Debug.Log("Loading model at " + path);

            WebUtils.ReadUrlToByteArray(path, data =>
            {
                using BinaryReader source = new BinaryReader(new MemoryStream(data));
                string fileFolder = path.Substring(0, path.LastIndexOf("/", StringComparison.Ordinal));

                GameObject modelGameObject = new GameObject(Path.GetFileNameWithoutExtension(path));

                int meshCount = source.ReadInt32();
                int loadedMeshes = 0;
                for (int i = 0; i < meshCount; i++)
                {
                    LoadMeshObject(source, fileFolder, scale, loadedMesh =>
                    {
                        if (loadedMesh)
                        {
                            loadedMesh.transform.SetParent(modelGameObject.transform);
                        }

                        loadedMeshes++;

                        if (loadedMeshes == meshCount)
                        {
                            onLoaded?.Invoke(modelGameObject);
                        }
                    });
                }

                if (meshCount == 0)
                {
                    onLoaded?.Invoke(modelGameObject);
                }
            });
        }

        private void LoadMeshObject(BinaryReader source, string fileFolder, Vector3 scale, Action<GameObject> onLoaded)
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
                _materialLoader.LoadMaterial(source, fileFolder, mat =>
                {
                    GameObject meshObject = new GameObject(meshName);

                    MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
                    MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = loadedMesh;
                    meshRenderer.sharedMaterial = mat;

                    onLoaded.Invoke(meshObject);
                });
            }
            else
            {
                Debug.Log("Discarding mesh " + meshName);
                // We need to load material metadata to advance file read to the next valid position.
                _materialLoader.LoadMaterialMetadata(source, fileFolder);
                Object.Destroy(loadedMesh);
                onLoaded.Invoke(null);
            }
        }
    }
}
