using UnityEditor;
using UnityEditor.SceneManagement;

namespace Warlander.Deedplanner.Editor
{
    public static class OpenSceneMenu
    {
        [MenuItem("Scenes/Loading")]
        public static void OpenLoadingScene()
        {
            OpenScene("Assets/Scenes/LoadingScene.unity");
        }
    
        [MenuItem("Scenes/Main")]
        public static void OpenMainScene()
        {
            OpenScene("Assets/Scenes/MainScene.unity");
        }

        private static void OpenScene(string scene)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(scene);
        }
    }
}