using System.Collections.Generic;
using SFB;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Logic.Projectors;

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
                MapProjector horizontalLine = MapProjectorManager.Instance.RequestProjector(ProjectorColor.Green);
                horizontalLine.ProjectLine(new Vector2Int(5, 5), PlaneAlignment.Horizontal);
                MapProjector firstVerticalLine = MapProjectorManager.Instance.RequestProjector(ProjectorColor.Red);
                firstVerticalLine.ProjectLine(new Vector2Int(5, 5), PlaneAlignment.Vertical);
                MapProjector secondVerticalLine = MapProjectorManager.Instance.RequestProjector(ProjectorColor.Yellow);
                secondVerticalLine.ProjectLine(new Vector2Int(15, 15), PlaneAlignment.Vertical);
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

        public void LoadModelDebug()
        {
            ExtensionFilter[] extensions = new[] {
                new ExtensionFilter("WOM Models", "wom"),
                new ExtensionFilter("All Files", "*" ),
            };
            
            string[] selection = StandaloneFileBrowser.OpenFilePanel("Open Model", "", extensions, false);
            
            if (selection == null || selection.Length == 0)
            {
                return;
            }

            string modelFilePath = selection[0];
            
            CoroutineManager.Instance.QueueCoroutine(WurmAssetsLoader.LoadModel(modelFilePath, OnModelLoaded));
        }

        public void ShowTextWindowDebug()
        {
            Window window = GuiManager.Instance.CreateTextWindow("Test Window", "Testing\nTesting Longer Text");
            window.ShowWindow();
        }

        private void OnModelLoaded(GameObject model)
        {
            
        }
    }
}
