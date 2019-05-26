using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui
{
    public delegate void UnityTreeValueChangedHandler(object sender, object value);

    public class UnityTree : MonoBehaviour
    {

        public event UnityListValueChangedHandler ValueChanged;

        // we want these fields to be settable via inspector, but not via code
        [SerializeField]
        private ToggleGroup toggleGroup = null;
        [SerializeField]
        private RectTransform treeElementsParent = null;
        [SerializeField]
        private UnityTreeNode treeElementPrefab = null;
        [SerializeField]
        private UnityListElement listElementPrefab = null;

        public object SelectedValue { get; private set; }

        public UnityListElement SelectedElement {
            get {
                return toggleGroup.ActiveToggles().FirstOrDefault().GetComponent<UnityListElement>();
            }
        }

        public object[] Values {
            get {
                return GetComponentsInChildren<UnityListElement>().Select((element) => element.Value).ToArray();
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
            if (node != null)
            {
                currentParent = node.transform;
            }

            UnityListElement newElement = Instantiate(listElementPrefab);
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
            newElement.transform.SetParent(currentParent);
            bool rootElement = tree == null || tree.Length == 0;
            newElement.gameObject.SetActive(rootElement);

            if (GetComponentsInChildren<UnityListElement>(true).Length == 1)
            {
                SelectedValue = newElement.Value;
                newElement.Toggle.isOn = true;
            }

            return newElement;
        }

        private UnityTreeNode GetOrCreateNode(Transform parent, List<UnityTreeNode> nodes, string[] tree)
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

            UnityTreeNode newNode = Instantiate(treeElementPrefab);
            newNode.Value = currentValue;
            newNode.transform.SetParent(parent);
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

    }
}
