using System.Collections.Generic;
using System.Threading;
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
        private Label _refreshIcon;
        private bool _refreshAnimating;
        private CancellationTokenSource _selectionCts;

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
            _detailPanel = new PackageDetailPanel(_apiClient);

            _listPanel.PackageSelected += OnPackageSelected;
            _detailPanel.OperationCompleted += OnPackageOperationCompleted;

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingLeft = 6;
            toolbar.style.paddingRight = 6;
            toolbar.style.paddingTop = 4;
            toolbar.style.paddingBottom = 4;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f);

            var refreshButton = new Button(OnRefreshClicked);
            refreshButton.style.height = 28;
            refreshButton.style.width = 28;
            refreshButton.style.paddingLeft = 0;
            refreshButton.style.paddingRight = 0;
            refreshButton.style.paddingTop = 0;
            refreshButton.style.paddingBottom = 0;

            _refreshIcon = new Label("\u21BB");
            _refreshIcon.style.position = Position.Absolute;
            _refreshIcon.style.left = 0;
            _refreshIcon.style.right = 0;
            _refreshIcon.style.top = 0;
            _refreshIcon.style.bottom = 0;
            _refreshIcon.style.fontSize = 16;
            _refreshIcon.style.unityFontStyleAndWeight = FontStyle.Bold;
            _refreshIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
            _refreshIcon.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));
            _refreshIcon.pickingMode = PickingMode.Ignore;
            refreshButton.Add(_refreshIcon);
            toolbar.Add(refreshButton);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            var configButton = new Button(OnRegistryConfigClicked) { text = "Registry Config" };
            configButton.style.height = 28;
            configButton.style.paddingLeft = 10;
            configButton.style.paddingRight = 10;
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

        private void OnRefreshClicked()
        {
            _detailPanel.ShowEmpty();
            _ = LoadPackagesAsync();
            AnimateRefresh();
        }

        private void OnPackageOperationCompleted()
        {
            _ = LoadPackagesAsync();
        }

        private void AnimateRefresh()
        {
            if (_refreshAnimating)
                return;
            _refreshAnimating = true;
            const double durationSec = 0.5;
            double startTime = EditorApplication.timeSinceStartup;
            _refreshIcon.schedule.Execute(() =>
            {
                float t = Mathf.Clamp01((float)((EditorApplication.timeSinceStartup - startTime) / durationSec));
                _refreshIcon.style.rotate = new Rotate(Angle.Degrees(360f * t));
                if (t >= 1f)
                {
                    _refreshIcon.style.rotate = new Rotate(Angle.Degrees(0f));
                    _refreshAnimating = false;
                }
            }).Every(16).Until(() => !_refreshAnimating);
        }

        private async System.Threading.Tasks.Task LoadPackagesAsync()
        {
            _listPanel.ShowLoading();
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
            _selectionCts?.Cancel();
            _selectionCts = new CancellationTokenSource();
            CancellationToken ct = _selectionCts.Token;

            await _detailPanel.FadeOutAsync(ct);
            if (ct.IsCancellationRequested)
                return;

            _detailPanel.ShowLoading();
            PackageDetails details = await _apiClient.FetchPackageDetailsAsync(summary.Id, summary.RegistryUrl);
            if (ct.IsCancellationRequested)
                return;

            _detailPanel.ShowPackage(details, summary);
            await _detailPanel.FadeInAsync(ct);
        }
    }
}
