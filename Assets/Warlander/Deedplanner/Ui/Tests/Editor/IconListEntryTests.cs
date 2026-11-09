using System.Reflection;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Warlander.Deedplanner.Ui.Widgets;

namespace Warlander.Deedplanner.Ui.Tests
{
    public class IconListEntryTests
    {
        private GameObject _column;
        private IconUnityListElement _entry;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_column);
        }

        [TestCase(289, "Wooden wall", 30, false)]
        [TestCase(278, "Hardwood log double T arched wall with chinking", 50, false)]
        [TestCase(150, "Hardwood log double T arched wall with chinking", 50, true)]
        public void Layout_KeepsThumbnailCenteredAndReservesCheckSpace(int width, string name, int height, bool truncated)
        {
            CreateEntry(width, name);

            TMP_Text label = _entry.GetComponentInChildren<TMP_Text>();
            RectTransform row = (RectTransform)_entry.transform;
            RectTransform thumbnail = (RectTransform)_entry.transform.Find("Preview Well");
            RectTransform check = (RectTransform)_entry.Toggle.graphic.transform.Find("Selection Check");
            Vector3 thumbnailCenter = row.InverseTransformPoint(thumbnail.TransformPoint(thumbnail.rect.center));
            Vector3 labelRight = row.InverseTransformPoint(label.rectTransform.TransformPoint(new Vector3(label.rectTransform.rect.xMax, 0)));
            Vector3 checkLeft = row.InverseTransformPoint(check.TransformPoint(new Vector3(check.rect.xMin, 0)));

            Assert.That(row.rect.height, Is.EqualTo(height).Within(0.01f));
            Assert.That(thumbnailCenter.y, Is.EqualTo(row.rect.center.y).Within(0.01f));
            Assert.That(thumbnailCenter.x - thumbnail.rect.width / 2 - row.rect.xMin, Is.EqualTo(3).Within(0.01f));
            Assert.That(checkLeft.x - labelRight.x, Is.EqualTo(8).Within(0.01f));
            Assert.That(label.fontSize, Is.EqualTo(17.9f).Within(0.01f));
            Assert.That(label.isTextTruncated, Is.EqualTo(truncated));
            Assert.That(label.font.HasCharacter('\u2026', true), Is.True);
            Assert.That(label.overflowMode, Is.EqualTo(TextOverflowModes.Ellipsis));
            if (truncated)
            {
                Assert.That(label.textInfo.characterInfo.Any(character => character.isVisible && character.character == '\u2026'), Is.True);
            }
        }

        [Test]
        public void SilentSelection_UpdatesTheWholeSelectedLayerAfterReenable()
        {
            CreateEntry(289, "Wooden wall");
            CanvasGroup selection = _entry.Toggle.graphic.GetComponent<CanvasGroup>();
            int changes = 0;
            _entry.Toggle.onValueChanged.AddListener(_ => changes++);

            _entry.Toggle.SetIsOnWithoutNotify(false);
            Tick();
            Assert.That(selection.alpha, Is.Zero);

            _entry.gameObject.SetActive(false);
            _entry.Toggle.SetIsOnWithoutNotify(true);
            _entry.gameObject.SetActive(true);
            Tick();

            Assert.That(selection.alpha, Is.EqualTo(1));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void FocusedTruncatedEntry_RequestsFullNameAndShowsFocusOutline()
        {
            const string name = "Hardwood log double T arched wall with chinking";
            CreateEntry(150, name);
            string requested = null;
            _entry.TooltipRequested += text => requested = text;
            Image outline = _entry.transform.Find("Focus Outline").GetComponent<Image>();

            _entry.OnSelect(new BaseEventData(null));
            Tick();

            Assert.That(requested, Is.EqualTo(name));
            Assert.That(outline.enabled, Is.True);

            requested = null;
            _entry.OnDeselect(new BaseEventData(null));
            Tick();

            Assert.That(requested, Is.Null);
            Assert.That(outline.enabled, Is.False);
        }

        private void CreateEntry(int width, string name)
        {
            _column = new GameObject("Entry Column", typeof(RectTransform), typeof(Canvas), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _column.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _column.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform rect = _column.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 500);
            VerticalLayoutGroup layout = _column.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gui/List Elements/Ground Icon List Element Variant.prefab");
            _entry = Object.Instantiate(prefab, _column.transform).GetComponent<IconUnityListElement>();
            _entry.Value = name;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Tick();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            _entry.GetComponentInChildren<TMP_Text>().ForceMeshUpdate();
            Tick();
        }

        private void Tick()
        {
            typeof(IconUnityListElement).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_entry, null);
        }
    }
}
