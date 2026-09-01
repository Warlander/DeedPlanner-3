using UnityEngine;
using Warlander.Deedplanner.Cameras;

namespace Warlander.Deedplanner.Rendering.Water
{
    public interface IWaterFacade
    {
        void PrepareForCamera(Camera camera, ICameraController cameraController, bool renderWater);
    }
}
