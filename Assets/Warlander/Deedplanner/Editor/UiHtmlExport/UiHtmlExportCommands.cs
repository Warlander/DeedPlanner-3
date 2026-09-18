using System;
using System.IO;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Editor.UiHtmlExport
{
    public static class UiHtmlExportCommands
    {
        private const string MenuPath = "DeedPlanner/Export Selected UI as HTML";

        [MenuItem(MenuPath)]
        public static void ExportSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null || selected.GetComponent<RectTransform>() == null)
                return;

            string projectPath = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string outputPath = EditorUtility.SaveFolderPanel("Export selected UI as HTML", projectPath,
                selected.name + "-html");
            if (string.IsNullOrEmpty(outputPath))
                return;

            try
            {
                UiHtmlExportResult result = ExportGameObject(selected, outputPath);
                EditorUtility.RevealInFinder(result.Path);
                EditorUtility.DisplayDialog("uGUI HTML export",
                    $"Exported {result.ElementCount} visual elements to:\n{result.Path}\n\n" +
                    $"RectTransforms: {result.RectTransformCount}\nImages: {result.ImageCount}\n" +
                    $"Raw Images: {result.RawImageCount}\n" +
                    $"Text: {result.TextCount}\nWarnings: {result.Warnings.Length}",
                    "OK");
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("uGUI HTML export failed", exception.Message, "OK");
            }
        }

        [MenuItem(MenuPath, true)]
        public static bool ValidateExportSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null || selected.GetComponent<RectTransform>() == null)
                return false;

            return EditorUtility.IsPersistent(selected)
                ? PrefabUtility.IsPartOfPrefabAsset(selected)
                : selected.activeInHierarchy;
        }

        [CliCommand("ui_export_html",
            "Export an active RectTransform hierarchy or UI prefab asset as a generic HTML fragment bundle.",
            MainThreadRequired = true, Tags = new[] { "ui", "capture" })]
        public static UiHtmlExportResult Export(
            [CliArg("target",
                "RectTransform GameObject or prefab asset reference (path, globalId, instanceId, or hierarchyPath).",
                Required = true)] ObjectRef target,
            [CliArg("output", "Empty output directory path.", Required = true)] string output)
        {
            if (!ObjectResolver.TryResolve(target, out Object resolved, out string error))
                throw new ArgumentException(error, nameof(target));

            GameObject gameObject = resolved as GameObject ?? (resolved as Component)?.gameObject;
            if (gameObject == null)
                throw new ArgumentException($"Target '{target}' is not a GameObject or Component.", nameof(target));

            return ExportGameObject(gameObject, output);
        }

        private static UiHtmlExportResult ExportGameObject(GameObject gameObject, string output)
        {
            if (!EditorUtility.IsPersistent(gameObject))
                return ExportRectTransform(gameObject, output);

            if (!PrefabUtility.IsPartOfPrefabAsset(gameObject))
                throw new ArgumentException($"Persistent target '{gameObject.name}' is not a prefab asset.",
                    nameof(gameObject));

            string assetPath = AssetDatabase.GetAssetPath(gameObject);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabRoot != gameObject)
                throw new ArgumentException("Direct prefab export requires the prefab asset root.", nameof(gameObject));

            GameObject stagedRoot = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                return ExportRectTransform(stagedRoot, output);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(stagedRoot);
            }
        }

        private static UiHtmlExportResult ExportRectTransform(GameObject gameObject, string output)
        {
            RectTransform root = gameObject.GetComponent<RectTransform>();
            if (root == null)
                throw new ArgumentException($"Target '{gameObject.name}' does not have a RectTransform.",
                    nameof(gameObject));

            return UiHtmlExporter.Export(root, output);
        }
    }
}
