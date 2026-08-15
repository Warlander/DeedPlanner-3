using System;
using System.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Debugging;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic.Saving;

namespace Warlander.Deedplanner.Logic
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StartupMapSelection : IInitializable
    {
        private readonly MapHandler _mapHandler;
        private readonly IObjectResolver _resolver;
        private readonly SaveCoordinator _saveCoordinator;
        private readonly HomeScreenPresenter _homeScreenPresenter;

        public StartupMapSelection(MapHandler mapHandler, IObjectResolver resolver,
            SaveCoordinator saveCoordinator, HomeScreenPresenter homeScreenPresenter)
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
                    await _saveCoordinator.LoadAsync(new MapLocation("pastebin", mapLocationString, "Shared map"));
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
                if (debugProperties != null && !string.IsNullOrEmpty(debugProperties.TestMapPath))
                {
                    await _mapHandler.LoadMapAsync(new Uri(debugProperties.TestMapPath));
                    // editor with a test map skips the home screen for fast iteration
                    return;
                }

                // no test map set: fall through to the release flow (home screen)
            }

            _saveCoordinator.NewMap();
            _homeScreenPresenter.ShowHomeScreen();
        }
    }
}
