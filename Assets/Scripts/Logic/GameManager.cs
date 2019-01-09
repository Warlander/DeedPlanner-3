using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{

    public class GameManager : MonoBehaviour
    {

        public static GameManager Instance { get; private set; }

        private Tab currentTab;

        public Tab CurrentTab {
            get {
                return currentTab;
            }
            set {
                currentTab = value;

            }
        }

        private void Awake()
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
            
        }

        private void Update()
        {

        }

        public void OnTabChange(TabReference tabReference)
        {
            Tab tab = tabReference.Tab;
            CurrentTab = tab;
        }
    }

}