using UnityEditor;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    [InitializeOnLoad]
    internal static class EmbedGitIgnorePrompter
    {
        static EmbedGitIgnorePrompter()
        {
            EditorApplication.update += RunOnNextUpdate;
        }

        private static void RunOnNextUpdate()
        {
            EditorApplication.update -= RunOnNextUpdate;
            CheckAndPrompt();
        }

        private static void CheckAndPrompt()
        {
            if (RegistryBrowserConfig.LoadGitIgnorePromptShown())
                return;

            // Mark as shown first so a failure below doesn't cause the prompt to loop.
            RegistryBrowserConfig.MarkGitIgnorePromptShown();

            if (GitEmbedOperations.IsEmbedFolderInGitIgnore())
                return;

            bool add = EditorUtility.DisplayDialog(
                "Registry Browser Setup",
                "The embed folder (Packages/Embeds/) is not in your .gitignore.\n\n" +
                "Embedded packages are temporary local copies and should not be committed to version control.\n\n" +
                "Add Packages/Embeds/ to .gitignore?",
                "Add to .gitignore",
                "Skip"
            );

            if (add)
                GitEmbedOperations.AddEmbedFolderToGitIgnore();
        }
    }
}
