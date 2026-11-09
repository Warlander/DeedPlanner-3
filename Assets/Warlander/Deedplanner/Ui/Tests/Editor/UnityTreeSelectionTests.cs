using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Warlander.Deedplanner.Ui.Widgets;

namespace Warlander.Deedplanner.Ui.Tests
{
    public class UnityTreeSelectionTests
    {
        private GameObject _treeObject;
        private UnityTree _tree;
        private UnityListElement _grass;
        private UnityListElement _dirt;
        private int _valueChanges;
        private int _toggleChanges;

        [SetUp]
        public void SetUp()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gui/Tree.prefab");
            _treeObject = Object.Instantiate(prefab);
            _tree = _treeObject.GetComponentInChildren<UnityTree>(true);
            _grass = _tree.Add("Grass");
            _dirt = _tree.Add("Dirt");
            _tree.SelectValue("Grass");
            _tree.ValueChanged += _ => _valueChanges++;
            _grass.Toggle.onValueChanged.AddListener(_ => _toggleChanges++);
            _dirt.Toggle.onValueChanged.AddListener(_ => _toggleChanges++);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_treeObject);
        }

        [Test]
        public void SelectValue_ChangesHighlightWithoutSelectionCallbacks()
        {
            _tree.SelectValue("Dirt");

            Assert.That(_tree.SelectedValue, Is.EqualTo("Dirt"));
            Assert.That(_dirt.Toggle.isOn, Is.True);
            Assert.That(_grass.Toggle.isOn, Is.False);
            Assert.That(_valueChanges, Is.Zero);
            Assert.That(_toggleChanges, Is.Zero);
        }

        [Test]
        public void SelectValue_SwitchesToHiddenEntryAndBackWithoutLeavingTwoSelections()
        {
            _dirt.gameObject.SetActive(false);

            _tree.SelectValue("Dirt");

            Assert.That(_tree.SelectedValue, Is.EqualTo("Dirt"));
            Assert.That(_dirt.Toggle.isOn, Is.True);
            Assert.That(_grass.Toggle.isOn, Is.False);

            _tree.SelectValue("Grass");
            _dirt.gameObject.SetActive(true);

            Assert.That(_tree.SelectedValue, Is.EqualTo("Grass"));
            Assert.That(_grass.Toggle.isOn, Is.True);
            Assert.That(_dirt.Toggle.isOn, Is.False);
            Assert.That(_valueChanges, Is.Zero);
            Assert.That(_toggleChanges, Is.Zero);
        }

        [Test]
        public void UserSelection_AfterSilentSelectionStillNotifies()
        {
            _tree.SelectValue("Dirt");

            _grass.Toggle.isOn = true;

            Assert.That(_tree.SelectedValue, Is.EqualTo("Grass"));
            Assert.That(_grass.Toggle.isOn, Is.True);
            Assert.That(_dirt.Toggle.isOn, Is.False);
            Assert.That(_valueChanges, Is.EqualTo(1));
        }
    }
}
