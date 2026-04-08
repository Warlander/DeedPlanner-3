using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public class PackageListPanel
    {
        private ListView _listView;
        private Label _statusLabel;
        private IReadOnlyList<PackageSummary> _packages = Array.Empty<PackageSummary>();
        private LabelLoadingAnimation _loadingAnimation;

        public event Action<PackageSummary> PackageSelected;

        public VisualElement CreateRoot()
        {
            var root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.minWidth = 200;
            root.style.flexDirection = FlexDirection.Column;

            var header = new Label("Packages");
            header.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            header.style.paddingLeft = 6;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            root.Add(header);

            _statusLabel = new Label();
            _statusLabel.style.paddingLeft = 6;
            _statusLabel.style.paddingRight = 6;
            _statusLabel.style.paddingTop = 4;
            _statusLabel.style.color = new UnityEngine.Color(0.6f, 0.6f, 0.6f);
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            root.Add(_statusLabel);

            _listView = new ListView
            {
                makeItem = MakeItem,
                bindItem = BindItem,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };
            _listView.style.flexGrow = 1;
            _listView.selectionChanged += OnSelectionChanged;
            root.Add(_listView);

            _loadingAnimation = new LabelLoadingAnimation(_statusLabel, "Loading packages");
            _loadingAnimation.Start();
            return root;
        }

        public void ShowLoading()
        {
            _loadingAnimation.Stop();
            _packages = Array.Empty<PackageSummary>();
            _listView.itemsSource = null;
            _listView.RefreshItems();
            _statusLabel.style.color = new UnityEngine.Color(0.6f, 0.6f, 0.6f);
            _statusLabel.style.display = DisplayStyle.Flex;
            _loadingAnimation.Start();
        }

        public void SetPackages(IReadOnlyList<PackageSummary> packages)
        {
            _loadingAnimation.Stop();
            _statusLabel.style.display = DisplayStyle.None;
            _packages = packages;
            _listView.itemsSource = (System.Collections.IList)packages;
            _listView.RefreshItems();
        }

        public void ShowNoRegistriesConfigured()
        {
            _loadingAnimation.Stop();
            _statusLabel.text = "No registries configured.\nUse the Registry Config button to set up scopes.";
            _statusLabel.style.color = new UnityEngine.Color(0.9f, 0.7f, 0.2f);
            _statusLabel.style.display = DisplayStyle.Flex;
        }

        private static VisualElement MakeItem()
        {
            var container = new VisualElement();
            container.style.paddingLeft = 6;
            container.style.paddingRight = 6;
            container.style.paddingTop = 3;
            container.style.paddingBottom = 3;
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;

            var statusIcon = new Label();
            statusIcon.name = "status-icon";
            statusIcon.style.width = 32;
            statusIcon.style.minWidth = 32;
            statusIcon.style.fontSize = 24;
            statusIcon.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            statusIcon.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            statusIcon.style.marginRight = 4;

            var textContainer = new VisualElement();
            textContainer.style.flexDirection = FlexDirection.Column;
            textContainer.style.flexGrow = 1;

            var idLabel = new Label();
            idLabel.name = "id-label";
            idLabel.style.whiteSpace = WhiteSpace.Normal;

            var versionLabel = new Label();
            versionLabel.name = "version-label";
            versionLabel.style.fontSize = 10;
            versionLabel.style.whiteSpace = WhiteSpace.Normal;

            textContainer.Add(idLabel);
            textContainer.Add(versionLabel);
            container.Add(statusIcon);
            container.Add(textContainer);
            return container;
        }

        private void BindItem(VisualElement element, int index)
        {
            PackageSummary summary = _packages[index];
            element.Q<Label>("id-label").text = summary.DisplayName;

            Label versionLabel = element.Q<Label>("version-label");
            VersionUpdateLevel updateLevel = summary.Status != PackageInstallStatus.NotInProject
                ? PackageVersionComparator.Compare(summary.InstalledVersion, summary.LatestVersion)
                : VersionUpdateLevel.None;

            if (updateLevel != VersionUpdateLevel.None)
            {
                versionLabel.text = $"{summary.LatestVersion} [{summary.InstalledVersion}]";
                versionLabel.style.color = updateLevel switch
                {
                    VersionUpdateLevel.Patch => new UnityEngine.Color(0.9f, 0.85f, 0.2f),
                    VersionUpdateLevel.Minor => new UnityEngine.Color(0.95f, 0.55f, 0.1f),
                    VersionUpdateLevel.Major => new UnityEngine.Color(0.85f, 0.25f, 0.25f),
                    _ => new UnityEngine.Color(0.6f, 0.6f, 0.6f),
                };
            }
            else
            {
                versionLabel.text = summary.LatestVersion;
                versionLabel.style.color = new UnityEngine.Color(0.6f, 0.6f, 0.6f);
            }

            Label statusIcon = element.Q<Label>("status-icon");
            switch (summary.Status)
            {
                case PackageInstallStatus.NotInProject:
                    statusIcon.text = "\u2717";
                    statusIcon.style.color = new UnityEngine.Color(0.85f, 0.25f, 0.25f);
                    statusIcon.tooltip = "Package not present in the project";
                    break;
                case PackageInstallStatus.InstalledFromRegistry:
                    statusIcon.text = "\u2713";
                    statusIcon.style.color = new UnityEngine.Color(0.35f, 0.75f, 0.35f);
                    statusIcon.tooltip = "Package present in the project, from registry";
                    break;
                case PackageInstallStatus.Embedded:
                    statusIcon.text = "!";
                    statusIcon.style.color = new UnityEngine.Color(0.9f, 0.7f, 0.2f);
                    statusIcon.tooltip = "Package present in the project, embedded";
                    break;
            }
        }

        private void OnSelectionChanged(System.Collections.Generic.IEnumerable<object> selection)
        {
            foreach (object item in selection)
            {
                if (item is PackageSummary summary)
                    PackageSelected?.Invoke(summary);
                return;
            }
        }
    }
}
