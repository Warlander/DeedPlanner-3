using System;
using System.IO;
using UnityEngine;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    public class DebugManager : MonoBehaviour
    {

        public static DebugManager Instance { get; private set; }
        
        [SerializeField] private bool loadTestMap = false;

        [SerializeField] private bool overrideStartingTileSelectionMode = false;
        [SerializeField] private TileSelectionMode tileSelectionMode = TileSelectionMode.Nothing;

        public bool LoadTestMap => loadTestMap;

        public DebugManager()
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                Instance = this;
            }
        }
        
        public void Start()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                Destroy(gameObject);
                return;
            }
        }

        public void Update()
        {
            if (overrideStartingTileSelectionMode)
            {
                LayoutManager.Instance.TileSelectionMode = tileSelectionMode;
            }
        }

    }
}
