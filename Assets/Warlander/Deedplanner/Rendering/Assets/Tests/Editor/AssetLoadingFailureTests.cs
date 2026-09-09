using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Logging;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Rendering.Assets.Tests
{
    public class AssetLoadingFailureTests
    {
        [Test]
        public async Task ModelHandle_RetriesAfterLoadFailure()
        {
            var logger = new RecordingLogger();
            var facade = new WurmAssetFacade(new StubLoggerSource(logger));
            var loader = new RetryModelLoader();
            typeof(WurmAssetFacade).GetField("_modelLoader", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(facade, loader);

            ModelHandle handle = facade.GetModel("Models/test.wom", 0);
            bool queuedCallbackCalled = false;
            GameObject instance = null;

            try
            {
                handle.CreateOrGetModel(_ => { });
                handle.CreateOrGetModel(_ => queuedCallbackCalled = true);
                loader.FailFirstLoad();
                await logger.ErrorLogged.Task;

                var completed = new TaskCompletionSource<GameObject>();
                handle.CreateOrGetModel(completed.SetResult);
                instance = await completed.Task;

                Assert.That(loader.Attempts, Is.EqualTo(2));
                Assert.That(queuedCallbackCalled, Is.False);
                Assert.That(instance, Is.Not.Null);
            }
            finally
            {
                if (instance)
                {
                    Object.DestroyImmediate(instance);
                }

                FieldInfo rootField = typeof(WurmAssetFacade).GetField("_modelsRoot", BindingFlags.Instance | BindingFlags.NonPublic);
                var root = rootField?.GetValue(facade) as GameObject;
                if (root)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public async Task MaterialCache_RetriesAfterLoadFailure()
        {
            var loader = new RetryMaterialLoader();
            var cache = new MaterialCache(loader);
            var metadata = new MaterialMetadata
            {
                MaterialName = "test",
                TextureLocation = "test.png"
            };
            Material material = null;

            try
            {
                Assert.ThrowsAsync<InvalidOperationException>(async () => await cache.GetOrCreateMaterialAsync(metadata));
                material = await cache.GetOrCreateMaterialAsync(metadata);

                Assert.That(loader.Attempts, Is.EqualTo(2));
                Assert.That(material, Is.Not.Null);
            }
            finally
            {
                if (material)
                {
                    Object.DestroyImmediate(material);
                }
            }
        }

        private class RetryModelLoader : IWurmModelLoader
        {
            private readonly TaskCompletionSource<GameObject> _firstLoad = new TaskCompletionSource<GameObject>();

            public int Attempts { get; private set; }

            public Task<GameObject> LoadModelAsync(string path)
            {
                return LoadModelAsync(path, Vector3.one);
            }

            public Task<GameObject> LoadModelAsync(string path, Vector3 scale)
            {
                Attempts++;
                if (Attempts == 1)
                {
                    return _firstLoad.Task;
                }

                return Task.FromResult(new GameObject("Master model"));
            }

            public void FailFirstLoad()
            {
                _firstLoad.SetException(new InvalidOperationException("load failed"));
            }
        }

        private class RetryMaterialLoader : IMaterialLoader
        {
            public int Attempts { get; private set; }

            public Task<Material> CreateMaterialAsync(MaterialMetadata materialMetadata)
            {
                Attempts++;
                if (Attempts == 1)
                {
                    return Task.FromException<Material>(new InvalidOperationException("load failed"));
                }

                return Task.FromResult(new Material(Shader.Find("Standard")));
            }
        }

        private class StubLoggerSource : ILoggerSource
        {
            private readonly ICategoryLogger _logger;

            public StubLoggerSource(ICategoryLogger logger)
            {
                _logger = logger;
            }

            public ICategoryLogger Create(LogCategory category)
            {
                return _logger;
            }
        }

        private class RecordingLogger : ICategoryLogger
        {
            public TaskCompletionSource<bool> ErrorLogged { get; } = new TaskCompletionSource<bool>();

            public void Message(string message) { }
            public void Warning(string message) { }
            public void Error(string message) { ErrorLogged.TrySetResult(true); }
            public void Exception(Exception exception) { }
            public void Write(LogType type, string message) { }
        }
    }
}
