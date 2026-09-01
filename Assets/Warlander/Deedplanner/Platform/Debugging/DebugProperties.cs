using Warlander.Deedplanner.Editing;
using System;
using System.IO;
using UnityEngine;

namespace Warlander.Deedplanner.Platform.Debugging
{
    public class DebugProperties : ScriptableObject
    {
        private static string SettingsPath => Path.Combine(Application.dataPath, "../UserSettings/DebugProperties.json");

        private static DebugProperties _current;

        public static DebugProperties Current
        {
            get
            {
                if (_current == null)
                {
                    _current = CreateInstance<DebugProperties>();
                    _current.hideFlags = HideFlags.HideAndDontSave;
#if UNITY_EDITOR
                    if (File.Exists(SettingsPath))
                    {
                        JsonUtility.FromJsonOverwrite(File.ReadAllText(SettingsPath), _current);
                    }
#endif
                }
                return _current;
            }
        }

#if UNITY_EDITOR
        public void Save()
        {
            File.WriteAllText(SettingsPath, JsonUtility.ToJson(this, true));
        }
#endif

        [SerializeField] private TestMap _testMap = TestMap.Warland;
        [SerializeField] private bool _preloadAllDecorations = false;
        [SerializeField] private bool _overrideStartingTileSelectionMode = false;
        [SerializeField] private TileSelectionMode _tileSelectionMode = TileSelectionMode.Nothing;
        [SerializeField] private bool _drawDebugPlaneLines = false;

        public TestMap SelectedTestMap
        {
            get => _testMap;
            set => _testMap = value;
        }
        public string TestMapPath => GetTestMapPath(_testMap);
        public bool PreloadAllDecorations
        {
            get => _preloadAllDecorations;
            set => _preloadAllDecorations = value;
        }
        public bool OverrideStartingTileSelectionMode
        {
            get => _overrideStartingTileSelectionMode;
            set => _overrideStartingTileSelectionMode = value;
        }
        public TileSelectionMode TileSelectionMode
        {
            get => _tileSelectionMode;
            set => _tileSelectionMode = value;
        }
        public bool DrawDebugPlaneLines
        {
            get => _drawDebugPlaneLines;
            set => _drawDebugPlaneLines = value;
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
                    return null;
            }
        }

        [Serializable]
        public enum TestMap
        {
            None, Warland, Roofs, Bridges, AssetZoo
        }
    }
}
