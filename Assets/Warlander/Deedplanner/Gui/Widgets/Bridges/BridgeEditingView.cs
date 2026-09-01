using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Bridges;

namespace Warlander.Deedplanner.Gui.Widgets.Bridges
{
    public class BridgeEditingView : MonoBehaviour, IBridgeEditingView
    {
        [SerializeField] private Button _deleteButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _typeLabel;
        [SerializeField] private GameObject _materialSection;
        [SerializeField] private Transform _materialToggleRoot;
        [SerializeField] private GameObject _extraArgumentSection;
        [SerializeField] private Transform _extraArgumentToggleRoot;
        [SerializeField] private LabeledToggle _togglePrefab;

        public event Action DeleteClicked;
        public event Action CancelClicked;
        public event Action BecameActive;
        public event Action BecameInactive;
        public event Action<BridgeData> SelectedMaterialChanged;
        public event Action<int> SelectedExtraArgumentChanged;

        public bool IsActive => gameObject.activeInHierarchy;

        private readonly List<BridgeData> _materials = new List<BridgeData>();
        private readonly List<LabeledToggle> _materialToggles = new List<LabeledToggle>();
        private int _selectedMaterialIndex = -1;
        private readonly List<int> _extraArguments = new List<int>();
        private readonly List<LabeledToggle> _extraArgumentToggles = new List<LabeledToggle>();
        private int _selectedExtraArgumentIndex = -1;

        private void Awake()
        {
            _deleteButton.onClick.AddListener(OnDeleteClicked);
            _cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnDestroy()
        {
            _deleteButton.onClick.RemoveListener(OnDeleteClicked);
            _cancelButton.onClick.RemoveListener(OnCancelClicked);

            ClearToggles(_materialToggles);
            ClearToggles(_extraArgumentToggles);
        }

        private void OnEnable()
        {
            BecameActive?.Invoke();
        }

        private void OnDisable()
        {
            BecameInactive?.Invoke();
        }

        public void SetDeleteButtonVisible(bool visible)
        {
            _deleteButton.gameObject.SetActive(visible);
        }

        public void SetCancelButtonVisible(bool visible)
        {
            _cancelButton.gameObject.SetActive(visible);
        }

        public void SetTypeLabel(string text)
        {
            if (_typeLabel != null)
            {
                _typeLabel.text = text;
            }
        }

        public void SetMaterialsVisible(bool visible)
        {
            if (_materialSection != null)
            {
                _materialSection.SetActive(visible);
            }
        }

        public void SetMaterials(IReadOnlyList<BridgeData> materials, int selectedIndex)
        {
            ClearToggles(_materialToggles);
            _materials.Clear();
            _selectedMaterialIndex = -1;

            if (materials == null || materials.Count == 0)
            {
                return;
            }

            _materials.AddRange(materials);
            ToggleGroup group = GetOrCreateToggleGroup(_materialToggleRoot);

            for (int i = 0; i < _materials.Count; i++)
            {
                BridgeData material = _materials[i];
                LabeledToggle toggle = Instantiate(_togglePrefab, _materialToggleRoot);
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
        }

        private void OnMaterialToggleChanged(int index, bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _selectedMaterialIndex = index;
            SelectedMaterialChanged?.Invoke(_materials[index]);
        }

        public void SetExtraArgumentsVisible(bool visible)
        {
            if (_extraArgumentSection != null)
            {
                _extraArgumentSection.SetActive(visible);
            }
        }

        public void SetExtraArguments(IReadOnlyList<int> values, int selectedIndex)
        {
            ClearToggles(_extraArgumentToggles);
            _extraArguments.Clear();
            _selectedExtraArgumentIndex = -1;

            if (values == null || values.Count == 0)
            {
                return;
            }

            _extraArguments.AddRange(values);
            ToggleGroup group = GetOrCreateToggleGroup(_extraArgumentToggleRoot);

            for (int i = 0; i < _extraArguments.Count; i++)
            {
                int value = _extraArguments[i];
                LabeledToggle toggle = Instantiate(_togglePrefab, _extraArgumentToggleRoot);
                toggle.LabelText = value.ToString();
                toggle.Group = group;
                toggle.gameObject.SetActive(true);

                int capturedIndex = i;
                toggle.Toggled += isOn => OnExtraArgumentToggleChanged(capturedIndex, isOn);
                _extraArgumentToggles.Add(toggle);
            }

            int index = Mathf.Clamp(selectedIndex, 0, _extraArguments.Count - 1);
            _selectedExtraArgumentIndex = index;
            _extraArgumentToggles[index].IsOn = true;
        }

        private void OnExtraArgumentToggleChanged(int index, bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _selectedExtraArgumentIndex = index;
            SelectedExtraArgumentChanged?.Invoke(_extraArguments[index]);
        }

        private void OnDeleteClicked()
        {
            DeleteClicked?.Invoke();
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
