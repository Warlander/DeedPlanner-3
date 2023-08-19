using System;
using System.Collections;
using UnityEngine;
using Warlander.Deedplanner.Debugging;
using Zenject;

namespace Warlander.Deedplanner.Logic
{
    public class StartupMapLoader : MonoBehaviour
    {
        [Inject] private GameManager _gameManager;
        [InjectOptional] private DebugProperties _debugProperties;
        
        private void Awake()
        {
            StartCoroutine(LoadMap());
        }

        private IEnumerator LoadMap()
        {
            string mapLocationString = "";
#if UNITY_WEBGL
            if (Properties.Web)
            {
                mapLocationString = JavaScriptUtils.GetMapLocationString();
                if (!string.IsNullOrEmpty(mapLocationString))
                {
                    mapLocationString = WebLinkUtils.ParseToDirectDownloadLink(mapLocationString);
                }
            }
#endif
            
            if (!string.IsNullOrEmpty(mapLocationString))
            {
                yield return _gameManager.LoadMap(new Uri(mapLocationString));
            }
            else if ((Application.isEditor || Debug.isDebugBuild) && _debugProperties != null)
            {
                yield return _gameManager.LoadMap(new Uri(_debugProperties.TestMapPath));
            }
            else
            {
                _gameManager.CreateNewMap(25, 25);
            }
        }
    }
}