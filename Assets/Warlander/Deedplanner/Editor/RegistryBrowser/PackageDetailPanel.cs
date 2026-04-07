using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public class PackageDetailPanel
    {
        private Label _statusLabel;
        private VisualElement _detailContent;
        private Label _idLabel;
        private Label _descriptionLabel;
        private Label _latestVersionLabel;
        private Label _repositoryLabel;
        private Label _registryUrlLabel;
        private ListView _versionsListView;

        public VisualElement CreateRoot()
        {
            var root = new ScrollView(ScrollViewMode.Vertical);
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

            _idLabel = AddDetailRow(_detailContent, "Package ID");
            _latestVersionLabel = AddDetailRow(_detailContent, "Latest Version");
            _descriptionLabel = AddDetailRow(_detailContent, "Description");
            _repositoryLabel = AddDetailRow(_detailContent, "Repository");
            _registryUrlLabel = AddDetailRow(_detailContent, "Registry");

            var versionsHeader = new Label("Versions");
            versionsHeader.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            versionsHeader.style.marginTop = 12;
            versionsHeader.style.marginBottom = 4;
            _detailContent.Add(versionsHeader);

            _versionsListView = new ListView
            {
                makeItem = () => new Label { style = { paddingLeft = 4, paddingTop = 2, paddingBottom = 2 } },
                bindItem = (element, i) => ((Label)element).text = ((IReadOnlyList<string>)_versionsListView.itemsSource)[i],
                selectionType = SelectionType.None,
                fixedItemHeight = 22
            };
            _versionsListView.style.maxHeight = 200;
            _detailContent.Add(_versionsListView);

            return root;
        }

        public void ShowPackage(PackageDetails details)
        {
            _statusLabel.text = "";
            _statusLabel.style.display = DisplayStyle.None;
            _detailContent.style.display = DisplayStyle.Flex;

            _idLabel.text = details.Id;
            _latestVersionLabel.text = details.LatestVersion;
            _descriptionLabel.text = details.Description;
            _repositoryLabel.text = details.RepositoryUrl;
            _registryUrlLabel.text = details.RegistryUrl;

            _versionsListView.itemsSource = (System.Collections.IList)details.Versions;
            _versionsListView.RefreshItems();
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

        private static Label AddDetailRow(VisualElement parent, string fieldName)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 4;
            row.style.flexWrap = Wrap.Wrap;

            var nameLabel = new Label(fieldName + ":");
            nameLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
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
