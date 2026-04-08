using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public class PackageDetailPanel
    {
        private readonly RegistryApiClient _apiClient;

        private VisualElement _root;
        private Label _statusLabel;
        private VisualElement _detailContent;
        private Label _displayNameLabel;
        private Label _idLabel;
        private Label _descriptionLabel;
        private Label _latestVersionLabel;
        private Label _repositoryLabel;
        private Label _registryUrlLabel;
        private VisualElement _versionsContainer;

        private string _repositoryUrl;
        private string _changelogUrl;
        private bool _changelogFetched;
        private IReadOnlyDictionary<string, string> _changelogCache;
        private readonly Dictionary<string, VisualElement> _versionChangelogLabels = new();

        public PackageDetailPanel(RegistryApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public VisualElement CreateRoot()
        {
            _root = new ScrollView(ScrollViewMode.Vertical);
            var root = _root;
            root.style.flexGrow = 1;
            root.style.minWidth = 400;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;

            _statusLabel = new Label();
            _statusLabel.style.marginBottom = 8;
            root.Add(_statusLabel);

            _detailContent = new VisualElement();
            _detailContent.style.display = DisplayStyle.None;
            root.Add(_detailContent);

            _displayNameLabel = new Label();
            _displayNameLabel.style.fontSize = 18;
            _displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _displayNameLabel.style.marginBottom = 8;
            _displayNameLabel.style.whiteSpace = WhiteSpace.Normal;
            _detailContent.Add(_displayNameLabel);

            _idLabel = AddDetailRow(_detailContent, "Package ID");
            _latestVersionLabel = AddDetailRow(_detailContent, "Latest Version");
            _descriptionLabel = AddDetailRow(_detailContent, "Description");
            _repositoryLabel = AddDetailRow(_detailContent, "Repository");
            _registryUrlLabel = AddDetailRow(_detailContent, "Registry");

            var versionsHeader = new Label("Versions");
            versionsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            versionsHeader.style.marginTop = 12;
            versionsHeader.style.marginBottom = 4;
            _detailContent.Add(versionsHeader);

            _versionsContainer = new VisualElement();
            _detailContent.Add(_versionsContainer);

            return root;
        }

        public void ShowPackage(PackageDetails details)
        {
            _statusLabel.text = "";
            _statusLabel.style.display = DisplayStyle.None;
            _detailContent.style.display = DisplayStyle.Flex;

            _displayNameLabel.text = details.DisplayName;
            _idLabel.text = details.Id;
            _latestVersionLabel.text = details.LatestVersion;
            _descriptionLabel.text = details.Description;
            _repositoryLabel.text = details.RepositoryUrl;
            _registryUrlLabel.text = details.RegistryUrl;

            _repositoryUrl = details.RepositoryUrl;
            _changelogUrl = details.ChangelogUrl;
            _changelogFetched = false;
            _changelogCache = null;
            _versionChangelogLabels.Clear();
            _versionsContainer.Clear();

            foreach (string version in details.Versions)
            {
                var changelogContainer = new VisualElement();
                changelogContainer.style.paddingLeft = 4;
                changelogContainer.style.paddingTop = 2;
                changelogContainer.style.paddingBottom = 4;

                var foldout = new Foldout { text = version, value = false };
                foldout.Add(changelogContainer);
                _versionsContainer.Add(foldout);
                _versionChangelogLabels[version] = changelogContainer;

                foldout.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                        _ = EnsureChangelogsLoadedAsync();
                });
            }
        }

        public void ShowLoading()
        {
            _statusLabel.text = "Loading\u2026";
            _statusLabel.style.display = DisplayStyle.Flex;
            _detailContent.style.display = DisplayStyle.None;
        }

        public void ShowEmpty()
        {
            _statusLabel.text = "Select a package to view details.";
            _statusLabel.style.display = DisplayStyle.Flex;
            _detailContent.style.display = DisplayStyle.None;
        }

        public Task FadeOutAsync(CancellationToken ct = default) => FadeAsync(1f, 0f, 0.1f, ct);

        public Task FadeInAsync(CancellationToken ct = default) => FadeAsync(0f, 1f, 0.1f, ct);

        private Task FadeAsync(float from, float to, float durationSec, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();
            _root.style.opacity = from;
            double startTime = EditorApplication.timeSinceStartup;
            _root.schedule.Execute(() =>
            {
                if (ct.IsCancellationRequested)
                {
                    _root.style.opacity = to;
                    tcs.TrySetResult(true);
                    return;
                }
                float t = Mathf.Clamp01((float)((EditorApplication.timeSinceStartup - startTime) / durationSec));
                _root.style.opacity = from + (to - from) * t;
                if (t >= 1f)
                    tcs.TrySetResult(true);
            }).Every(16).Until(() => tcs.Task.IsCompleted);
            return tcs.Task;
        }

        private async Task EnsureChangelogsLoadedAsync()
        {
            if (_changelogFetched)
                return;

            _changelogFetched = true;

            foreach (KeyValuePair<string, VisualElement> entry in _versionChangelogLabels)
                SetChangelogStatus(entry.Value, "Loading changelog\u2026");

            try
            {
                _changelogCache = await _apiClient.FetchChangelogsAsync(_changelogUrl, _repositoryUrl);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RegistryBrowser] Changelog fetch failed: {ex.Message}");
                _changelogCache = new Dictionary<string, string>();
            }

            foreach (KeyValuePair<string, VisualElement> entry in _versionChangelogLabels)
            {
                if (_changelogCache.TryGetValue(entry.Key, out string text) && !string.IsNullOrEmpty(text))
                    RenderChangelog(entry.Value, text);
                else
                    SetChangelogStatus(entry.Value, "No changelog available.");
            }
        }

        private static void SetChangelogStatus(VisualElement container, string message)
        {
            container.Clear();
            var label = new Label(message);
            label.style.whiteSpace = WhiteSpace.Normal;
            container.Add(label);
        }

        private static void RenderChangelog(VisualElement container, string text)
        {
            container.Clear();
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

        private static Label AddDetailRow(VisualElement parent, string fieldName)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 4;
            row.style.flexWrap = Wrap.Wrap;

            var nameLabel = new Label(fieldName + ":");
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.minWidth = 120;
            row.Add(nameLabel);

            var valueLabel = new Label();
            valueLabel.style.flexGrow = 1;
            valueLabel.style.whiteSpace = WhiteSpace.Normal;
            row.Add(valueLabel);

            parent.Add(row);
            return valueLabel;
        }
    }
}
