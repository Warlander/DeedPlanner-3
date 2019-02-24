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
    public class GroundUpdater : MonoBehaviour
    {

        private GroundData leftClickData;
        private GroundData rightClickData;

        [SerializeField]
        private Image leftClickImage = null;
        [SerializeField]
        private TextMeshProUGUI leftClickText = null;
        [SerializeField]
        private Image rightClickImage = null;
        [SerializeField]
        private TextMeshProUGUI rightClickText = null;

        [SerializeField]
        private Toggle leftClickToggle = null;

        private GroundData LeftClickData {
            get {
                return leftClickData;
            }
            set {
                leftClickData = value;
                leftClickImage.sprite = leftClickData.Tex2d.Sprite;
                leftClickText.text = leftClickData.Name;
            }
        }

        private GroundData RightClickData {
            get {
                return rightClickData;
            }
            set {
                rightClickData = value;
                rightClickImage.sprite = rightClickData.Tex2d.Sprite;
                rightClickText.text = rightClickData.Name;
            }
        }

        public void Start()
        {
            GuiManager.Instance.GroundsTree.ValueChanged += OnGroundsTreeValueChanged;
            LeftClickData = Database.Grounds["gr"];
            RightClickData = Database.Grounds["di"];
        }

        private void OnGroundsTreeValueChanged(object sender, object value)
        {
            bool leftClick = leftClickToggle.isOn;
            GroundData groundData = value as GroundData;
            if (leftClick)
            {
                LeftClickData = groundData;
            }
            else
            {
                RightClickData = groundData;
            }
        }

        public void Tick(RaycastHit raycast)
        {
            if (raycast.transform == null)
            {
                return;
            }

            Ground ground = raycast.transform.GetComponent<Ground>();

            if (Input.GetMouseButton(0))
            {
                ground.Data = leftClickData;
            }
            else if (Input.GetMouseButton(1))
            {
                ground.Data = rightClickData;
            }
        }

    }
}
