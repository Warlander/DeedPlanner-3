using System;
using UnityEngine;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Ui;

namespace Warlander.Deedplanner.Rendering.Water
{
    public class WaterFacade : IWaterFacade, IDisposable
    {
        private readonly WaterObjectContainer _objectContainer;
        private readonly WaterReflectionController _reflectionController;
        private readonly WaterController _waterController;
        private readonly WaterSettingsApplier _settingsApplier;
        private readonly GraphicsOptions _graphics;
        
        public WaterFacade(GraphicsOptions graphics)
        {
            _graphics = graphics;
            var loader = new WaterObjectLoader();
            _objectContainer = new WaterObjectContainer(loader);
            _reflectionController = new WaterReflectionController();
            _waterController = new WaterController(_objectContainer, _reflectionController);
            _settingsApplier = new WaterSettingsApplier(_objectContainer, graphics);
        }
        
        public void PrepareForCamera(Camera camera, ICameraController cameraController, bool renderWater)
        {
            _waterController.PrepareForCamera(camera, cameraController, renderWater, _graphics.WaterQuality);
        }

        public IDisposable PrepareForCamera(Camera camera, ICameraController cameraController, bool renderWater,
            WaterQuality qualityOverride)
        {
            _settingsApplier.Apply(qualityOverride);
            _waterController.PrepareForCamera(camera, cameraController, renderWater, qualityOverride);
            return new QualityOverride(_settingsApplier, _graphics);
        }

        public void Dispose()
        {
            _settingsApplier.Dispose();
            _waterController.Dispose();
            _objectContainer.Dispose();
        }

        private sealed class QualityOverride : IDisposable
        {
            private readonly WaterSettingsApplier _settingsApplier;
            private readonly GraphicsOptions _graphics;

            public QualityOverride(WaterSettingsApplier settingsApplier, GraphicsOptions graphics)
            {
                _settingsApplier = settingsApplier;
                _graphics = graphics;
            }

            public void Dispose()
            {
                _settingsApplier.Apply(_graphics.WaterQuality);
            }
        }
    }
}
