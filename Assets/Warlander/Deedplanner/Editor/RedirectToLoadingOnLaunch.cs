using System.Collections;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Warlander.Deedplanner.Editor
{
    /// <summary>
    /// Force redirect to main menu so we can actually login and enter the game correctly.
    /// </summary>
    public static class RedirectToLoadingOnLaunch
    {
        [InitializeOnLoadMethod]
        private static void OnReload()
        {
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
        }

        private static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange playMode)
        {
            if (playMode == PlayModeStateChange.EnteredPlayMode && SceneManager.GetSceneAt(0).buildIndex != 0)
            {
                SceneManager.LoadScene(0);
                EditorCoroutineUtility.StartCoroutineOwnerless(ClearConsoleAfterFrame());
            }
        }

        private static IEnumerator ClearConsoleAfterFrame()
        {
            yield return null;
            ClearConsole();
        }

        private static void ClearConsole()
        {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }
    }
}