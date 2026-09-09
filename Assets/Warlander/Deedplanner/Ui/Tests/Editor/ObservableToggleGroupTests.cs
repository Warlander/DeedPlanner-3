using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Ui.Tests
{
    public class ObservableToggleGroupTests
    {
        [Test]
        public void InactiveToggleAtStartupRaisesActiveToggleChangedAfterActivation()
        {
            GameObject root = new GameObject("Toggle Group", typeof(RectTransform), typeof(ObservableToggleGroup));
            try
            {
                ObservableToggleGroup group = root.GetComponent<ObservableToggleGroup>();
                Toggle activeToggle = CreateToggle(root.transform, "Active", group, true);

                GameObject hiddenObject = new GameObject("Initially Hidden", typeof(RectTransform));
                hiddenObject.SetActive(false);
                hiddenObject.transform.SetParent(root.transform);
                Toggle hiddenToggle = hiddenObject.AddComponent<Toggle>();
                hiddenToggle.group = group;

                Toggle selectedToggle = null;
                group.ActiveToggleChanged += toggle => selectedToggle = toggle;
                typeof(ObservableToggleGroup).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(group, Array.Empty<object>());

                hiddenObject.SetActive(true);
                hiddenToggle.isOn = true;

                Assert.That(activeToggle.isOn, Is.False);
                Assert.That(selectedToggle, Is.SameAs(hiddenToggle));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Toggle CreateToggle(Transform parent, string name, ToggleGroup group, bool isOn)
        {
            GameObject toggleObject = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            toggleObject.transform.SetParent(parent);
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.group = group;
            toggle.isOn = isOn;
            return toggle;
        }
    }
}
