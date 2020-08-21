using UnityEngine;
using UnityEngine.EventSystems;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic.Cameras
{
    public interface ICameraController
    {
        bool SupportsMode(CameraMode mode);
        void UpdateDrag(PointerEventData eventData);
        void UpdateInput(Map map, CameraMode mode, Vector3 focusedPoint, float aspect, int currentFloor, bool focusedWindow, bool mouseOver);
        void UpdateState(Camera camera, Transform cameraTransform, Transform cameraParentTransform);
        Vector2 CalculateWaterTablePosition(Vector3 cameraPosition);
    }
}