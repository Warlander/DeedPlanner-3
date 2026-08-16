#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Warlander.Deedplanner.Graphics.Outline;

namespace Warlander.Deedplanner.Editor
{
    /// <summary>
    /// Automatically adds <see cref="ScreenSpaceOutlineFeature"/> to every
    /// <see cref="UniversalRendererData"/> asset in the project when the Unity Editor loads.
    /// This removes the need for any manual renderer-asset configuration.
    /// </summary>
    [InitializeOnLoad]
    static class OutlineFeatureSetup
    {
        static OutlineFeatureSetup()
        {
            // Run on the next editor update tick. EditorApplication.update fires
            // every frame regardless of editor focus, making it more reliable than
            // delayCall (which requires the editor window to be active).
            EditorApplication.update += RunOnNextUpdate;
        }

        static void RunOnNextUpdate()
        {
            EditorApplication.update -= RunOnNextUpdate;
            SetupOutlineFeature();
        }

        static void SetupOutlineFeature()
        {
            bool dirty = false;

            string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (rendererData == null) continue;

                bool hasFeature = rendererData.rendererFeatures
                    .Any(f => f is ScreenSpaceOutlineFeature);

                if (hasFeature) continue;

                var feature = ScriptableObject.CreateInstance<ScreenSpaceOutlineFeature>();
                feature.name = "ScreenSpaceOutlineFeature";

                // Sub-asset: the feature must live inside the renderer data asset.
                AssetDatabase.AddObjectToAsset(feature, rendererData);

                rendererData.rendererFeatures.Add(feature);

                // UniversalRendererData keeps a parallel m_RendererFeatureMap list
                // (List<long> of instance IDs) for serialisation consistency.
                // Updating it via reflection prevents "Feature count mismatch" warnings.
                UpdateFeatureMap(rendererData, feature);

                EditorUtility.SetDirty(rendererData);
                dirty = true;

                Debug.Log($"[OutlineFeatureSetup] Added ScreenSpaceOutlineFeature to '{path}'.");
            }

            if (dirty)
                AssetDatabase.SaveAssets();
        }

        private static void UpdateFeatureMap(ScriptableRendererData data, ScriptableRendererFeature feature)
        {
            FieldInfo mapField = typeof(ScriptableRendererData).GetField(
                "m_RendererFeatureMap",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (mapField == null) return;

            var map = mapField.GetValue(data) as List<long>;
            if (map == null) return;

            map.Add(feature.GetInstanceID());
        }
    }
}
#endif
