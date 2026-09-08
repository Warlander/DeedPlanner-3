using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Warlander.Deedplanner.Ui.Tooltips.Tests
{
    public class TooltipTests
    {
        private static readonly Rect Bounds = new Rect(-512, -384, 1024, 768);

        [Test]
        public void SelectPivotPrefersBottomRightWhenTooltipFits()
        {
            Vector2 pivot = SelectPivot(new Vector2(0, 0), new Vector2(200, 100), new Vector2(0, -20));

            Assert.That(pivot, Is.EqualTo(new Vector2(0, 1)));
        }

        [Test]
        public void SelectPivotFlipsLeftWhenRightEdgeWouldOverflow()
        {
            Vector2 pivot = SelectPivot(new Vector2(313, 0), new Vector2(200, 100), new Vector2(0, -20));

            Assert.That(pivot.x, Is.EqualTo(1));
        }

        [Test]
        public void SelectPivotFlipsAboveWhenBottomEdgeWouldOverflowWithCursorGap()
        {
            Vector2 pivot = SelectPivot(new Vector2(0, -265), new Vector2(200, 100), new Vector2(0, -20));

            Assert.That(pivot.y, Is.EqualTo(0));
        }

        [Test]
        public void SelectPivotKeepsPreferredSidesAtExactBoundaries()
        {
            Vector2 pivot = SelectPivot(new Vector2(312, -264), new Vector2(200, 100), new Vector2(0, -20));

            Assert.That(pivot, Is.EqualTo(new Vector2(0, 1)));
        }

        private static Vector2 SelectPivot(Vector2 focusPosition, Vector2 tooltipSize, Vector2 cursorCorrection)
        {
            MethodInfo method = typeof(Tooltip).GetMethod("SelectPivot",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);

            return (Vector2)method.Invoke(null, new object[] { Bounds, focusPosition, tooltipSize, cursorCorrection });
        }
    }
}
