using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Warlogic.LaunchRedirect
{
    public static class LaunchRedirectSettings
    {
        private const string SettingsFilePath = "ProjectSettings/LaunchRedirectSettings.json";

        [Serializable]
        private class SettingsData
        {
            public string startupScenePath = "";
        }

        public static string LoadStartupScenePath()
        {
            if (!File.Exists(SettingsFilePath))
            {
                return null;
            }

            var data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(SettingsFilePath));
            return string.IsNullOrEmpty(data?.startupScenePath) ? null : data.startupScenePath;
        }

        private static void Save(string scenePath)
        {
            var data = new SettingsData { startupScenePath = scenePath ?? "" };
            File.WriteAllText(SettingsFilePath, JsonUtility.ToJson(data, true));
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Launch Redirect", SettingsScope.Project)
            {
                label = "Launch Redirect",
                guiHandler = _ =>
                {
                    string currentPath = LoadStartupScenePath();
                    var currentScene = string.IsNullOrEmpty(currentPath)
                        ? null
                        : AssetDatabase.LoadAssetAtPath<SceneAsset>(currentPath);

                    EditorGUI.BeginChangeCheck();
                    var newScene = (SceneAsset)EditorGUILayout.ObjectField(
                        new GUIContent("Startup Scene",
                            "Scene to redirect to when pressing Play. Leave empty to disable redirect."),
                        currentScene,
                        typeof(SceneAsset),
                        false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        string newPath = newScene != null ? AssetDatabase.GetAssetPath(newScene) : "";
                        Save(newPath);
                    }
                }
            };
        }
    }
}
