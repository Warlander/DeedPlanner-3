using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
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

            public StubTextureLoader(Func<Task<Texture2D>> load) => Load = load;

            public Task<Texture2D> LoadTextureAsync(string location, bool readable, bool normalMap = false)
            {
                Attempts++;
                return Load();
            }
        }
    }
}
