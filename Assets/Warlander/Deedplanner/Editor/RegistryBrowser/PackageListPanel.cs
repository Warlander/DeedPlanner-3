using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public class PackageListPanel
    {
        private ListView _listView;
        private Label _statusLabel;
        private IReadOnlyList<PackageSummary> _packages = Array.Empty<PackageSummary>();
        private bool _animating;
        private double _animationTimer;
        private int _dotCount;

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

            _statusLabel = new Label("Loading packages");
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

            StartLoadingAnimation();
            return root;
        }

        public void SetPackages(IReadOnlyList<PackageSummary> packages)
        {
            StopLoadingAnimation();
            _statusLabel.style.display = DisplayStyle.None;
            _packages = packages;
            _listView.itemsSource = (System.Collections.IList)packages;
            _listView.RefreshItems();
        }

        public void ShowNoRegistriesConfigured()
        {
            StopLoadingAnimation();
            _statusLabel.text = "No registries configured.\nUse the Registry Config button to set up scopes.";
            _statusLabel.style.color = new UnityEngine.Color(0.9f, 0.7f, 0.2f);
            _statusLabel.style.display = DisplayStyle.Flex;
        }

        private void StartLoadingAnimation()
        {
            _animating = true;
            _dotCount = 0;
            _animationTimer = EditorApplication.timeSinceStartup;
            EditorApplication.update += AnimateLoading;
        }

        private void StopLoadingAnimation()
        {
            _animating = false;
            EditorApplication.update -= AnimateLoading;
        }

        private void AnimateLoading()
        {
            if (!_animating)
                return;
            double now = EditorApplication.timeSinceStartup;
            if (now - _animationTimer < 0.5)
                return;
            _animationTimer = now;
            _dotCount = (_dotCount + 1) % 4;
            _statusLabel.text = "Loading packages" + new string('.', _dotCount);
        }

        private static VisualElement MakeItem()
        {
            var container = new VisualElement();
            container.style.paddingLeft = 6;
            container.style.paddingRight = 6;
            container.style.paddingTop = 3;
            container.style.paddingBottom = 3;
            container.style.justifyContent = Justify.Center;

            var idLabel = new Label();
            idLabel.name = "id-label";
            idLabel.style.whiteSpace = WhiteSpace.Normal;

            var versionLabel = new Label();
            versionLabel.name = "version-label";
            versionLabel.style.fontSize = 10;
            versionLabel.style.color = new UnityEngine.Color(0.6f, 0.6f, 0.6f);
            versionLabel.style.whiteSpace = WhiteSpace.Normal;

            container.Add(idLabel);
            container.Add(versionLabel);
            return container;
        }

        private void BindItem(VisualElement element, int index)
        {
            PackageSummary summary = _packages[index];
            element.Q<Label>("id-label").text = summary.Id;
            element.Q<Label>("version-label").text = summary.LatestVersion;
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
