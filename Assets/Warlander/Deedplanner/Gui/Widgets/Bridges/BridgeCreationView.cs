using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data.Bridges;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeCreationView : MonoBehaviour, IBridgeCreationView
    {
        [SerializeField] private Transform _toggleRoot;
        [SerializeField] private Transform _typeToggleRoot;
        [SerializeField] private LabeledToggle _togglePrefab;
        [SerializeField] private Button _placeButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _messageText;

        public event Action<BridgeData> SelectedMaterialChanged;
        public event Action<BridgeType?> SelectedTypeChanged;
        public event Action PlaceClicked;
        public event Action CancelClicked;
        public event Action BecameActive;
        public event Action BecameInactive;

        public BridgeData SelectedMaterial
        {
            get
            {
                if (_selectedMaterialIndex >= 0 && _selectedMaterialIndex < _materials.Count)
                {
                    return _materials[_selectedMaterialIndex];
                }

                return null;
            }
        }

        public BridgeType? SelectedType
        {
            get
            {
                if (_selectedTypeIndex >= 0 && _selectedTypeIndex < _types.Count)
                {
                    return _types[_selectedTypeIndex];
                }

                return null;
            }
        }

        public bool IsActive => gameObject.activeInHierarchy;

        private readonly List<BridgeData> _materials = new List<BridgeData>();
        private readonly List<BridgeType> _types = new List<BridgeType>();
        private readonly List<LabeledToggle> _materialToggles = new List<LabeledToggle>();
        private readonly List<LabeledToggle> _typeToggles = new List<LabeledToggle>();
        private int _selectedMaterialIndex = -1;
        private int _selectedTypeIndex = -1;

        private void Awake()
        {
            _placeButton.onClick.AddListener(OnPlaceClicked);
            _cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnDestroy()
        {
            _placeButton.onClick.RemoveListener(OnPlaceClicked);
            _cancelButton.onClick.RemoveListener(OnCancelClicked);

            ClearToggles(_materialToggles);
            ClearToggles(_typeToggles);
        }

        private void OnEnable()
        {
            BecameActive?.Invoke();
        }

        private void OnDisable()
        {
            BecameInactive?.Invoke();
        }

        public void SetMaterials(IReadOnlyList<BridgeData> materials, int selectedIndex = 0)
        {
            ClearToggles(_materialToggles);
            _materials.Clear();
            _selectedMaterialIndex = -1;

            ClearToggles(_typeToggles);
            _types.Clear();
            _selectedTypeIndex = -1;

            if (materials == null || materials.Count == 0)
            {
                return;
            }

            _materials.AddRange(materials);
            ToggleGroup group = GetOrCreateToggleGroup(_toggleRoot);

            for (int i = 0; i < _materials.Count; i++)
            {
                BridgeData material = _materials[i];
                LabeledToggle toggle = Instantiate(_togglePrefab, _toggleRoot);
                toggle.LabelText = FormatMaterialName(material.Name);
                toggle.Group = group;
                toggle.gameObject.SetActive(true);

                int capturedIndex = i;
                toggle.Toggled += isOn => OnMaterialToggleChanged(capturedIndex, isOn);
                _materialToggles.Add(toggle);
            }

            int index = Mathf.Clamp(selectedIndex, 0, _materials.Count - 1);
            _selectedMaterialIndex = index;
            _materialToggles[index].IsOn = true;

            if (!_materialToggles[index].gameObject.activeInHierarchy)
            {
                SelectedMaterialChanged?.Invoke(SelectedMaterial);
            }
        }

        public void SetTypes(IReadOnlyList<BridgeType> types, bool visible, int selectedIndex = 0)
        {
            ClearToggles(_typeToggles);
            _types.Clear();
            _selectedTypeIndex = -1;
            _typeToggleRoot.gameObject.SetActive(visible);

            if (types == null || types.Count == 0)
            {
                return;
            }

            _types.AddRange(types);

            if (_types.Count == 1)
            {
                _selectedTypeIndex = 0;
                SelectedTypeChanged?.Invoke(SelectedType);
                return;
            }

            ToggleGroup group = GetOrCreateToggleGroup(_typeToggleRoot);

            for (int i = 0; i < _types.Count; i++)
            {
                BridgeType type = _types[i];
                LabeledToggle toggle = Instantiate(_togglePrefab, _typeToggleRoot);
                toggle.LabelText = type.ToString();
                toggle.Group = group;
                toggle.gameObject.SetActive(true);

                int capturedIndex = i;
                toggle.Toggled += isOn => OnTypeToggleChanged(capturedIndex, isOn);
                _typeToggles.Add(toggle);
            }

            int index = Mathf.Clamp(selectedIndex, 0, _types.Count - 1);
            _selectedTypeIndex = index;
            _typeToggles[index].IsOn = true;

            if (!_typeToggles[index].gameObject.activeInHierarchy)
            {
                SelectedTypeChanged?.Invoke(SelectedType);
            }
        }

        public void SetPlaceButtonVisible(bool visible)
        {
            _placeButton.gameObject.SetActive(visible);
        }

        public void SetCancelButtonVisible(bool visible)
        {
            _cancelButton.gameObject.SetActive(visible);
        }

        public void SetMessage(string message)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }
        }

        private void OnMaterialToggleChanged(int index, bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _selectedMaterialIndex = index;
            SelectedMaterialChanged?.Invoke(SelectedMaterial);
        }

        private void OnTypeToggleChanged(int index, bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _selectedTypeIndex = index;
            SelectedTypeChanged?.Invoke(SelectedType);
        }

        private void OnPlaceClicked()
        {
            PlaceClicked?.Invoke();
        }

        private void OnCancelClicked()
        {
            CancelClicked?.Invoke();
        }

        private void ClearToggles(List<LabeledToggle> toggles)
        {
            foreach (LabeledToggle toggle in toggles)
            {
                if (toggle != null)
                {
                    Destroy(toggle.gameObject);
                }
            }

            toggles.Clear();
        }

        private ToggleGroup GetOrCreateToggleGroup(Transform root)
        {
            ToggleGroup group = root.GetComponent<ToggleGroup>();
            if (group == null)
            {
                group = root.gameObject.AddComponent<ToggleGroup>();
            }

            return group;
        }

        private string FormatMaterialName(string name)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
        }
    }
}
