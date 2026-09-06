using System;
using UnityEngine;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Ui;

namespace Warlander.Deedplanner.Rendering.Water
{
    public interface IWaterFacade
    {
        void PrepareForCamera(Camera camera, ICameraController cameraController, bool renderWater);
        IDisposable PrepareForCamera(Camera camera, ICameraController cameraController, bool renderWater,
            WaterQuality qualityOverride);
    }
}
