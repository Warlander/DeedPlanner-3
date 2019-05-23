using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    public class HeightUpdater : MonoBehaviour
    {

        private readonly Color neutralColor = Color.white;
        private readonly Color hoveredColor = new Color(0.7f, 0.7f, 0, 1);

        private HeightmapHandle hoveredHandle = null;

        public void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Nothing;
        }

        private void Update()
        {
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;

            Ground ground = raycast.transform ? raycast.transform.GetComponent<Ground>() : null;
            HeightmapHandle heightmapHandle =  raycast.transform ? raycast.transform.GetComponent<HeightmapHandle>() : null;

            if (!heightmapHandle && hoveredHandle)
            {
                hoveredHandle.Color = neutralColor;
                hoveredHandle = null;
            }
            else if (heightmapHandle && heightmapHandle != hoveredHandle)
            {
                if (hoveredHandle)
                {
                    hoveredHandle.Color = neutralColor;
                }
                heightmapHandle.Color = hoveredColor;
                hoveredHandle = heightmapHandle;
            }

            if (raycast.transform == null)
            {
                return;
            }

            if (heightmapHandle)
            {
                
            }
            else if (ground)
            {

            }
        }

    }
}
