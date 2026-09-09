using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Editing;
using Warlander.UI.Utils;

namespace Warlander.Deedplanner.Ui.Tests
{
    public class TabSelectionViewTests
    {
        [Test]
        public void ProgrammaticSelectionUpdatesHiddenToggleVisualWithoutRaisingTabSelected()
        {
            GameObject root = new GameObject("Tabs", typeof(RectTransform),
                typeof(ObservableToggleGroup), typeof(TabSelectionView));
            try
            {
                ObservableToggleGroup group = root.GetComponent<ObservableToggleGroup>();
                TabSelectionView view = root.GetComponent<TabSelectionView>();
                Toggle groundToggle = CreateToggle(root.transform, "Ground", Tab.Ground, group, true);
                Toggle cavesToggle = CreateToggle(root.transform, "Caves", Tab.Caves, group, false);
                GameObject cavesOrb = CreateSelectedOrb(cavesToggle);
                cavesToggle.gameObject.SetActive(false);

                var serializedView = new SerializedObject(view);
                serializedView.FindProperty("_tabToggleGroup").objectReferenceValue = group;
                serializedView.ApplyModifiedPropertiesWithoutUndo();

                InvokeLifecycle(group, "Start");
                InvokeLifecycle(view, "Start");
                Tab selectedTab = Tab.Ground;
                view.TabSelected += tab => selectedTab = tab;

                view.SelectTab(Tab.Caves);
                cavesToggle.gameObject.SetActive(true);

                Assert.That(groundToggle.isOn, Is.False);
                Assert.That(cavesToggle.isOn, Is.True);
                Assert.That(cavesOrb.activeInHierarchy, Is.True);
                Assert.That(selectedTab, Is.EqualTo(Tab.Ground));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Toggle CreateToggle(Transform parent, string name, Tab tab,
            ToggleGroup group, bool isOn)
        {
            GameObject toggleObject = new GameObject(name, typeof(RectTransform),
                typeof(Toggle), typeof(TabReference));
            toggleObject.transform.SetParent(parent);
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.group = group;
            toggle.isOn = isOn;
            toggleObject.GetComponent<TabReference>().Tab = tab;
            return toggle;
        }

        private static GameObject CreateSelectedOrb(Toggle toggle)
        {
            GameObject orb = new GameObject("SelectedOrb", typeof(RectTransform));
            orb.transform.SetParent(toggle.transform);
            SwitchObjectOnToggle switcher = orb.AddComponent<SwitchObjectOnToggle>();
            var serializedSwitcher = new SerializedObject(switcher);
            serializedSwitcher.FindProperty("_trackedToggle").objectReferenceValue = toggle;
            serializedSwitcher.ApplyModifiedPropertiesWithoutUndo();
            InvokeLifecycle(switcher, "Awake");
            return orb;
        }

        private static void InvokeLifecycle(object target, string methodName)
        {
            target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(target, Array.Empty<object>());
        }
    }
}
