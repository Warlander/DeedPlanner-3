using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    [RequireComponent(typeof(Camera))]
    public class MultiCamera : MonoBehaviour
    {

        private Camera attachedCamera;
        [SerializeField]
        private int screenId;
        [SerializeField]
        private GameObject screen;
        [SerializeField]
        private CameraMode cameraMode = CameraMode.Perspective;
        [SerializeField]
        private int floor = 0;

        private Vector3 fppPosition = new Vector3(-3, 4, -3);
        private Vector3 fppRotation = new Vector3(15, 45, 0);

        private Vector2 topPosition;
        private float topScale = 10;

        private Vector2 isometricPosition;
        private float isometricScale = 10;

        public CameraMode CameraMode {
            get {
                return cameraMode;
            }
            set {
                cameraMode = value;
                UpdateState();
            }
        }

        public int Floor {
            get {
                return floor;
            }
            set {
                floor = value;
                UpdateState();
            }
        }

        void Start()
        {
            attachedCamera = GetComponent<Camera>();

            screen.GetComponent<MouseEventCatcher>().OnDragEvent.AddListener(data =>
            {
                if (CameraMode == CameraMode.Perspective)
                {
                    fppRotation += new Vector3(-data.delta.y * Properties.FppMouseSensitivity, data.delta.x * Properties.FppMouseSensitivity, 0);
                    attachedCamera.transform.localRotation = Quaternion.Euler(fppRotation);
                }
                else if (CameraMode == CameraMode.Top)
                {
                    topPosition += new Vector2(data.delta.x * Properties.TopMouseSensitivity, data.delta.y * Properties.TopMouseSensitivity);
                    attachedCamera.transform.localPosition = new Vector3(topPosition.x, topPosition.y, -10);
                }
            });

            CameraMode = cameraMode;
        }

        void Update()
        {
            int activeWindow = LayoutManager.Instance.ActiveWindow;
            if (activeWindow != screenId)
            {
                return;
            }

            if (CameraMode == CameraMode.Perspective)
            {
                Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                movement *= Properties.FppMovementSpeed * Time.deltaTime;
                attachedCamera.transform.Translate(movement, Space.Self);
                fppPosition = attachedCamera.transform.position;
            }
        }

        private void UpdateState()
        {
            
        }

    }
}