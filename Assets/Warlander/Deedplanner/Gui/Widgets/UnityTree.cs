using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Widgets
{
    public delegate void UnityTreeValueChangedHandler(object sender, object value);

    public class UnityTree : MonoBehaviour
    {

        public event UnityListValueChangedHandler ValueChanged;

        // we want these fields to be settable via inspector, but not via code
        [SerializeField] private ToggleGroup toggleGroup = null;
        [SerializeField] private RectTransform treeElementsParent = null;
        [SerializeField] private UnityTreeNode treeElementPrefab = null;
        [SerializeField] private UnityListElement listElementPrefab = null;

        [SerializeField] private TMP_InputField searchInputField = null;
        [SerializeField] private GameObject searchBoxRoot = null;
        [SerializeField] private int searchMinimumCharacters = 3;

        public object SelectedValue { get; private set; }

        public UnityListElement SelectedElement => toggleGroup.ActiveToggles().FirstOrDefault().GetComponent<UnityListElement>();

        public bool SearchBoxActive
        {
            get => searchBoxRoot.activeSelf;
            set => searchBoxRoot.SetActive(value);
        }
        
        public object[] Values {
            get {
                return GetComponentsInChildren<UnityListElement>().Select(element => element.Value).ToArray();
            }
        }

        private List<UnityTreeNode> RootBranches {
            get {
                List<UnityTreeNode> elements = new List<UnityTreeNode>();
                foreach (Transform childTransform in treeElementsParent)
                {
                    if (childTransform.gameObject.GetComponent<UnityTreeNode>())
                    {
                        elements.Add(childTransform.gameObject.GetComponent<UnityTreeNode>());
                    }
                }
                return elements;
            }
        }

        /// <summary>
        /// See returns for notes on returned value
        /// </summary>
        /// <param name="value">value to add to the list</param>
        /// <returns>Created list element from prefab (useful if any further modification is needed)</returns>
        public UnityListElement Add(object value, params string[] tree)
        {
            Transform currentParent = treeElementsParent;
            UnityTreeNode node = GetOrCreateNode(currentParent, RootBranches, tree);
            if (node)
            {
                currentParent = node.transform;
            }

            UnityListElement newElement = Instantiate(listElementPrefab, currentParent);
            newElement.Toggle.group = toggleGroup;
            newElement.Value = value;
            newElement.Toggle.onValueChanged.AddListener(toggled =>
            {
                if (toggled)
                {
                    SelectedValue = newElement.Value;
                    ValueChanged?.Invoke(this, newElement.Value);
                }
            });
            bool rootElement = tree == null || tree.Length == 0;
            newElement.gameObject.SetActive(rootElement);

            if (GetComponentsInChildren<UnityListElement>(true).Length == 1)
            {
                SelectedValue = newElement.Value;
                newElement.Toggle.isOn = true;
            }

            return newElement;
        }

        private UnityTreeNode GetOrCreateNode(Transform parent, IEnumerable<UnityTreeNode> nodes, string[] tree)
        {
            if (tree == null || tree.Length == 0)
            {
                return null;
            }

            string currentValue = tree[0];
            string[] newTree = tree.Skip(1).ToArray();
            foreach (UnityTreeNode node in nodes)
            {
                if (node.Value == currentValue)
                {
                    if (tree.Length == 1)
                    {
                        return node;
                    }
                    else
                    {
                        return GetOrCreateNode(node.transform, node.Branches, newTree);
                    }
                }
            }

            UnityTreeNode newNode = Instantiate(treeElementPrefab, parent);
            newNode.Value = currentValue;
            bool root = (parent == treeElementsParent);
            newNode.gameObject.SetActive(root);
            if (tree.Length == 1)
            {
                return newNode;
            }
            else
            {
                return GetOrCreateNode(newNode.transform, newNode.Branches, newTree);
            }
        }

        private void UpdateSearch()
        {
            string text = searchInputField.text;
            text = text.Trim().ToLower();
            // to avoid change spam in tree, any search shorter than number of characters set in inspector will be ignored
            if (text.Length < searchMinimumCharacters)
            {
                text = String.Empty;
            }
            
            foreach (UnityTreeNode treeNode in RootBranches)
            {
                UpdateSearchRecursively(treeNode, text);
            }
        }

        private bool UpdateSearchRecursively(UnityTreeNode currentNode, string text)
        {
            if (text == null)
            {
                return false;
            }
            
            bool selfContainsElement = false;

            foreach (UnityTreeNode unityTreeNode in currentNode.Branches)
            {
                bool childContainsElement = UpdateSearchRecursively(unityTreeNode, text);
                if (childContainsElement)
                {
                    selfContainsElement = true;
                }
            }

            // first check if an child contains given text, to expand or collapse the branch
            foreach (UnityListElement unityListElement in currentNode.Leaves)
            {
                string valueString = unityListElement.Value.ToString().ToLower();
                bool childContainsElement = valueString.Contains(text) && !string.IsNullOrEmpty(text);
                if (childContainsElement)
                {
                    selfContainsElement = true;
                    break;
                }
            }
            
            // only alter the tree structure if search box is not empty to avoid changing tree structure once box is cleared
            if (!string.IsNullOrEmpty(text))
            {
                currentNode.Expanded = selfContainsElement;
            }
            else
            {
                currentNode.Expanded = currentNode.Expanded;
            }
            
            // hide individual leaves for search purposes
            foreach (UnityListElement unityListElement in currentNode.Leaves)
            {
                string valueString = unityListElement.Value.ToString().ToLower();
                bool childContainsElement = valueString.Contains(text) && !string.IsNullOrEmpty(text);
                bool noSearchBranchExpanded = currentNode.Expanded && string.IsNullOrEmpty(text);
                unityListElement.gameObject.SetActive(childContainsElement || noSearchBranchExpanded);
            }

            return selfContainsElement;
        }

        public void OnSearchTextUpdated()
        {
            UpdateSearch();
        }
    }
}
