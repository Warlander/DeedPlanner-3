using System;
using System.Collections.Generic;
using System.IO;
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
        
        [SerializeField] private TestMap testMap = TestMap.Warland;
        [SerializeField] private bool preloadAllDecorations = false;

        [SerializeField] private bool overrideStartingTileSelectionMode = false;
        [SerializeField] private TileSelectionMode tileSelectionMode = TileSelectionMode.Nothing;
        
        [SerializeField] private bool drawDebugPlaneLines = false;

        public bool ShouldLoadTestMap => testMap != TestMap.None;
        public string TestMapPath => GetTestMapPath(testMap);

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
                    data.Model.CreateOrGetModel(Destroy);
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
            
            WurmAssetsLoader.LoadModel(modelFilePath, OnModelLoaded);
        }

        public void ShowTextWindowDebug()
        {
            Window window = GuiManager.Instance.CreateTextWindow("Test Window", "Testing\nTesting Longer Text");
            window.ShowWindow();
        }

        private void OnModelLoaded(GameObject model)
        {
            // Do nothing, this is test of model loading system and we do nothing extra with loaded model.
        }

        private string GetTestMapPath(TestMap map)
        {
            switch (map)
            {
                case TestMap.None:
                    return null;
                case TestMap.Warland:
                    return Path.Combine(Application.streamingAssetsPath, "./Special/Maps/Test Map.MAP");
                case TestMap.Roofs:
                    return Path.Combine(Application.streamingAssetsPath, "./Special/Maps/Roof World.MAP");
                case TestMap.Bridges:
                    return Path.Combine(Application.streamingAssetsPath, "./Special/Maps/Bridge World.MAP");
                default:
                    Debug.LogWarning($"No asset path mapping found for test map {map}");
                    return null;
            }
        }

        [Serializable]
        public enum TestMap
        {
            None, Warland, Roofs, Bridges
        }
    }
}
