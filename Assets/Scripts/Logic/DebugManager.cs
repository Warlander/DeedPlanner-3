using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    public class DebugManager : MonoBehaviour
    {

        [SerializeField]
        private bool testSurfaceHeights = false;

        [SerializeField]
        private bool overrideStartingTileSelectionMode = false;
        [SerializeField]
        private TileSelectionMode tileSelectionMode = TileSelectionMode.Nothing;

        public void Start()
        {
            if (!Application.isEditor)
            {
                Destroy(gameObject);
                return;
            }

            if (testSurfaceHeights)
            {
                Map map = GameManager.Instance.Map;
                map[5, 5].Surface.Height = 30;
                map[6, 5].Surface.Height = 30;
                map[5, 6].Surface.Height = 30;
                map[6, 6].Surface.Height = 30;
                map[5, 7].Surface.Height = 10;
                map[6, 7].Surface.Height = 10;
                map[5, 8].Surface.Height = -10;
                map[6, 8].Surface.Height = -10;
            }

            if (overrideStartingTileSelectionMode)
            {
                LayoutManager.Instance.TileSelectionMode = tileSelectionMode;
            }
        }

    }
}
