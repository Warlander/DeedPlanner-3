using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Settings;
using Warlogic.Settings;
using Warlogic.Features;
using Warlander.Deedplanner.Platform.Features;

namespace Warlander.Deedplanner.Rendering.Assets.Tests
{
    public class SurfaceHeightRenderingTests
    {
        private const string EnabledProperty = "_SurfaceHeightDetailEnabled";
        private float _previousGlobal;
        private float _previousBridges;
        private float _previousCaves;

        [SetUp]
        public void SetUp()
        {
            _previousGlobal = Shader.GetGlobalFloat(EnabledProperty);
            _previousBridges = Shader.GetGlobalFloat(ShaderPropertyIds.BridgesPavementParallax);
            _previousCaves = Shader.GetGlobalFloat(ShaderPropertyIds.CavesPavementParallax);
        }

        [TearDown]
        public void TearDown()
        {
            Shader.SetGlobalFloat(EnabledProperty, _previousGlobal);
            Shader.SetGlobalFloat(ShaderPropertyIds.BridgesPavementParallax, _previousBridges);
            Shader.SetGlobalFloat(ShaderPropertyIds.CavesPavementParallax, _previousCaves);
        }

        [Test]
        public void MissingSettingDefaultsOnAndAppearsInGraphics()
        {
            DeedPlannerSettings settings = Create(new MemoryStore());

            Assert.That(settings.Graphics.SurfaceHeightDetail, Is.True);
            SettingsTab tab = settings.Registry.Tabs.Single(item => item.Id == "graphics");
            Assert.That(tab.Settings.Any(item => item.Key == "surfaceHeightDetail"), Is.True);
        }

        [Test]
        public void SavedOffSurvivesSettingsReload()
        {
            var store = new MemoryStore();
            DeedPlannerSettings settings = Create(store);

            settings.Graphics.SurfaceHeightDetail = false;

            Assert.That(store.TryLoad("surfaceHeightDetail", out string saved), Is.True);
            Assert.That(saved, Is.EqualTo("false"));
            DeedPlannerSettings reloaded = Create(store);
            Assert.That(reloaded.Graphics.SurfaceHeightDetail, Is.False);
            Shader.SetGlobalFloat(EnabledProperty, 1);
            using var rendering = new SurfaceHeightRendering(reloaded.Graphics, new FlagStates());
            rendering.Initialize();
            Assert.That(Shader.GetGlobalFloat(EnabledProperty), Is.Zero);
        }

        [Test]
        public void LiveChangesUpdateShader()
        {
            Shader.SetGlobalFloat(EnabledProperty, 0);
            DeedPlannerSettings settings = Create(new MemoryStore());
            using var rendering = new SurfaceHeightRendering(settings.Graphics, new FlagStates());

            rendering.Initialize();

            Assert.That(Shader.GetGlobalFloat(EnabledProperty), Is.EqualTo(1));
            settings.Graphics.SurfaceHeightDetail = false;
            Assert.That(Shader.GetGlobalFloat(EnabledProperty), Is.Zero);
            settings.Graphics.SurfaceHeightDetail = true;
            Assert.That(Shader.GetGlobalFloat(EnabledProperty), Is.EqualTo(1));
        }

        [Test]
        public void DisposeStopsFollowingSettingChanges()
        {
            DeedPlannerSettings settings = Create(new MemoryStore());
            var rendering = new SurfaceHeightRendering(settings.Graphics, new FlagStates());
            rendering.Initialize();
            rendering.Dispose();

            settings.Graphics.SurfaceHeightDetail = false;

            Assert.That(Shader.GetGlobalFloat(EnabledProperty), Is.EqualTo(1));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void PavementFlagsAreIndependentOfOutdoorHeightSetting(bool bridges, bool caves)
        {
            DeedPlannerSettings settings = Create(new MemoryStore());
            using var rendering = new SurfaceHeightRendering(settings.Graphics, new FlagStates(bridges, caves));
            rendering.Initialize();

            Assert.That(Shader.GetGlobalFloat(EnabledProperty), Is.EqualTo(1));
            Assert.That(Shader.GetGlobalFloat(ShaderPropertyIds.BridgesPavementParallax), Is.EqualTo(bridges ? 1 : 0));
            Assert.That(Shader.GetGlobalFloat(ShaderPropertyIds.CavesPavementParallax), Is.EqualTo(caves ? 1 : 0));
        }

        [TestCase(Feature.BridgesPavementParallax)]
        [TestCase(Feature.CavesPavementParallax)]
        public void AuthoredPavementFlagsAreOffEverywhere(Feature feature)
        {
            var repository = Resources.Load<DPFeatureStateRepository>("FeatureStates");
            Assert.That(repository, Is.Not.Null);
            Assert.That(repository.IsEnabledInEditor(feature), Is.False);
            Assert.That(repository.IsEnabledInDebug(feature), Is.False);
            Assert.That(repository.IsEnabledInProduction(feature), Is.False);
        }

        private sealed class FlagStates : IFeatureStateRetriever<Feature>
        {
            private readonly bool _bridges;
            private readonly bool _caves;

            public FlagStates(bool bridges = false, bool caves = false)
            {
                _bridges = bridges;
                _caves = caves;
            }

            public bool IsFeatureEnabled(Feature feature)
            {
                return feature == Feature.BridgesPavementParallax ? _bridges
                    : feature == Feature.CavesPavementParallax && _caves;
            }
        }

        private static DeedPlannerSettings Create(ISettingsStore store)
        {
            return DeedPlannerSettings.Create(new SilentLogger(), store);
        }

        private sealed class MemoryStore : ISettingsStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public bool TryLoad(string key, out string value) => _values.TryGetValue(key, out value);
            public void Save(string key, string value) => _values[key] = value;
        }

        private sealed class SilentLogger : ICategoryLogger
        {
            public void Message(string message) { }
            public void Warning(string message) { }
            public void Error(string message) { }
            public void Exception(Exception exception) { }
            public void Write(LogType type, string message) { }
        }
    }
}
