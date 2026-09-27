using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Logging;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Rendering.Assets.Tests
{
    public class ModelHandleTextureOverrideTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task ModelCallbacksWaitForTextureOverrideAsync(bool missingTexture)
        {
            Shader shader = Shader.Find("Warlander/ModelShader");
            Assert.That(shader, Is.Not.Null);

            var defaultTexture = new Texture2D(2, 2);
            Texture2D overrideTexture = missingTexture ? null : new Texture2D(2, 2);
            var defaultMaterial = new Material(shader);
            defaultMaterial.SetTexture(ShaderPropertyIds.BaseMap, defaultTexture);

            var master = new GameObject("Master model");
            var mesh = new GameObject("FloorMesh");
            mesh.transform.SetParent(master.transform);
            MeshRenderer masterRenderer = mesh.AddComponent<MeshRenderer>();
            masterRenderer.sharedMaterial = defaultMaterial;

            var loggerSource = new LoggerSource();
            var facade = new WurmAssetFacade(loggerSource);
            var textureLoader = new PendingTextureLoader();
            typeof(WurmAssetFacade).GetField("_modelLoader", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(facade, new StubModelLoader(master));
            typeof(WurmAssetFacade).GetField("_textureReferenceFactory", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(facade, new TextureReferenceFactory(textureLoader, loggerSource.Create(WurmAssetFacade.Category)));

            var document = new XmlDocument();
            document.LoadXml("<model location='Floors/Floor.wom'><override mesh='FloorMesh' texture='Floors/marble_floor.png'/></model>");
            ModelHandle handle = facade.GetModel(document.DocumentElement, 0);
            var first = new TaskCompletionSource<GameObject>();
            var queued = new TaskCompletionSource<GameObject>();
            var instances = new List<GameObject>();
            Texture firstCallbackTexture = null;
            Texture queuedCallbackTexture = null;

            try
            {
                handle.CreateOrGetModel(instance =>
                {
                    instances.Add(instance);
                    firstCallbackTexture = instance.GetComponentInChildren<MeshRenderer>().sharedMaterial.GetTexture(ShaderPropertyIds.BaseMap);
                    first.SetResult(instance);
                });
                handle.CreateOrGetModel(instance =>
                {
                    instances.Add(instance);
                    queuedCallbackTexture = instance.GetComponentInChildren<MeshRenderer>().sharedMaterial.GetTexture(ShaderPropertyIds.BaseMap);
                    queued.SetResult(instance);
                });

                Assert.That(textureLoader.Attempts, Is.EqualTo(1));
                Assert.That(first.Task.IsCompleted, Is.False);
                Assert.That(queued.Task.IsCompleted, Is.False);

                textureLoader.Complete(overrideTexture);
                Task callbacks = Task.WhenAll(first.Task, queued.Task);
                Assert.That(await Task.WhenAny(callbacks, Task.Delay(5000)), Is.SameAs(callbacks));
                GameObject firstInstance = await first.Task;
                GameObject queuedInstance = await queued.Task;
                Texture2D expected = missingTexture ? defaultTexture : overrideTexture;
                Assert.That(firstInstance, Is.Not.Null);
                Assert.That(queuedInstance, Is.Not.Null);
                Assert.That(firstCallbackTexture, Is.SameAs(expected));
                Assert.That(queuedCallbackTexture, Is.SameAs(expected));
            }
            finally
            {
                foreach (GameObject instance in instances)
                {
                    if (instance) Object.DestroyImmediate(instance);
                }

                Material appliedMaterial = masterRenderer.sharedMaterial;
                if (master) Object.DestroyImmediate(master);
                if (appliedMaterial && appliedMaterial != defaultMaterial) Object.DestroyImmediate(appliedMaterial);
                Object.DestroyImmediate(defaultMaterial);
                Object.DestroyImmediate(defaultTexture);
                if (overrideTexture) Object.DestroyImmediate(overrideTexture);

                FieldInfo rootField = typeof(WurmAssetFacade).GetField("_modelsRoot", BindingFlags.Instance | BindingFlags.NonPublic);
                var root = rootField?.GetValue(facade) as GameObject;
                if (root) Object.DestroyImmediate(root);
            }
        }

        private sealed class StubModelLoader : IWurmModelLoader
        {
            private readonly GameObject _model;

            public StubModelLoader(GameObject model)
            {
                _model = model;
            }

            public Task<GameObject> LoadModelAsync(string path)
            {
                return Task.FromResult(_model);
            }

            public Task<GameObject> LoadModelAsync(string path, Vector3 scale)
            {
                return Task.FromResult(_model);
            }
        }

        private sealed class PendingTextureLoader : ITextureLoader
        {
            private readonly TaskCompletionSource<Texture2D> _completion = new TaskCompletionSource<Texture2D>();

            public int Attempts { get; private set; }

            public Task<Texture2D> LoadTextureAsync(string location, bool readable)
            {
                Attempts++;
                return _completion.Task;
            }

            public void Complete(Texture2D texture)
            {
                _completion.SetResult(texture);
            }
        }
    }
}
