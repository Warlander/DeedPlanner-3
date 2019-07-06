using System;
using System.IO;
using UnityEngine;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    public class DebugManager : MonoBehaviour
    {

        [SerializeField]
        private bool loadTestMap = false;

        [SerializeField]
        private bool overrideStartingTileSelectionMode = false;
        [SerializeField]
        private TileSelectionMode tileSelectionMode = TileSelectionMode.Nothing;

        public void Start()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                Destroy(gameObject);
                return;
            }

            if (loadTestMap)
            {
                string fullTestMapLocation = Path.Combine(Application.streamingAssetsPath, "./Special/Test Map.MAP");
                StartCoroutine(GameManager.Instance.LoadMap(new Uri(fullTestMapLocation)));
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
