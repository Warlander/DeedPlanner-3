using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    public class DebugManager : MonoBehaviour
    {

        public static DebugManager Instance { get; private set; }
        
        [SerializeField] private bool loadTestMap = false;
        [SerializeField] private bool preloadAllDecorations = false;

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

        private void Start()
        {
            if (drawDebugPlaneLines)
            {
                PlaneLine horizontalLine = Instantiate(GameManager.Instance.PlaneLinePrefab);
                horizontalLine.Alignment = PlaneAlignment.Horizontal;
                horizontalLine.TileCoords = new Vector2Int(5, 5);
                PlaneLine firstVerticalLine = Instantiate(GameManager.Instance.PlaneLinePrefab);
                firstVerticalLine.Alignment = PlaneAlignment.Vertical;
                firstVerticalLine.TileCoords = new Vector2Int(5, 5);
                PlaneLine secondVerticalLine = Instantiate(GameManager.Instance.PlaneLinePrefab);
                secondVerticalLine.Alignment = PlaneAlignment.Vertical;
                secondVerticalLine.TileCoords = new Vector2Int(15, 15);
            }

            if (preloadAllDecorations)
            {
                foreach (KeyValuePair<string,DecorationData> pair in Database.Decorations)
                {
                    DecorationData data = pair.Value;
                    CoroutineManager.Instance.QueueCoroutine(data.Model.CreateOrGetModel(Destroy));
                }
            }
        }
        
        private void Update()
        {
            if (overrideStartingTileSelectionMode)
            {
                LayoutManager.Instance.TileSelectionMode = tileSelectionMode;
            }
        }

    }
}
