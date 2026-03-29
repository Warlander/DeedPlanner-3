using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Warlogic.LaunchRedirect
{
    /// <summary>
    /// Redirects to the configured startup scene when pressing Play, so the program can initialize correctly.
    /// Configure the startup scene via Edit → Project Settings → Launch Redirect.
    /// If no settings asset exists, pressing Play works normally.
    /// </summary>
    public static class LaunchRedirect
    {
        private const string PreviousSceneKey = "Warlander_LaunchRedirect_PreviousScene";

        private static string PreviousScene
        {
            get => SessionState.GetString(PreviousSceneKey, null);
            set => SessionState.SetString(PreviousSceneKey, value);
        }

        [InitializeOnLoadMethod]
        private static void OnReload()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange playMode)
        {
            string startupScenePath = GetStartupScenePath();
            if (string.IsNullOrEmpty(startupScenePath))
            {
                return;
            }

            string currentScenePath = SceneManager.GetActiveScene().path;
            if (playMode == PlayModeStateChange.ExitingEditMode && currentScenePath != startupScenePath)
            {
                PreviousScene = currentScenePath;
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += ForcePlayFromStartupScene;
            }
            else if (playMode == PlayModeStateChange.EnteredEditMode)
            {
                if (!string.IsNullOrEmpty(PreviousScene))
                {
                    EditorSceneManager.OpenScene(PreviousScene, OpenSceneMode.Single);
                    PreviousScene = null;
                }
            }
        }

        private static void ForcePlayFromStartupScene()
        {
            string startupScenePath = GetStartupScenePath();
            if (string.IsNullOrEmpty(startupScenePath))
            {
                return;
            }

            EditorSceneManager.OpenScene(startupScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static string GetStartupScenePath()
        {
            return LaunchRedirectSettings.LoadStartupScenePath();
        }
    }
}
