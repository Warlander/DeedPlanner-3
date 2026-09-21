using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Domain;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Rendering.Outline.Tests
{
    public class OutlineCoordinatorTests
    {
        private OutlineCoordinator _coordinator;
        private DynamicModelBehaviour _behaviour;

        [SetUp]
        public void SetUp()
        {
            _coordinator = new OutlineCoordinator();
            _behaviour = new GameObject("Outline test").AddComponent<DynamicModelBehaviour>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_behaviour.gameObject);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AddingTwiceThenRemovingDoesNotLeaveAModelLoadedCallback(bool modelAlreadyLoaded)
        {
            if (modelAlreadyLoaded) LoadModel();
            _coordinator.AddObject(_behaviour, OutlineType.Neutral, 1);
            _coordinator.AddObject(_behaviour, OutlineType.Positive, 1);
            _coordinator.RemoveObject(_behaviour, 1);

            Assert.DoesNotThrow(() => LoadModel());
            Assert.That(_coordinator.HasOutlinedObjects, Is.False);
            Assert.That(_coordinator.GetOutlinedObjectsSnapshot(), Is.Empty);
        }

        [Test]
        public void PendingOutlineUsesLatestTypeWhenModelLoads()
        {
            _coordinator.AddObject(_behaviour, OutlineType.Neutral, 1);
            _coordinator.AddObject(_behaviour, OutlineType.Positive, 1);
            Assert.That(_coordinator.HasOutlinedObjects, Is.False);

            Renderer renderer = LoadModel();

            Assert.That(_coordinator.HasOutlinedObjects, Is.True);
            AssertOutline(OutlineType.Positive, renderer);
        }

        [Test]
        public void HigherPriorityBlocksLowerChangesButAllowsEqualPriorityChanges()
        {
            Renderer renderer = LoadModel();
            _coordinator.AddObject(_behaviour, OutlineType.Neutral, 1);
            _coordinator.AddObject(_behaviour, OutlineType.Positive, 2);
            _coordinator.AddObject(_behaviour, OutlineType.Negative, 1);
            _coordinator.RemoveObject(_behaviour, 1);
            AssertOutline(OutlineType.Positive, renderer);

            _coordinator.AddObject(_behaviour, OutlineType.Negative, 2);
            AssertOutline(OutlineType.Negative, renderer);
            _coordinator.RemoveObject(_behaviour, 2);
            Assert.That(_coordinator.HasOutlinedObjects, Is.False);
        }

        [Test]
        public void ReloadingModelReplacesOutlinedRenderers()
        {
            Renderer original = LoadModel();
            _coordinator.AddObject(_behaviour, OutlineType.Positive, 1);
            AssertOutline(OutlineType.Positive, original);

            Renderer replacement = LoadModel();

            AssertOutline(OutlineType.Positive, replacement);
        }

        private Renderer LoadModel()
        {
            var model = new GameObject("Outline test model");
            model.transform.SetParent(_behaviour.transform);
            Renderer renderer = model.AddComponent<MeshRenderer>();
            typeof(DynamicModelBehaviour).GetMethod("OnModelLoadedCallback",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_behaviour, new object[] { model });
            return renderer;
        }

        private void AssertOutline(OutlineType type, Renderer renderer)
        {
            var outlines = _coordinator.GetOutlinedObjectsSnapshot();
            Assert.That(outlines, Has.Count.EqualTo(1));
            Assert.That(outlines[0].Type, Is.EqualTo(type));
            Assert.That(outlines[0].Renderers, Is.EqualTo(new[] { renderer }));
        }
    }
}
