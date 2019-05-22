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
        private bool testHeightEditIndicators = false;

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

            if (testHeightEditIndicators)
            {
                for (int i = 0; i <= GameManager.Instance.Map.Width; i++)
                {
                    for (int i2 = 0; i2 <= GameManager.Instance.Map.Height; i2++)
                    {
                        GameManager.Instance.Map.SurfaceGridMesh.TogglePoint(i, i2, true);
                    }
                }
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
