using System;
using System.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Debugging;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Home;
using Warlander.Deedplanner.Logic.Saving;

namespace Warlander.Deedplanner.Logic
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StartupMapSelection : IInitializable
    {
        private readonly MapHandler _mapHandler;
        private readonly IObjectResolver _resolver;
        private readonly ISaveCoordinator _saveCoordinator;
        private readonly IHomeScreenPresenter _homeScreenPresenter;

        public StartupMapSelection(MapHandler mapHandler, IObjectResolver resolver,
            ISaveCoordinator saveCoordinator, IHomeScreenPresenter homeScreenPresenter)
        {
            _mapHandler = mapHandler;
            _resolver = resolver;
            _saveCoordinator = saveCoordinator;
            _homeScreenPresenter = homeScreenPresenter;
        }

        public void Initialize()
        {
            LoadInitialAsync().ToObservable().Subscribe();
        }

        private async Task LoadInitialAsync()
        {
            string mapLocationString = "";
#if UNITY_WEBGL && !UNITY_EDITOR
            mapLocationString = Utils.JavaScriptUtils.GetMapLocationString();
            if (!string.IsNullOrEmpty(mapLocationString))
            {
                mapLocationString = WebLinkUtils.ParseToDirectDownloadLink(mapLocationString);
            }
#endif

            DebugProperties debugProperties = _resolver.ResolveOrDefault<DebugProperties>();

            if (!string.IsNullOrEmpty(mapLocationString))
            {
                if (mapLocationString.Contains("pastebin.com"))
                {
                    await _saveCoordinator.LoadAsync(new MapLocation(SaveBackendId.Pastebin, mapLocationString, "Shared map"));
                }
                else
                {
                    await _mapHandler.LoadMapAsync(new Uri(mapLocationString));
                }

                if (_mapHandler.Map != null)
                {
                    return;
                }
            }
            else if (Application.isEditor || Debug.isDebugBuild)
            {
                if (debugProperties != null && debugProperties.SelectedTestMap == DebugProperties.TestMap.AssetZoo)
                {
                    new AssetZooMapGenerator(_mapHandler).Generate();
                    // test maps skip the home screen for fast iteration
                    _homeScreenPresenter.HideHomeScreen();
                    return;
                }

                if (debugProperties != null && !string.IsNullOrEmpty(debugProperties.TestMapPath))
                {
                    await _mapHandler.LoadMapAsync(new Uri(debugProperties.TestMapPath));
                    if (_mapHandler.Map != null)
                    {
                        // test maps skip the home screen for fast iteration
                        _homeScreenPresenter.HideHomeScreen();
                        return;
                    }
                }

                // no test map set: fall through to the release flow (home screen)
            }

            await _saveCoordinator.NewMapAsync();
            _homeScreenPresenter.ShowHomeScreen(false);
        }
    }
}
