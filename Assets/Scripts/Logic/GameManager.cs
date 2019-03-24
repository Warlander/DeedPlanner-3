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
        private CaveUpdater caveUpdater = null;
        [SerializeField]
        private FloorUpdater floorUpdater = null;
        [SerializeField]
        private WallUpdater wallUpdater = null;
        [SerializeField]
        private RoofUpdater roofUpdater = null;
        [SerializeField]
        private ObjectUpdater objectUpdater = null;
        [SerializeField]
        private LabelUpdater labelUpdater = null;
        [SerializeField]
        private BorderUpdater borderUpdater = null;
        [SerializeField]
        private BridgesUpdater bridgeUpdater = null;
        [SerializeField]
        private MirrorUpdater mirrorUpdater = null;

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
            CheckUpdater(caveUpdater, newUpdater);
            CheckUpdater(floorUpdater, newUpdater);
            CheckUpdater(roofUpdater, newUpdater);
            CheckUpdater(wallUpdater, newUpdater);
            CheckUpdater(objectUpdater, newUpdater);
            CheckUpdater(labelUpdater, newUpdater);
            CheckUpdater(borderUpdater, newUpdater);
            CheckUpdater(bridgeUpdater, newUpdater);
            CheckUpdater(mirrorUpdater, newUpdater);
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
                case Tab.Caves:
                    return caveUpdater;
                case Tab.Floors:
                    return floorUpdater;
                case Tab.Roofs:
                    return roofUpdater;
                case Tab.Walls:
                    return wallUpdater;
                case Tab.Objects:
                    return objectUpdater;
                case Tab.Labels:
                    return labelUpdater;
                case Tab.Borders:
                    return borderUpdater;
                case Tab.Bridges:
                    return bridgeUpdater;
                case Tab.Mirror:
                    return mirrorUpdater;
                default:
                    return null;
            }
        }

    }

}