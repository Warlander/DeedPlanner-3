using UnityEngine;
using Warlander.Deedplanner.Logic.Cameras;

namespace Warlander.Deedplanner.Graphics.Water
{
    public interface IWaterFacade
    {
        void PrepareForCamera(Camera camera, ICameraController cameraController, bool renderWater);
    }
}
