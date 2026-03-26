using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Warlogic.Features;

namespace Warlogic.Featurestates.Editor.Tests
{
    class FeatureStateRepositoryTests
    {
        private enum TestFeature
        {
            Known = 1,
            Other = 2
        }

        private class TestRepository : FeatureStateRepository<TestFeature> { }

        private TestRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _repo = ScriptableObject.CreateInstance<TestRepository>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_repo);
        }

        [Test]
        public void IsEnabled_UnknownFeature_ReturnsFalse()
        {
            // Arrange
            SetFeatureStates(_repo, new FeatureStateEntry<TestFeature>[0]);

            // Act
            bool result = _repo.IsEnabledInProduction(TestFeature.Other);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsEnabled_UnknownFeature_LogsWarning()
        {
            // Arrange
            SetFeatureStates(_repo, new FeatureStateEntry<TestFeature>[0]);

            // Act and assert
            LogAssert.Expect(LogType.Warning, new Regex("No feature state found for feature"));
            _repo.IsEnabledInProduction(TestFeature.Other);
        }

        [Test]
        public void IsEnabled_KnownEnabledFeature_ReturnsTrue()
        {
            // Arrange
            SetFeatureStates(_repo, new[] { MakeState(TestFeature.Known, production: true) });

            // Act
            bool result = _repo.IsEnabledInProduction(TestFeature.Known);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsEnabled_KnownDisabledFeature_ReturnsFalse()
        {
            // Arrange
            SetFeatureStates(_repo, new[] { MakeState(TestFeature.Known, production: false) });

            // Act
            bool result = _repo.IsEnabledInProduction(TestFeature.Known);

            // Assert
            Assert.IsFalse(result);
        }

        private static void SetFeatureStates(TestRepository repo, FeatureStateEntry<TestFeature>[] states)
        {
            typeof(FeatureStateRepository<TestFeature>)
                .GetField("_featureStates", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(repo, states);
        }

        private static FeatureStateEntry<TestFeature> MakeState(TestFeature feature, bool production)
        {
            object boxed = new FeatureStateEntry<TestFeature>(feature);
            typeof(FeatureStateEntry<TestFeature>)
                .GetField("_enabledInProduction", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(boxed, production);
            return (FeatureStateEntry<TestFeature>)boxed;
        }
    }
}
