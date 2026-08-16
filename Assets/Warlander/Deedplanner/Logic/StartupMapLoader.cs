using System;
using System.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Debugging;

namespace Warlander.Deedplanner.Logic
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StartupMapLoader : IInitializable
    {
        private readonly MapHandler _mapHandler;
        private readonly IObjectResolver _resolver;

        public StartupMapLoader(MapHandler mapHandler, IObjectResolver resolver)
        {
            _mapHandler = mapHandler;
            _resolver = resolver;
        }

        public void Initialize()
        {
            LoadMapAsync().ToObservable().Subscribe();
        }

        private async Task LoadMapAsync()
        {
            string mapLocationString = "";
#if UNITY_WEBGL && !UNITY_EDITOR
            mapLocationString = Warlander.Deedplanner.Utils.JavaScriptUtils.GetMapLocationString();
            if (!string.IsNullOrEmpty(mapLocationString))
            {
                mapLocationString = WebLinkUtils.ParseToDirectDownloadLink(mapLocationString);
            }
#endif

            DebugProperties debugProperties = _resolver.ResolveOrDefault<DebugProperties>();

            if (!string.IsNullOrEmpty(mapLocationString))
            {
                await _mapHandler.LoadMapAsync(new Uri(mapLocationString));
            }
            else if ((Application.isEditor || Debug.isDebugBuild) && debugProperties != null)
            {
                await _mapHandler.LoadMapAsync(new Uri(debugProperties.TestMapPath));
            }
            else
            {
                _mapHandler.CreateNewMap(25, 25);
            }
        }
    }
}
