using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    public class CameraManager : MonoBehaviour
    {

        public float MouseSensitivity = 0.5f;

        [SerializeField]
        private Camera[] cameras = new Camera[4];
        [SerializeField]
        private GameObject[] screens = new GameObject[4];

        void Start()
        {
            for (int i = 0; i < 4; i++)
            {
                GameObject screen = screens[i];
                screen.GetComponent<MouseEventCatcher>().OnDragEvent.AddListener(data =>
                {
                    cameras[0].transform.Rotate(data.delta.y * MouseSensitivity, data.delta.x * MouseSensitivity, 0, Space.World);
                });
            }
        }

        void Update()
        {

        }
    }
}