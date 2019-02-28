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

        private bool editCorners = true;

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

        public bool EditCorners {
            get {
                return editCorners;
            }
            set {
                editCorners = value;
                UpdateSelectionMode();
            }
        }

        public void Start()
        {
            GuiManager.Instance.GroundsTree.ValueChanged += OnGroundsTreeValueChanged;
            LeftClickData = Database.Grounds["gr"];
            RightClickData = Database.Grounds["di"];
        }

        public void OnEnable()
        {
            UpdateSelectionMode();
        }

        private void UpdateSelectionMode()
        {
            if (editCorners)
            {
                LayoutManager.Instance.TileSelectionMode = TileSelectionMode.TilesAndCorners;
            }
            else
            {
                LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Tiles;
            }
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

        private void Update()
        {
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;
            if (raycast.transform == null)
            {
                return;
            }

            Ground ground = raycast.transform.GetComponent<Ground>();

            if (Input.GetMouseButton(0))
            {
                if (editCorners && leftClickData.Diagonal)
                {

                }
                else
                {
                    //ground.RoadDirection = RoadDirection.Center;
                }
                ground.Data = leftClickData;
            }
            else if (Input.GetMouseButton(1))
            {
                ground.Data = rightClickData;
            }
        }

    }
}
