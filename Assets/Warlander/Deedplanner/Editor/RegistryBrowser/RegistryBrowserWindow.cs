using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public class RegistryBrowserWindow : EditorWindow
    {
        private RegistryApiClient _apiClient;
        private PackageListPanel _listPanel;
        private PackageDetailPanel _detailPanel;

        [MenuItem("Window/Warlander/Registry Browser")]
        public static void Open()
        {
            RegistryBrowserWindow window = GetWindow<RegistryBrowserWindow>();
            window.titleContent = new GUIContent("Registry Browser");
            window.Show();
        }

        private void CreateGUI()
        {
            minSize = new Vector2(600, minSize.y);

            _apiClient = new RegistryApiClient();
            _listPanel = new PackageListPanel();
            _detailPanel = new PackageDetailPanel();

            _listPanel.PackageSelected += OnPackageSelected;

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingLeft = 6;
            toolbar.style.paddingRight = 6;
            toolbar.style.paddingTop = 4;
            toolbar.style.paddingBottom = 4;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f);

            var configButton = new Button(OnRegistryConfigClicked) { text = "Registry Config" };
            toolbar.Add(configButton);
            rootVisualElement.Add(toolbar);

            var split = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;
            split.Add(_listPanel.CreateRoot());
            split.Add(_detailPanel.CreateRoot());
            rootVisualElement.Add(split);

            _detailPanel.ShowEmpty();
            _ = LoadPackagesAsync();
        }

        private static void OnRegistryConfigClicked()
        {
            SettingsService.OpenProjectSettings("Project/Registry Browser");
        }

        private async System.Threading.Tasks.Task LoadPackagesAsync()
        {
            IReadOnlyList<RegistryScope> registries = RegistryBrowserConfig.LoadRegistries();

            if (registries.Count == 0)
            {
                _listPanel.ShowNoRegistriesConfigured();
                return;
            }

            try
            {
                IReadOnlyList<PackageSummary> packages = await _apiClient.FetchPackagesAsync(registries);
                _listPanel.SetPackages(packages);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RegistryBrowser] Fetch failed: {ex}");
            }
        }

        private async void OnPackageSelected(PackageSummary summary)
        {
            _detailPanel.ShowLoading();
            PackageDetails details = await _apiClient.FetchPackageDetailsAsync(summary.Id, summary.RegistryUrl);
            _detailPanel.ShowPackage(details);
        }
    }
}
