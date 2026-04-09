using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public static class RegistryBrowserConfig
    {
        private const string SettingsFilePath = "ProjectSettings/RegistryBrowserConfig.json";

        [Serializable]
        private class SettingsData
        {
            public bool showPackageManagerWarning = true;
            public List<RegistryScope> registries = new();
        }

        public static IReadOnlyList<RegistryScope> LoadRegistries()
        {
            if (!File.Exists(SettingsFilePath))
                return Array.Empty<RegistryScope>();

            SettingsData data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(SettingsFilePath));
            return data?.registries ?? new List<RegistryScope>();
        }

        public static bool LoadShowPackageManagerWarning()
        {
            if (!File.Exists(SettingsFilePath))
                return true;

            SettingsData data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(SettingsFilePath));
            return data?.showPackageManagerWarning ?? true;
        }

        private static void Save(bool showPackageManagerWarning, List<RegistryScope> registries)
        {
            var data = new SettingsData { showPackageManagerWarning = showPackageManagerWarning, registries = registries };
            File.WriteAllText(SettingsFilePath, JsonUtility.ToJson(data, true));
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            List<RegistryScope> editing = null;
            bool showWarning = true;

            var provider = new SettingsProvider("Project/Registry Browser", SettingsScope.Project)
            {
                label = "Registry Browser",
                guiHandler = _ =>
                {
                    if (editing == null)
                    {
                        editing = new List<RegistryScope>(LoadRegistries());
                        showWarning = LoadShowPackageManagerWarning();
                    }

                    EditorGUILayout.LabelField("Package Manager Integration", EditorStyles.boldLabel);
                    EditorGUILayout.Space(4);

                    bool newShowWarning = EditorGUILayout.ToggleLeft("Show Warning for Managed Packages", showWarning);
                    EditorGUILayout.Space(8);

                    EditorGUILayout.LabelField("Tracked Registries", EditorStyles.boldLabel);
                    EditorGUILayout.Space(4);

                    bool changed = newShowWarning != showWarning;
                    showWarning = newShowWarning;
                    int removeIndex = -1;

                    for (int i = 0; i < editing.Count; i++)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        string newScope = EditorGUILayout.TextField("Scope Prefix", editing[i].Scope);
                        string newUrl = EditorGUILayout.TextField("Registry URL", editing[i].RegistryUrl);

                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                            removeIndex = i;

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);

                        if (newScope != editing[i].Scope || newUrl != editing[i].RegistryUrl)
                        {
                            editing[i] = new RegistryScope(newScope, newUrl);
                            changed = true;
                        }
                    }

                    if (removeIndex >= 0)
                    {
                        editing.RemoveAt(removeIndex);
                        changed = true;
                    }

                    if (GUILayout.Button("Add Registry"))
                    {
                        editing.Add(new RegistryScope("", ""));
                        changed = true;
                    }

                    if (changed)
                        Save(showWarning, editing);
                }
            };

            return provider;
        }
    }
}
