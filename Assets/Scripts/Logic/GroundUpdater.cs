using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    public static class GroundUpdater
    {

        public static void Update(RaycastHit raycast)
        {
            if (raycast.transform == null)
            {
                Debug.Log("Empty raycast");
                return;
            }

            Debug.Log("Updating ground");
            GroundData groundData = GuiManager.Instance.GroundsTree.SelectedValue as GroundData;
            Ground ground = raycast.transform.GetComponent<Ground>();

            if (Input.GetMouseButton(0))
            {
                ground.Data = groundData;
            }
        }

    }
}
