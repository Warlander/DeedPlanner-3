using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public class EmbedSelectorWindow : EditorWindow
    {
        private PackageDetails _details;
        private RegistryApiClient _apiClient;
        private Action _onOperationCompleted;

        private VisualElement _selectionRow;
        private PopupField<string> _commitDropdown;
        private List<CommitInfo> _commitInfos;
        private Button _confirmButton;
        private VisualElement _changelogContainer;

        public static void Open(PackageDetails details, RegistryApiClient apiClient, Action onOperationCompleted)
        {
            var window = CreateInstance<EmbedSelectorWindow>();
            window._details = details;
            window._apiClient = apiClient;
            window._onOperationCompleted = onOperationCompleted;
            window.titleContent = new GUIContent("Embed Package");
            window.minSize = new Vector2(480, 500);
            window.ShowUtility();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 12;
            root.style.paddingBottom = 12;

            var titleLabel = new Label($"Embed \"{_details.DisplayName}\"");
            titleLabel.style.fontSize = 15;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 8;
            titleLabel.style.whiteSpace = WhiteSpace.Normal;
            root.Add(titleLabel);

            _selectionRow = new VisualElement();
            _selectionRow.style.flexDirection = FlexDirection.Row;
            _selectionRow.style.marginBottom = 10;
            root.Add(_selectionRow);

            var loadingLabel = new Label("Loading commits\u2026");
            loadingLabel.style.flexGrow = 1;
            loadingLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            _selectionRow.Add(loadingLabel);

            _confirmButton = new Button(OnConfirmClicked) { text = "Embed" };
            _confirmButton.style.paddingLeft = 12;
            _confirmButton.style.paddingRight = 12;
            _confirmButton.SetEnabled(false);
            _selectionRow.Add(_confirmButton);

            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            separator.style.marginBottom = 8;
            root.Add(separator);

            var changelogScroll = new ScrollView(ScrollViewMode.Vertical);
            changelogScroll.style.flexGrow = 1;
            root.Add(changelogScroll);

            _changelogContainer = new VisualElement();
            changelogScroll.Add(_changelogContainer);

            SetChangelogStatus("Loading changelog\u2026");

            _ = LoadCommitsAsync();
            _ = LoadChangelogAsync();
        }

        private async Task LoadCommitsAsync()
        {
            IReadOnlyList<CommitInfo> commits;
            try
            {
                commits = await _apiClient.FetchCommitsAsync(_details.RepositoryUrl);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RegistryBrowser] Failed to fetch commits: {ex.Message}");
                RebuildSelectionRowWithError("Failed to load commits.");
                return;
            }

            if (commits == null || commits.Count == 0)
            {
                RebuildSelectionRowWithError("No commits found.");
                return;
            }

            _commitInfos = new List<CommitInfo>(commits);
            var labels = new List<string>(_commitInfos.Count);
            foreach (CommitInfo c in _commitInfos)
                labels.Add(c.DisplayLabel);

            // Rebuild selection row with actual dropdown
            _selectionRow.Clear();

            _commitDropdown = new PopupField<string>(labels, 0);
            _commitDropdown.style.flexGrow = 1;
            _commitDropdown.style.marginRight = 6;
            _selectionRow.Add(_commitDropdown);

            _confirmButton = new Button(OnConfirmClicked) { text = "Embed" };
            _confirmButton.style.paddingLeft = 12;
            _confirmButton.style.paddingRight = 12;
            _selectionRow.Add(_confirmButton);
        }

        private void RebuildSelectionRowWithError(string message)
        {
            _selectionRow.Clear();
            var errorLabel = new Label(message);
            errorLabel.style.flexGrow = 1;
            errorLabel.style.color = new Color(0.85f, 0.3f, 0.3f);
            _selectionRow.Add(errorLabel);
        }

        private async Task LoadChangelogAsync()
        {
            IReadOnlyDictionary<string, string> changelogs;
            try
            {
                changelogs = await _apiClient.FetchChangelogsAsync(_details.ChangelogUrl, _details.RepositoryUrl);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RegistryBrowser] Changelog fetch failed: {ex.Message}");
                SetChangelogStatus("Failed to load changelog.");
                return;
            }

            _changelogContainer.Clear();

            bool anyEntry = false;
            foreach (string version in _details.Versions)
            {
                var versionHeader = new Label(version);
                versionHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                versionHeader.style.fontSize = 13;
                versionHeader.style.marginTop = anyEntry ? 12 : 0;
                versionHeader.style.marginBottom = 4;
                _changelogContainer.Add(versionHeader);

                if (changelogs.TryGetValue(version, out string text) && !string.IsNullOrEmpty(text))
                    PackageVersionSelectorWindow.RenderChangelog(_changelogContainer, text);
                else
                    SetSectionStatus(_changelogContainer, "No changelog available.");

                anyEntry = true;
            }

            if (!anyEntry)
                SetChangelogStatus("No versions available.");
        }

        private void OnConfirmClicked()
        {
            if (_commitInfos == null || _commitInfos.Count == 0 || _commitDropdown == null)
                return;

            int selectedIndex = _commitDropdown.index;
            if (selectedIndex < 0 || selectedIndex >= _commitInfos.Count)
                return;

            CommitInfo selected = _commitInfos[selectedIndex];
            PackageDetails details = _details;
            Action callback = _onOperationCompleted;

            Close();

            _ = PerformEmbedAsync(details, selected, callback);
        }

        private static async Task PerformEmbedAsync(PackageDetails details, CommitInfo commit, Action callback)
        {
            try
            {
                await GitEmbedOperations.CloneAndCheckoutAsync(details.Id, details.RepositoryUrl, commit.Sha);
                PackageManifestEditor.AddOrUpdateDependency(details.Id, $"file:Embeds/{details.Id}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RegistryBrowser] Failed to embed package: {ex.Message}");
            }
            callback?.Invoke();
        }

        private void SetChangelogStatus(string message)
        {
            _changelogContainer.Clear();
            var label = new Label(message);
            label.style.whiteSpace = WhiteSpace.Normal;
            _changelogContainer.Add(label);
        }

        private static void SetSectionStatus(VisualElement parent, string message)
        {
            var label = new Label(message);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = new Color(0.6f, 0.6f, 0.6f);
            parent.Add(label);
        }
    }
}
