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
        private FeatureStateRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _repo = ScriptableObject.CreateInstance<FeatureStateRepository>();
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
            SetFeatureStates(_repo, new FeatureState[0]);

            // Act
            bool result = _repo.IsEnabledInProduction("Unknown");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsEnabled_UnknownFeature_LogsWarning()
        {
            // Arrange
            SetFeatureStates(_repo, new FeatureState[0]);

            // Act and assert
            LogAssert.Expect(LogType.Warning, new Regex("No feature state found for feature"));
            _repo.IsEnabledInProduction("Unknown");
        }

        [Test]
        public void IsEnabled_KnownEnabledFeature_ReturnsTrue()
        {
            // Arrange
            SetFeatureStates(_repo, new[] { MakeState("KnownFeature", production: true) });

            // Act
            bool result = _repo.IsEnabledInProduction("KnownFeature");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsEnabled_KnownDisabledFeature_ReturnsFalse()
        {
            // Arrange
            SetFeatureStates(_repo, new[] { MakeState("KnownFeature", production: false) });

            // Act
            bool result = _repo.IsEnabledInProduction("KnownFeature");

            // Assert
            Assert.IsFalse(result);
        }

        private static void SetFeatureStates(FeatureStateRepository repo, FeatureState[] states)
        {
            typeof(FeatureStateRepository)
                .GetField("featureStates", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(repo, states);
        }

        private static FeatureState MakeState(string name, bool production)
        {
            object boxed = new FeatureState(name);
            typeof(FeatureState)
                .GetField("_enabledInProduction", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(boxed, production);
            return (FeatureState)boxed;
        }
    }
}
