using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic.Cameras
{
    public interface ICameraController
    {
        GridMaterialType GridMaterialToUse { get; }
        
        bool SupportsMode(CameraMode mode);
        void UpdateDrag(Camera attachedCamera, PointerEventData eventData);
        void UpdateInput(Map map, CameraMode mode, Vector3 focusedPoint, float aspect, int currentLevel, bool focusedWindow, bool mouseOver);
        void UpdateState(MultiCamera camera, Transform cameraTransform);
        Vector2 CalculateWaterTablePosition(Vector3 cameraPosition);
        float CalculateGridAlphaMultiplier();
    }
}
