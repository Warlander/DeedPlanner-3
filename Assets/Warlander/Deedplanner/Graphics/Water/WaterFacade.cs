using System;
using UnityEngine;
using Warlander.Deedplanner.Logic.Cameras;
using Warlander.Deedplanner.Settings;
using Zenject;

namespace Warlander.Deedplanner.Graphics.Water
{
    public class WaterFacade : IWaterFacade, IDisposable
    {
        private readonly WaterObjectContainer _objectContainer;
        private readonly WaterReflectionController _reflectionController;
        private readonly WaterController _waterController;
        private readonly WaterSettingsApplier _settingsApplier;
        
        public WaterFacade(DPSettings settings)
        {
            var loader = new WaterObjectLoader();
            _objectContainer = new WaterObjectContainer(loader);
            _reflectionController = new WaterReflectionController();
            _waterController = new WaterController(_objectContainer, _reflectionController, settings);
            _settingsApplier = new WaterSettingsApplier(_objectContainer, settings);
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
