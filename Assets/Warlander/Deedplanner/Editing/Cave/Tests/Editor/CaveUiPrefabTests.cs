using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Ui;
using Warlander.Deedplanner.Ui.Widgets;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class CaveUiPrefabTests
    {
        [Test]
        public void CaveToolbeltUsesFourButtonGroundLayoutWithCullingEnabled()
        {
            const string path = "Assets/Prefabs/MainScene/Tabs/Caves Tab.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform tools = root.transform.Find("Toolbelt/Tools Vertical");
                Transform wrapper = tools.Find("Culling Wrapper");
                Toggle offToggle = wrapper.Find("Culling Off Toggle Button").GetComponent<Toggle>();
                Toggle onToggle = wrapper.Find("Culling On Toggle Button").GetComponent<Toggle>();
                Image offIcon = wrapper.Find("Culling Off Toggle Button/Icon").GetComponent<Image>();
                Image onIcon = wrapper.Find("Culling On Toggle Button/Icon").GetComponent<Image>();

                Assert.That(tools.childCount, Is.EqualTo(2));
                Assert.That(wrapper.childCount, Is.EqualTo(2));
                Assert.That(offToggle.group, Is.SameAs(onToggle.group));
                Assert.That(offToggle.isOn, Is.False);
                Assert.That(onToggle.isOn, Is.True);
                Assert.That(AssetDatabase.GetAssetPath(offIcon.sprite),
                    Is.EqualTo("Assets/Graphics/GUI/Icons/034-cave-culling-off.png"));
                Assert.That(AssetDatabase.GetAssetPath(onIcon.sprite),
                    Is.EqualTo("Assets/Graphics/GUI/Icons/035-cave-culling-on.png"));
                var offTooltip = new SerializedObject(offToggle.GetComponent<HoverTooltip>());
                var onTooltip = new SerializedObject(onToggle.GetComponent<HoverTooltip>());
                Assert.That(offTooltip.FindProperty("text").stringValue, Does.Contain("backfaces"));
                Assert.That(onTooltip.FindProperty("text").stringValue, Does.Contain("backfaces"));
                Assert.That(root.transform.Find("Cave Culling Box"), Is.Null);

                var view = new SerializedObject(root.GetComponent<CaveUpdaterView>());
                Assert.That(view.FindProperty("_cullingOffToggle").objectReferenceValue,
                    Is.SameAs(offToggle));
                Assert.That(view.FindProperty("_cullingOnToggle").objectReferenceValue,
                    Is.SameAs(onToggle));

                var caveView = root.GetComponent<CaveUpdaterView>();
                caveView.SetCullingEnabled(false);
                Assert.That(offToggle.isOn, Is.True);
                Assert.That(onToggle.isOn, Is.False);
                caveView.SetCullingEnabled(true);
                Assert.That(offToggle.isOn, Is.False);
                Assert.That(onToggle.isOn, Is.True);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void CaveHeightControlsExposeCeilingPreservationAndClearanceTooltip()
        {
            const string path = "Assets/Prefabs/MainScene/Tabs/Height Tab.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform preserveBox = root.transform.Find("Preserve Cave Ceiling Box");
                Toggle preserveToggle = preserveBox.Find("Preserve Cave Ceiling Checkbox")
                    .GetComponent<Toggle>();
                Transform clearanceToggle = root.transform.Find(
                    "Cave Height Modes Toggle Group/Cave Clearance Toggle");

                Assert.That(preserveToggle.isOn, Is.False);
                var preserveTooltip = new SerializedObject(preserveBox.GetComponent<HoverTooltip>());
                var clearanceTooltip = new SerializedObject(clearanceToggle.GetComponent<HoverTooltip>());
                Assert.That(preserveTooltip.FindProperty("text").stringValue,
                    Does.Contain("absolute ceiling height"));
                Assert.That(clearanceTooltip.FindProperty("text").stringValue,
                    Does.Contain("absolute ceiling heights"));

                var view = new SerializedObject(root.GetComponent<HeightUpdaterView>());
                Assert.That(view.FindProperty("_preserveCaveCeilingToggle").objectReferenceValue,
                    Is.SameAs(preserveToggle));
                Assert.That(view.FindProperty("_preserveCaveCeilingTransform").objectReferenceValue,
                    Is.SameAs(preserveBox));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void CaveLevelButtonsDoNotShowObsoleteUnavailableTooltip()
        {
            const string path = "Assets/Prefabs/MainScene/Height Chooser.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (LevelToggle levelToggle in root.GetComponentsInChildren<LevelToggle>(true))
                {
                    var serialized = new SerializedObject(levelToggle);
                    if (serialized.FindProperty("_level").intValue < 0)
                    {
                        Assert.That(levelToggle.GetComponentsInChildren<HoverTooltip>(true), Is.Empty);
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
