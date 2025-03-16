using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Warlander.Deedplanner.Editor
{
    /// <summary>
    /// Force redirect to main menu so we can actually login and enter the game correctly.
    /// </summary>
    public static class RedirectToLoadingOnLaunch
    {
        private const string LoadingScenePath = "Assets/Scenes/LoadingScene.unity";
        private const string PreviousSceneKey = "Warlander_LaunchRedirect_PreviousScene";

        private static string PreviousScene
        {
            get => EditorPrefs.GetString(PreviousSceneKey, null);
            set => EditorPrefs.SetString(PreviousSceneKey, value);
        }
        
        [InitializeOnLoadMethod]
        private static void OnReload()
        {
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
        }

        private static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange playMode)
        {
            string currentScenePath = SceneManager.GetActiveScene().path;
            if (playMode == PlayModeStateChange.ExitingEditMode && currentScenePath != LoadingScenePath)
            {
                PreviousScene = SceneManager.GetActiveScene().path;
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += ForcePlayFromStartupScene;
            }
            else if (playMode == PlayModeStateChange.EnteredEditMode)
            {
                if (PreviousScene != null)
                {
                    EditorSceneManager.OpenScene(PreviousScene, OpenSceneMode.Single);
                    PreviousScene = null;
                }
            }
        }

        private static void ForcePlayFromStartupScene()
        {
            EditorSceneManager.OpenScene(LoadingScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
    }
}