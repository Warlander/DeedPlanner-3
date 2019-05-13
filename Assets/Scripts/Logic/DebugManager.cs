using System;
using System.Collections.Generic;
using System.IO;
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
        private bool loadTestMap = false;
        [SerializeField]
        private bool testSurfaceHeights = false;
        [SerializeField]
        private bool testSurfaceFloors = false;

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

            if (loadTestMap)
            {
                string fullTestMapLocation = Path.Combine(Application.streamingAssetsPath, "./Special/Test Map.MAP");
                StartCoroutine(GameManager.Instance.LoadMap(new Uri(fullTestMapLocation)));
            }
            if (testSurfaceHeights)
            {
                Map map = GameManager.Instance.Map;
                map[5, 5].SurfaceHeight = 30;
                map[6, 5].SurfaceHeight = 30;
                map[5, 6].SurfaceHeight = 30;
                map[6, 6].SurfaceHeight = 30;
                map[5, 7].SurfaceHeight = 10;
                map[6, 7].SurfaceHeight = 10;
                map[4, 8].SurfaceHeight = -10;
                map[5, 8].SurfaceHeight = -10;
                map[6, 8].SurfaceHeight = -10;
                map[7, 8].SurfaceHeight = -10;
                map[4, 9].SurfaceHeight = -10;
                map[5, 9].SurfaceHeight = -10;
                map[6, 9].SurfaceHeight = -10;
                map[7, 9].SurfaceHeight = -10;
            }
            if (testSurfaceFloors)
            {
                Map map = GameManager.Instance.Map;
                FloorData exampleData = Database.Floors["wFloor"];
                EntityOrientation orientation = EntityOrientation.Up;
                map[15, 15].SetFloor(exampleData, orientation, 0);
                map[15, 15].SetFloor(exampleData, orientation, 1);
                map[16, 15].SetFloor(exampleData, orientation, 0);
                map[15, 16].SetFloor(exampleData, orientation, 0);
                map[16, 16].SetFloor(exampleData, orientation, 0);
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
