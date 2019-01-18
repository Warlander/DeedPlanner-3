using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Unitydata
{
    public class UnityMap : MonoBehaviour
    {
        private Map map;
        private UnityTile[,] tiles;

        public Map Map {
            get {
                return map;
            }
            set {
                PurgeMap();
                map = value;
                InitializeMap();
            }
        }

        private void InitializeMap()
        {
            if (map == null)
            {
                return;
            }

            for (int i = 0; i < map.Width; i++)
            {
                for (int i2 = 0; i2 < map.Height; i2++)
                {
                    GameObject tileObject = new GameObject("Tile " + i + "X" + i2, typeof(UnityTile));
                    tileObject.transform.SetParent(transform);
                    tileObject.transform.localPosition = new Vector3(i * 4, 0, i2 * 4);
                    UnityTile unityTile = tileObject.GetComponent<UnityTile>();
                    unityTile.Tile = map[i, i2];
                }
            }
        }

        private void PurgeMap()
        {
            if (map == null)
            {
                return;
            }

            for (int i = 0; i < map.Width; i++)
            {
                for (int i2 = 0; i2 < map.Height; i2++)
                {
                    Destroy(tiles[i, i2]);
                }
            }
        }
    }
}
