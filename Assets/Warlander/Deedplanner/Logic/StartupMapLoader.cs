using System;
using System.Threading.Tasks;
using R3;
using UnityEngine;
using Warlander.Deedplanner.Debugging;
using Zenject;

namespace Warlander.Deedplanner.Logic
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StartupMapLoader : IInitializable
    {
        [Inject] private MapHandler _mapHandler;
        [InjectOptional] private DebugProperties _debugProperties;
        
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

            if (!string.IsNullOrEmpty(mapLocationString))
            {
                await _mapHandler.LoadMapAsync(new Uri(mapLocationString));
            }
            else if ((Application.isEditor || Debug.isDebugBuild) && _debugProperties != null)
            {
                await _mapHandler.LoadMapAsync(new Uri(_debugProperties.TestMapPath));
            }
            else
            {
                _mapHandler.CreateNewMap(25, 25);
            }
        }
    }
}