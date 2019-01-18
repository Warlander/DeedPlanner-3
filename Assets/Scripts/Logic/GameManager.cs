using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Unitydata;

namespace Warlander.Deedplanner.Logic
{

    public class GameManager : MonoBehaviour
    {

        public static GameManager Instance { get; private set; }

        public Map Map { get; private set; }
        public UnityMap UnityMap { get; private set; }
        private Tab currentTab;

        public Tab CurrentTab {
            get {
                return currentTab;
            }
            set {
                currentTab = value;

            }
        }

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
            Debug.Log("Loading data");
            DataLoader.LoadData();

            Debug.Log("Creating map model");
            Map = new Map(25, 25);

            Debug.Log("Creating map view");
            GameObject unityMapObject = new GameObject("Map", typeof(UnityMap));
            UnityMap = unityMapObject.GetComponent<UnityMap>();
            UnityMap.Map = Map;
        }

        private void Update()
        {

        }

        public void OnTabChange(TabReference tabReference)
        {
            Tab tab = tabReference.Tab;
            CurrentTab = tab;
        }
    }

}