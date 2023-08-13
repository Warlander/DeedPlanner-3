using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Inputs;
using Zenject;

namespace Warlander.Deedplanner.Gui
{
    public class GuiManager : MonoBehaviour
    {
        public static GuiManager Instance { get; private set; }

        [Inject] private DPInput _input;
        
        [SerializeField] private RectTransform[] interfaceTransforms = null;

        [SerializeField] private UnityTree groundsTree = null;
        [SerializeField] private UnityTree cavesTree = null;
        [SerializeField] private UnityTree floorsTree = null;
        [SerializeField] private UnityTree wallsTree = null;
        [SerializeField] private UnityList roofsList = null;
        [SerializeField] private UnityTree objectsTree = null;

        public UnityTree GroundsTree => groundsTree;
        public UnityTree CavesTree => cavesTree;
        public UnityTree FloorsTree => floorsTree;
        public UnityTree WallsTree => wallsTree;
        public UnityList RoofsList => roofsList;
        public UnityTree ObjectsTree => objectsTree;

        public GuiManager()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            _input.EditingControls.ToggleUI.performed += ToggleUIOnperformed;
        }

        private void ToggleUIOnperformed(InputAction.CallbackContext obj)
        {
            foreach (RectTransform interfaceTransform in interfaceTransforms)
            {
                interfaceTransform.gameObject.SetActive(!interfaceTransform.gameObject.activeSelf);
            }
        }
    }
}