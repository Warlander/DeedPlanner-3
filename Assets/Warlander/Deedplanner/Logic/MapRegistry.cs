using System;
using UnityEngine;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic
{
    public class MapRegistry
    {
        public Map CurrentMap { get; private set; }

        public event Action MapInitialized;

        public void SetMap(Map map)
        {
            CurrentMap = map;

            // safety net: destroy any map instances that escaped the normal unload path
            foreach (Map other in UnityEngine.Object.FindObjectsByType<Map>(FindObjectsSortMode.None))
            {
                if (other != map)
                {
                    other.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(other.gameObject);
                }
            }

            MapInitialized?.Invoke();
        }
    }
}
