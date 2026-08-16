using System;
using UnityEngine;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic.Cameras;
using Warlander.Deedplanner.Settings;

namespace Warlander.Deedplanner.Graphics.Water
{
    public class WaterController : IDisposable
    {
        private readonly WaterObjectContainer _container;
        private readonly WaterReflectionController _reflectionController;
        private readonly DPSettings _settings;

        public WaterController(WaterObjectContainer container, WaterReflectionController reflectionController, DPSettings settings)
        {
            _container = container;
            _reflectionController = reflectionController;
            _settings = settings;
        }
        
        public void PrepareForCamera(Camera camera, ICameraController cameraController, bool renderWater)
        {
            WaterQuality quality = _settings.WaterQuality;

            if (quality == WaterQuality.Ultra || quality == WaterQuality.High)
            {
                _container.ComplexWater.SetActive(renderWater);

                if (renderWater)
                {
                    Vector2 waterPos = cameraController.CalculateWaterTablePosition(camera.transform.position);
                    Vector3 current = _container.ComplexWater.transform.position;
                    _container.ComplexWater.transform.position = new Vector3(waterPos.x, current.y, waterPos.y);
                }

                if (quality == WaterQuality.Ultra && renderWater)
                {
                    _reflectionController.RenderForCamera(camera, _container.ComplexWaterRenderer);
                }

                _container.SimpleWater.SetActive(false);
            }
            else if (quality == WaterQuality.Simple)
            {
                _container.SimpleWater.SetActive(renderWater);
                _container.ComplexWater.SetActive(false);
            }
        }

        public void Dispose()
        {
            _reflectionController.Dispose();
        }
    }
}
