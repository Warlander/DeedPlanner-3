using System;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Domain.Entities.Walls;
using Warlander.Deedplanner.Ui.Widgets;

namespace Warlander.Deedplanner.Editing
{
    public class WallUpdaterView : MonoBehaviour, IWallUpdaterView
    {
        [SerializeField] private UnityTree _wallsTree;

        [SerializeField] private Toggle _reverseToggle;
        [SerializeField] private Toggle _automaticReverseToggle;

        public event Action<WallData> WallSelected;
        public event Action<bool> ReverseChanged;
        public event Action<bool> AutomaticReverseChanged;

        private void Awake()
        {
            _wallsTree.ValueChanged += OnWallsTreeValueChanged;
            _reverseToggle.onValueChanged.AddListener(OnReverseChanged);
            _automaticReverseToggle.onValueChanged.AddListener(OnAutomaticReverseChanged);
        }

        public void AddWallEntry(WallData data, string[] category, Sprite sprite)
        {
            IconUnityListElement iconListElement = (IconUnityListElement) _wallsTree.Add(data, category);
            iconListElement.Sprite = sprite;
        }

        public void SetReverseToggles(bool reverse, bool automaticReverse)
        {
            _reverseToggle.SetIsOnWithoutNotify(reverse);
            _automaticReverseToggle.SetIsOnWithoutNotify(automaticReverse);
        }

        public void PushSelection()
        {
            OnWallsTreeValueChanged(_wallsTree.SelectedValue);
        }

        private void OnWallsTreeValueChanged(object value)
        {
            WallSelected?.Invoke(value as WallData);
        }

        private void OnReverseChanged(bool value)
        {
            ReverseChanged?.Invoke(value);
        }

        private void OnAutomaticReverseChanged(bool value)
        {
            AutomaticReverseChanged?.Invoke(value);
        }
    }
}
