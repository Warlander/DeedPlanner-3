using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Unitydata
{
    public class UnityTile : MonoBehaviour
    {
        private Tile tile;

        private UnityGround ground;

        public Tile Tile {
            get {
                return tile;
            }
            set {
                tile = value;
                InitializeTile();
            }
        }

        public UnityGround Ground {
            get {
                return ground;
            }
            set {
                ground = value;
            }
        }

        private void InitializeTile()
        {
            if (tile == null)
            {
                return;
            }

            GameObject groundObject = new GameObject("Ground", typeof(UnityGround));
            groundObject.transform.SetParent(transform);
            groundObject.transform.localPosition = Vector3.zero;
            UnityGround unityGround = groundObject.GetComponent<UnityGround>();
            unityGround.Ground = tile.Ground;

        }
    }
}