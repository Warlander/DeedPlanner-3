using System;
using System.IO;
using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Debugging
{
    [CreateAssetMenu(menuName = "DeedPlanner/Debug Properties", fileName = "DebugProperties")]
    public class DebugProperties : ScriptableObject
    {
        [SerializeField] private TestMap _testMap = TestMap.Warland;
        [SerializeField] private bool _preloadAllDecorations = false;
        [SerializeField] private bool _overrideStartingTileSelectionMode = false;
        [SerializeField] private TileSelectionMode _tileSelectionMode = TileSelectionMode.Nothing;
        [SerializeField] private bool _drawDebugPlaneLines = false;

        public string TestMapPath => GetTestMapPath(_testMap);
        public bool PreloadAllDecorations => _preloadAllDecorations;
        public bool OverrideStartingTileSelectionMode => _overrideStartingTileSelectionMode;
        public TileSelectionMode TileSelectionMode => _tileSelectionMode;
        public bool DrawDebugPlaneLines => _drawDebugPlaneLines;
        
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