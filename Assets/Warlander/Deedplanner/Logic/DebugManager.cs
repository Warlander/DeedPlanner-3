using System;
using System.IO;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    public class DebugManager : MonoBehaviour
    {

        public static DebugManager Instance { get; private set; }
        
        [SerializeField] private bool loadTestMap = false;

        [SerializeField] private bool overrideStartingTileSelectionMode = false;
        [SerializeField] private TileSelectionMode tileSelectionMode = TileSelectionMode.Nothing;
        
        [SerializeField] private bool drawDebugPlaneLines = false;

        public bool LoadTestMap => loadTestMap;

        public DebugManager()
        {
            Instance = this;
        }

        private void Awake()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (overrideStartingTileSelectionMode)
            {
                LayoutManager.Instance.TileSelectionMode = tileSelectionMode;
            }

            if (drawDebugPlaneLines)
            {
                Map map = GameManager.Instance.Map;
                if (map.PlaneLines.Count == 0)
                {
                    map.PlaneLines.Add(new PlaneLine(new Vector2Int(5, 5), PlaneAlignment.Horizontal));
                    map.PlaneLines.Add(new PlaneLine(new Vector2Int(5, 5), PlaneAlignment.Vertical));
                    map.PlaneLines.Add(new PlaneLine(new Vector2Int(15, 5), PlaneAlignment.Vertical));
                }
            }
        }

    }
}
