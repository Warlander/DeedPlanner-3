using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Warlander.Deedplanner.Ui.Widgets;

namespace Warlander.Deedplanner.Ui.Tests
{
    public class UnityListTests
    {
        private GameObject _root;
        private UnityList _list;
        private RectTransform _entries;

        [SetUp]
        public void SetUp()
        {
            _root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gui/List.prefab"));
            _list = _root.GetComponentInChildren<UnityList>(true);
            _entries = (RectTransform)typeof(UnityList).GetField("listElementsParent", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_list);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(6)]
        public void ClearRemovesEveryRowImmediately(int count)
        {
            for (int i = 0; i < count; i++) _list.Add("Row " + i);

            _list.Clear();

            Assert.That(_entries.childCount, Is.Zero);
            Assert.That(_list.Values, Is.Empty);
            _list.Clear();
            Assert.That(_entries.childCount, Is.Zero);
        }

        [Test]
        public void ClearAlsoRemovesInactiveRows()
        {
            for (int i = 0; i < 6; i++) _list.Add("Row " + i).gameObject.SetActive(i % 2 == 0);

            _list.Clear();

            Assert.That(_entries.childCount, Is.Zero);
            Assert.That(_entries.GetComponentsInChildren<UnityListElement>(true), Is.Empty);
        }

        [Test]
        public void RepeatedRefreshContainsOnlyCurrentOrderedValuesIncludingDuplicates()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gui/List Elements/Warnings List Element Variant.prefab").GetComponent<UnityListElement>();
            typeof(UnityList).GetField("listElementPrefab", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_list, prefab);
            for (int i = 0; i < 6; i++) _list.Add("Old " + i);
            string[] current = { "Repeated warning", "Repeated warning", "New warning" };
            _list.Clear();
            foreach (string warning in current) _list.Add(warning);
            Assert.That(_list.Values, Is.EqualTo(current));

            _list.Clear();
            _list.Add("Replacement warning");
            Assert.That(_list.Values, Is.EqualTo(new[] { "Replacement warning" }));
        }

        [TestCase("Assets/Prefabs/Gui/Simple List Element.prefab")]
        [TestCase("Assets/Prefabs/Gui/Icon List Element.prefab")]
        public void RepopulatingSelectsNewFirstRowAndStillNotifiesUserSelection(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<UnityListElement>();
            typeof(UnityList).GetField("listElementPrefab", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_list, prefab);
            for (int i = 0; i < 3; i++) _list.Add("Old " + i);
            _list.Clear();

            UnityListElement first = _list.Add("First");
            UnityListElement second = _list.Add("Second");
            Assert.That(_list.SelectedValue, Is.EqualTo("First"));
            Assert.That(_list.SelectedElement, Is.SameAs(first));
            int changes = 0;
            _list.ValueChanged += _ => changes++;

            second.Toggle.isOn = true;

            Assert.That(_list.SelectedValue, Is.EqualTo("Second"));
            Assert.That(_list.SelectedElement, Is.SameAs(second));
            Assert.That(changes, Is.EqualTo(1));
        }
    }
}
