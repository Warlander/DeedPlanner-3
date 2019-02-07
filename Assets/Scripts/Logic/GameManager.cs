using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{

    public class GameManager : MonoBehaviour
    {

        public static GameManager Instance { get; private set; }

        public Map Map { get; private set; }

        [SerializeField]
        private GameObject water;

        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // we are disabling water to avoid annoying editor errors
            water.SetActive(true);

            Debug.Log("Loading data");
            DataLoader.LoadData();

            Debug.Log("Creating map");
            GameObject mapObject = new GameObject("Map", typeof(Map));
            Map = mapObject.GetComponent<Map>();
            Map.Initialize(25, 25);
        }

        private void Update()
        {

        }

    }

}