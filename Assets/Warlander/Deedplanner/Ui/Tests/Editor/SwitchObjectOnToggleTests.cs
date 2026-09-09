using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Warlander.UI.Utils;

namespace Warlander.Deedplanner.Ui.Tests
{
    public class SwitchObjectOnToggleTests
    {
        [Test]
        public void ReenableSynchronizesStateChangedWithoutNotification()
        {
            GameObject toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            GameObject switchedObject = new GameObject("Selected Orb", typeof(RectTransform));
            switchedObject.transform.SetParent(toggleObject.transform);
            try
            {
                Toggle toggle = toggleObject.GetComponent<Toggle>();
                SwitchObjectOnToggle switcher = switchedObject.AddComponent<SwitchObjectOnToggle>();
                var serializedSwitcher = new SerializedObject(switcher);
                serializedSwitcher.FindProperty("_trackedToggle").objectReferenceValue = toggle;
                serializedSwitcher.ApplyModifiedPropertiesWithoutUndo();

                toggle.SetIsOnWithoutNotify(true);
                InvokeLifecycle(switcher, "Awake");
                Assert.That(switchedObject.activeSelf, Is.True);

                toggleObject.SetActive(false);
                toggle.SetIsOnWithoutNotify(false);
                toggleObject.SetActive(true);
                InvokeLifecycle(switcher, "OnEnable");

                Assert.That(switchedObject.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(toggleObject);
            }
        }

        private static void InvokeLifecycle(SwitchObjectOnToggle switcher, string methodName)
        {
            typeof(SwitchObjectOnToggle).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(switcher, Array.Empty<object>());
        }
    }
}
