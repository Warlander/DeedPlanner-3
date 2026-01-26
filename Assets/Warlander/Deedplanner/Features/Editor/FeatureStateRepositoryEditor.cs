using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Warlander.Deedplanner.Features.Editor
{
    [CustomEditor(typeof(FeatureStateRepository))]
    public class FeatureStateRepositoryEditor : UnityEditor.Editor
    {
        private FeatureStateTreeView _treeView;
        private TreeViewState<int> _treeViewState;

        private void OnEnable()
        {
            SyncFeatureStates();
            _treeViewState = new TreeViewState<int>();
            _treeView = new FeatureStateTreeView(_treeViewState, serializedObject.FindProperty("featureStates"));
        }

        private void SyncFeatureStates()
        {
            SerializedProperty featureStatesProperty = serializedObject.FindProperty("featureStates");
            System.Array enumValues = System.Enum.GetValues(typeof(Feature));
            
            bool changed = false;
            
            // Add missing features
            foreach (Feature feature in enumValues)
            {
                bool found = false;
                for (int i = 0; i < featureStatesProperty.arraySize; i++)
                {
                    SerializedProperty element = featureStatesProperty.GetArrayElementAtIndex(i);
                    if ((Feature)element.FindPropertyRelative("_feature").enumValueIndex == feature)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    int index = featureStatesProperty.arraySize;
                    featureStatesProperty.InsertArrayElementAtIndex(index);
                    SerializedProperty element = featureStatesProperty.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("_feature").enumValueIndex = (int)feature;
                    element.FindPropertyRelative("_enabledInProduction").boolValue = false;
                    element.FindPropertyRelative("_enabledInDebug").boolValue = false;
                    element.FindPropertyRelative("_enabledInEditor").boolValue = false;
                    changed = true;
                }
            }
            
            // Remove extra features (optional, but cleaner)
            for (int i = featureStatesProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty element = featureStatesProperty.GetArrayElementAtIndex(i);
                Feature feature = (Feature)element.FindPropertyRelative("_feature").enumValueIndex;
                bool found = false;
                foreach (Feature enumFeature in enumValues)
                {
                    if (feature == enumFeature)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    featureStatesProperty.DeleteArrayElementAtIndex(i);
                    changed = true;
                }
            }

            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }

        public override void OnInspectorGUI()
        {
            if (_treeView == null)
            {
                OnEnable();
            }

            serializedObject.Update();

            Rect rect = EditorGUILayout.GetControlRect(false, 200);
            _treeView.OnGUI(rect);

            serializedObject.ApplyModifiedProperties();
        }
    }

    public class FeatureStateTreeView : TreeView<int>
    {
        private SerializedProperty _featureStatesProperty;

        public FeatureStateTreeView(TreeViewState<int> state, SerializedProperty featureStatesProperty) : base(state, CreateHeader())
        {
            _featureStatesProperty = featureStatesProperty;
            showBorder = true;
            Reload();
        }

        private static MultiColumnHeader CreateHeader()
        {
            MultiColumnHeaderState.Column[] columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Feature"),
                    width = 150,
                    minWidth = 100,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Prod"),
                    width = 50,
                    minWidth = 40,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Debug"),
                    width = 50,
                    minWidth = 40,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Editor"),
                    width = 50,
                    minWidth = 40,
                    autoResize = false
                }
            };

            return new MultiColumnHeader(new MultiColumnHeaderState(columns));
        }

        protected override TreeViewItem<int> BuildRoot()
        {
            TreeViewItem<int> root = new TreeViewItem<int> { id = 0, depth = -1, displayName = "Root" };
            root.children = new List<TreeViewItem<int>>();

            for (int i = 0; i < _featureStatesProperty.arraySize; i++)
            {
                SerializedProperty stateProp = _featureStatesProperty.GetArrayElementAtIndex(i);
                SerializedProperty featureProp = stateProp.FindPropertyRelative("_feature");
                string featureName = ((Feature)featureProp.enumValueIndex).ToString();
                root.AddChild(new TreeViewItem<int> { id = i + 1, depth = 0, displayName = featureName });
            }

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                Rect rect = args.GetCellRect(i);
                int columnIndex = args.GetColumn(i);

                SerializedProperty stateProp = _featureStatesProperty.GetArrayElementAtIndex(args.item.id - 1);

                if (columnIndex == 0)
                {
                    EditorGUI.LabelField(rect, args.item.displayName);
                }
                else if (columnIndex == 1)
                {
                    SerializedProperty prop = stateProp.FindPropertyRelative("_enabledInProduction");
                    prop.boolValue = EditorGUI.Toggle(rect, prop.boolValue);
                }
                else if (columnIndex == 2)
                {
                    SerializedProperty prop = stateProp.FindPropertyRelative("_enabledInDebug");
                    prop.boolValue = EditorGUI.Toggle(rect, prop.boolValue);
                }
                else if (columnIndex == 3)
                {
                    SerializedProperty prop = stateProp.FindPropertyRelative("_enabledInEditor");
                    prop.boolValue = EditorGUI.Toggle(rect, prop.boolValue);
                }
            }
        }
    }
}