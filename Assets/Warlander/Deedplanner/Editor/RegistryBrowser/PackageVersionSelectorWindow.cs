using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public class PackageVersionSelectorWindow : EditorWindow
    {
        private PackageDetails _details;
        private string _installedVersion;
        private RegistryApiClient _apiClient;
        private Action _onOperationCompleted;

        private PopupField<string> _versionDropdown;
        private VisualElement _changelogContainer;

        public static void Open(PackageDetails details, string installedVersion, RegistryApiClient apiClient, Action onOperationCompleted)
        {
            var window = CreateInstance<PackageVersionSelectorWindow>();
            window._details = details;
            window._installedVersion = installedVersion;
            window._apiClient = apiClient;
            window._onOperationCompleted = onOperationCompleted;

            bool isAdding = string.IsNullOrEmpty(installedVersion);
            window.titleContent = new GUIContent(isAdding ? "Add to Project" : "Change Version");
            window.minSize = new Vector2(480, 500);
            window.ShowUtility();
        }

        private void CreateGUI()
        {
            bool isAdding = string.IsNullOrEmpty(_installedVersion);

            var root = rootVisualElement;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 12;
            root.style.paddingBottom = 12;

            var titleLabel = new Label(isAdding
                ? $"Add \"{_details.DisplayName}\" to project"
                : $"Change \"{_details.DisplayName}\" version");
            titleLabel.style.fontSize = 15;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 8;
            titleLabel.style.whiteSpace = WhiteSpace.Normal;
            root.Add(titleLabel);

            if (!isAdding)
            {
                var installedLabel = new Label($"Currently installed: {_installedVersion}");
                installedLabel.style.marginBottom = 8;
                installedLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                root.Add(installedLabel);
            }

            var selectionRow = new VisualElement();
            selectionRow.style.flexDirection = FlexDirection.Row;
            selectionRow.style.marginBottom = 10;
            root.Add(selectionRow);

            var versions = new List<string>(_details.Versions);
            string defaultVersion = isAdding
                ? (versions.Count > 0 ? versions[0] : "")
                : (_installedVersion ?? (versions.Count > 0 ? versions[0] : ""));

            int defaultIndex = versions.IndexOf(defaultVersion);
            if (defaultIndex < 0)
                defaultIndex = 0;

            _versionDropdown = new PopupField<string>(versions, defaultIndex);
            _versionDropdown.style.flexGrow = 1;
            _versionDropdown.style.marginRight = 6;
            selectionRow.Add(_versionDropdown);

            var confirmButton = new Button(OnConfirmClicked)
            {
                text = isAdding ? "Add to project" : "Change version"
            };
            confirmButton.style.paddingLeft = 12;
            confirmButton.style.paddingRight = 12;
            selectionRow.Add(confirmButton);

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
            _ = LoadChangelogAsync();
        }

        private void OnConfirmClicked()
        {
            string selectedVersion = _versionDropdown.value;
            Action callback = _onOperationCompleted;
            RegistryApiClient apiClient = _apiClient;
            string id = _details.Id;

            Close();

            _ = PerformAddAsync(apiClient, id, selectedVersion, callback);
        }

        private static async Task PerformAddAsync(RegistryApiClient apiClient, string id, string version, Action callback)
        {
            try
            {
                await apiClient.AddPackageAsync(id, version);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RegistryBrowser] Failed to add/change package: {ex.Message}");
            }
            callback?.Invoke();
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
                    RenderChangelog(_changelogContainer, text);
                else
                    SetSectionStatus(_changelogContainer, "No changelog available.");

                anyEntry = true;
            }

            if (!anyEntry)
                SetChangelogStatus("No versions available.");
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

        internal static void RenderChangelog(VisualElement container, string text)
        {
            string[] lines = text.Split('\n');
            var bodyLines = new List<string>();

            void FlushBody()
            {
                string body = string.Join("\n", bodyLines).Trim();
                bodyLines.Clear();
                if (string.IsNullOrEmpty(body))
                    return;
                var label = new Label(body);
                label.style.whiteSpace = WhiteSpace.Normal;
                container.Add(label);
            }

            foreach (string line in lines)
            {
                if (line.StartsWith("### "))
                {
                    FlushBody();
                    var header = new Label(line.Substring(4).Trim());
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.style.marginTop = 6;
                    header.style.marginBottom = 2;
                    container.Add(header);
                }
                else
                {
                    bodyLines.Add(line);
                }
            }
            FlushBody();
        }
    }
}
