using System;
using UnityEngine;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Settings;
using VContainer;
using VContainer.Unity;

namespace Warlander.Deedplanner.Rendering.Water
{
    public class WaterFacade : IWaterFacade, IDisposable
    {
        private readonly WaterObjectContainer _objectContainer;
        private readonly WaterReflectionController _reflectionController;
        private readonly WaterController _waterController;
        private readonly WaterSettingsApplier _settingsApplier;
        
        public WaterFacade(GraphicsOptions graphics)
        {
            var loader = new WaterObjectLoader();
            _objectContainer = new WaterObjectContainer(loader);
            _reflectionController = new WaterReflectionController();
            _waterController = new WaterController(_objectContainer, _reflectionController, graphics);
            _settingsApplier = new WaterSettingsApplier(_objectContainer, graphics);
        }
        
        public void PrepareForCamera(Camera camera, ICameraController cameraController, bool renderWater)
        {
            _waterController.PrepareForCamera(camera, cameraController, renderWater);
        }

        public void Dispose()
        {
            _settingsApplier.Dispose();
            _waterController.Dispose();
            _objectContainer.Dispose();
        }
    }
}
