using UnityEditor;
using UnityEngine;
using Warlander.Deedplanner.Debugging;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Editor
{
    public static class DebugPropertiesSettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider Create()
        {
            return new SettingsProvider("Project/DeedPlanner/Debug Properties", SettingsScope.Project)
            {
                label = "Debug Properties",
                guiHandler = Draw
            };
        }

        private static void Draw(string searchContext)
        {
            DebugProperties properties = DebugProperties.Current;

            EditorGUI.BeginChangeCheck();
            properties.SelectedTestMap = (DebugProperties.TestMap)EditorGUILayout.EnumPopup("Test Map", properties.SelectedTestMap);
            properties.PreloadAllDecorations = EditorGUILayout.Toggle("Preload All Decorations", properties.PreloadAllDecorations);
            properties.OverrideStartingTileSelectionMode = EditorGUILayout.Toggle("Override Starting Tile Selection Mode", properties.OverrideStartingTileSelectionMode);
            using (new EditorGUI.IndentLevelScope())
            {
                properties.TileSelectionMode = (TileSelectionMode)EditorGUILayout.EnumPopup("Tile Selection Mode", properties.TileSelectionMode);
            }
            properties.DrawDebugPlaneLines = EditorGUILayout.Toggle("Draw Debug Plane Lines", properties.DrawDebugPlaneLines);

            if (EditorGUI.EndChangeCheck())
            {
                properties.Save();
            }

            EditorGUILayout.HelpBox("Stored per-user in UserSettings/DebugProperties.json. Never committed; builds use code defaults.", MessageType.Info);
        }
    }
}
