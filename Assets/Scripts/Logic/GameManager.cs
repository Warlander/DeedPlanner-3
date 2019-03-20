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
        private GroundUpdater groundUpdater = null;
        [SerializeField]
        private FloorUpdater floorUpdater = null;
        [SerializeField]
        private WallUpdater wallUpdater = null;

        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            Debug.Log("Loading data");
            DataLoader.LoadData();

            Debug.Log("Creating map");
            GameObject mapObject = new GameObject("Map", typeof(Map));
            Map = mapObject.GetComponent<Map>();
            Map.Initialize(25, 25);
        }

        private void Start()
        {
            groundUpdater.gameObject.SetActive(true);
            LayoutManager.Instance.TabChanged += OnTabChange;
        }

        private void OnTabChange(Tab tab)
        {
            MonoBehaviour newUpdater = GetUpdaterForTab(tab);

            CheckUpdater(groundUpdater, newUpdater);
            CheckUpdater(floorUpdater, newUpdater);
            CheckUpdater(wallUpdater, newUpdater);
        }

        private void CheckUpdater(MonoBehaviour updater, MonoBehaviour check)
        {
            updater.gameObject.SetActive(updater == check);
        }

        private MonoBehaviour GetUpdaterForTab(Tab tab)
        {
            switch (tab)
            {
                case Tab.Ground:
                    return groundUpdater;
                case Tab.Floors:
                    return floorUpdater;
                case Tab.Walls:
                    return wallUpdater;
                default:
                    return null;
            }
        }

    }

}