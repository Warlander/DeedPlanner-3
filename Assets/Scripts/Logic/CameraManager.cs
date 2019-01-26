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
                int index = i;
                GameObject screen = screens[i];
                screen.GetComponent<MouseEventCatcher>().OnDragEvent.AddListener(data =>
                {
                    Vector3 rotation = cameras[index].transform.localRotation.eulerAngles;
                    rotation += new Vector3(-data.delta.y * MouseSensitivity, data.delta.x * MouseSensitivity, 0);
                    cameras[index].transform.localRotation = Quaternion.Euler(rotation);
                });
            }
        }

        void Update()
        {

        }
    }
}