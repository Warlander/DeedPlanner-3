using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Bridges;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Rendering.Assets.Tests
{
    public class TextureReferenceTests
    {
        private readonly HashSet<Object> _createdObjects = new HashSet<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _createdObjects)
            {
                if (created) Object.DestroyImmediate(created);
            }
            _createdObjects.Clear();
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task FailedOrEmptyLoadCanRetry(bool delayed, bool returnsNull)
        {
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new StubTextureLoader(() => completion.Task);
            var reference = new TextureReference(loader, "retry.png");
            if (!delayed) CompleteFailure(completion, returnsNull);
            Task<Texture2D> first = reference.LoadOrGetTextureAsync();
            if (delayed) CompleteFailure(completion, returnsNull);
            if (returnsNull)
            {
                Assert.That(await first, Is.Null);
            }
            else
            {
                Assert.ThrowsAsync<InvalidOperationException>(async () => await first);
            }

            Texture2D expected = CreateTexture();
            loader.Load = () => Task.FromResult(expected);

            Assert.That(await reference.LoadOrGetTextureAsync(), Is.SameAs(expected));
            Assert.That(await reference.LoadOrGetTextureAsync(), Is.SameAs(expected));
            Assert.That(loader.Attempts, Is.EqualTo(2));
        }

        [Test]
        public async Task SynchronousLoaderExceptionCanRetry()
        {
            var loader = new StubTextureLoader(() => throw new InvalidOperationException("load failed"));
            var reference = new TextureReference(loader, "retry.png");
            Assert.ThrowsAsync<InvalidOperationException>(async () => await reference.LoadOrGetTextureAsync());
            Texture2D expected = CreateTexture();
            loader.Load = () => Task.FromResult(expected);

            Assert.That(await reference.LoadOrGetTextureAsync(), Is.SameAs(expected));
            Assert.That(loader.Attempts, Is.EqualTo(2));
        }

        [Test]
        public async Task PendingTextureCallersShareOneTaskAndLoad()
        {
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new StubTextureLoader(() => completion.Task);
            var reference = new TextureReference(loader, "shared.png");
            Task<Texture2D> first = reference.LoadOrGetTextureAsync();
            Task<Texture2D> second = reference.LoadOrGetTextureAsync();
            Assert.That(second, Is.SameAs(first));
            Assert.That(loader.Attempts, Is.EqualTo(1));
            Texture2D expected = CreateTexture();
            completion.SetResult(expected);

            Assert.That(await first, Is.SameAs(expected));
            Assert.That(await second, Is.SameAs(expected));
            Assert.That(await reference.LoadOrGetTextureAsync(), Is.SameAs(expected));
            Assert.That(loader.Attempts, Is.EqualTo(1));
        }

        [Test]
        public async Task ConcurrentSpriteCallersReceiveTheSameSprite()
        {
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new StubTextureLoader(() => completion.Task);
            var reference = new TextureReference(loader, "sprite.png");
            Task<Sprite> first = reference.LoadOrGetSpriteAsync();
            Task<Sprite> second = reference.LoadOrGetSpriteAsync();
            completion.SetResult(CreateTexture());
            Sprite[] sprites = await Task.WhenAll(first, second);
            foreach (Sprite sprite in sprites) _createdObjects.Add(sprite);
            Sprite cached = await reference.LoadOrGetSpriteAsync();
            _createdObjects.Add(cached);

            Assert.That(sprites[0], Is.Not.Null);
            Assert.That(sprites[1], Is.SameAs(sprites[0]));
            Assert.That(cached, Is.SameAs(sprites[0]));
            Assert.That(loader.Attempts, Is.EqualTo(1));
        }

        [Test]
        public async Task UnclassifiedTextureKeepsSpriteDataWithoutCreatingAnEagerSprite()
        {
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new StubTextureLoader(() => completion.Task);
            var reference = new TextureReference(loader, "models/unclassified.dds");
            Texture2D expected = CreateTexture();
            Task<Texture2D> texture = reference.LoadOrGetTextureAsync();
            Assert.That(loader.CanDiscardReadableData(), Is.False);
            completion.SetResult(expected);
            Assert.That(await texture, Is.SameAs(expected));
            Assert.That(expected.name, Is.EqualTo(reference.Location));
            Assert.That(Resources.FindObjectsOfTypeAll<Sprite>().Any(sprite => sprite.texture == expected), Is.False);
            Sprite actual = await reference.LoadOrGetSpriteAsync();
            _createdObjects.Add(actual);
            Assert.That(actual.texture, Is.SameAs(expected));
            Assert.That(loader.Attempts, Is.EqualTo(1));
        }

        [Test]
        public async Task RenderingOnlyTextureCanDiscardWithoutCreatingAnEagerSprite()
        {
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new StubTextureLoader(() => completion.Task);
            var reference = new TextureReference(loader, "render.dds");
            Texture2D expected = CreateTexture();
            Task<Texture2D> pending = reference.LoadForRenderingAsync();
            Assert.That(reference.LoadOrGetTextureAsync(), Is.SameAs(pending));
            Assert.That(loader.CanDiscardReadableData(), Is.True);
            expected.Apply(false, true);
            completion.SetResult(expected);
            Assert.That(await pending, Is.SameAs(expected));
            Assert.That(expected.isReadable, Is.False);
            Assert.That(Resources.FindObjectsOfTypeAll<Sprite>().Any(sprite => sprite.texture == expected), Is.False);
            Assert.That(loader.Attempts, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task SharedSpriteUsageVetoesRenderingDiscardBeforePendingLoadCompletes(bool requestSprite)
        {
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new StubTextureLoader(() => completion.Task);
            var reference = new TextureReference(loader, "shared.dds");
            Task<Texture2D> pending = reference.LoadForRenderingAsync();
            Assert.That(loader.CanDiscardReadableData(), Is.True);
            Task<Sprite> spriteTask = null;
            if (requestSprite) spriteTask = reference.LoadOrGetSpriteAsync();
            else reference.PreserveSpriteData();
            Assert.That(loader.CanDiscardReadableData(), Is.False);
            Texture2D expected = CreateTexture();
            completion.SetResult(expected);
            await pending;
            if (spriteTask == null)
                Assert.That(Resources.FindObjectsOfTypeAll<Sprite>().Any(sprite => sprite.texture == expected), Is.False);
            Sprite actual = spriteTask == null ? await reference.LoadOrGetSpriteAsync() : await spriteTask;
            _createdObjects.Add(actual);
            Assert.That(actual.texture, Is.SameAs(expected));
            Assert.That(await reference.LoadOrGetSpriteAsync(), Is.SameAs(actual));
            Assert.That(loader.CanDiscardReadableData(), Is.False);
            Assert.That(loader.Attempts, Is.EqualTo(1));
        }

        [Test]
        public async Task AlreadyUnreadableGenericTexturePreservesLazySpriteCreation()
        {
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new StubTextureLoader(() => completion.Task);
            var reference = new TextureReference(loader, "finalized.png");
            Texture2D expected = CreateTexture();
            expected.Apply(false, true);
            Task<Texture2D> pending = reference.LoadOrGetTextureAsync();
            Assert.That(Resources.FindObjectsOfTypeAll<Sprite>().Any(sprite => sprite.texture == expected), Is.False);
            completion.SetResult(expected);
            await pending;
            Assert.That(Resources.FindObjectsOfTypeAll<Sprite>().Any(sprite => sprite.texture == expected), Is.False);

            Sprite actual = await reference.LoadOrGetSpriteAsync();
            _createdObjects.Add(actual);
            Assert.That(actual.texture, Is.SameAs(expected));
            Assert.That(loader.Attempts, Is.EqualTo(1));
        }

        [Test]
        public async Task NormalAndOcclusionRequestsOptIntoRenderingWithoutInheritingAlbedoSpriteUsage()
        {
            Texture2D normal = CreateTexture(), occlusion = CreateTexture();
            var normalLoader = new StubTextureLoader(() => Task.FromResult(normal));
            var occlusionLoader = new StubTextureLoader(() => Task.FromResult(occlusion));
            var reference = new TextureReference(null, "base.dds",
                normalReference: new TextureReference(normalLoader, "normal.dds", normalMap: true),
                occlusionReference: new TextureReference(occlusionLoader, "occlusion.dds", linearData: true));
            reference.PreserveSpriteData();

            Assert.That(await reference.LoadOrGetNormalAsync(), Is.SameAs(normal));
            Assert.That(await reference.LoadOrGetOcclusionAsync(), Is.SameAs(occlusion));
            Assert.That(normalLoader.CanDiscardReadableData(), Is.True);
            Assert.That(occlusionLoader.CanDiscardReadableData(), Is.True);
        }

        [Test]
        public async Task CustomBridgePartDdsIconPreservesItsSharedPendingRenderingReference()
        {
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new StubTextureLoader(() => completion.Task);
            var factory = new TextureReferenceFactory(loader, null);
            TextureReference rendering = factory.GetTextureReference("custom/bridge-icon.dds");
            Task<Texture2D> pending = rendering.LoadForRenderingAsync();
            Assert.That(loader.CanDiscardReadableData(), Is.True);
            var document = new XmlDocument();
            document.LoadXml("<part type='Crown'><tex location='custom/bridge-icon.dds'/></part>");
            var part = new BridgePartData(new TextureOnlyAssetFacade(factory), document.DocumentElement);

            Assert.That(part.GetUISprite(), Is.SameAs(rendering));
            Assert.That(loader.CanDiscardReadableData(), Is.False);
            Texture2D expected = CreateTexture();
            completion.SetResult(expected);
            Assert.That(await pending, Is.SameAs(expected));
            Assert.That(Resources.FindObjectsOfTypeAll<Sprite>().Any(sprite => sprite.texture == expected), Is.False);
            Sprite actual = await part.GetUISprite().LoadOrGetSpriteAsync();
            _createdObjects.Add(actual);
            Assert.That(actual.texture, Is.SameAs(expected));
            Assert.That(loader.Attempts, Is.EqualTo(1));
        }

        private Texture2D CreateTexture()
        {
            var texture = new Texture2D(2, 2);
            _createdObjects.Add(texture);
            return texture;
        }

        private static void CompleteFailure(TaskCompletionSource<Texture2D> completion, bool returnsNull)
        {
            if (returnsNull) completion.SetResult(null);
            else completion.SetException(new InvalidOperationException("load failed"));
        }

        private sealed class StubTextureLoader : ITextureLoader
        {
            public Func<Task<Texture2D>> Load { get; set; }
            public int Attempts { get; private set; }
            public Func<bool> CanDiscardReadableData { get; private set; }

            public StubTextureLoader(Func<Task<Texture2D>> load) => Load = load;

            public Task<Texture2D> LoadTextureAsync(string location, bool readable, bool normalMap = false, bool linearData = false,
                Func<bool> canDiscardReadableData = null)
            {
                Attempts++;
                CanDiscardReadableData = canDiscardReadableData;
                return Load();
            }
        }

        private sealed class TextureOnlyAssetFacade : IWurmAssetFacade
        {
            private readonly TextureReferenceFactory _factory;
            public TextureOnlyAssetFacade(TextureReferenceFactory factory) => _factory = factory;
            public void LoadTextureDefinitions(XmlDocument document) => _factory.LoadTextureDefinitions(document);
            public TextureReference GetTextureReference(XmlElement element) => _factory.GetTextureReference(element);
            public TextureReference GetTextureReference(string location) => _factory.GetTextureReference(location);
            public ModelHandle GetModel(XmlElement element, int layer = int.MaxValue) => throw new NotSupportedException();
            public ModelHandle GetModel(XmlElement element, Vector3 scale, int layer = int.MaxValue) => throw new NotSupportedException();
            public ModelHandle GetModel(string location, int layer = int.MaxValue) => throw new NotSupportedException();
            public ModelHandle GetModel(string location, Vector3 scale, int layer = int.MaxValue) => throw new NotSupportedException();
        }
    }
}
