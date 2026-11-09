using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace Warlander.Deedplanner.Bridges.Tests
{
    public class BridgeGeometryTests
    {
        [TestCase(true, 1, 1)]
        [TestCase(false, 1, 1)]
        [TestCase(true, 1, 2)]
        [TestCase(false, 1, 2)]
        [TestCase(true, 1, 3)]
        [TestCase(false, 1, 3)]
        [TestCase(true, 2, 3)]
        [TestCase(false, 2, 3)]
        [TestCase(true, 5, 1)]
        [TestCase(false, 5, 1)]
        [TestCase(true, 5, 3)]
        [TestCase(false, 5, 3)]
        public void PartSides_FollowLanesRegardlessOfLength(bool vertical, int length, int width)
        {
            var bridge = (Bridge)FormatterServices.GetUninitializedObject(typeof(Bridge));
            MethodInfo getPartSide = typeof(Bridge).GetMethod("GetPartSide",
                BindingFlags.Instance | BindingFlags.NonPublic);
            int startX = 4;
            int startY = 7;
            int endX = startX + (vertical ? width : length) - 1;
            int endY = startY + (vertical ? length : width) - 1;

            for (int segment = 0; segment < length; segment++)
            {
                for (int lane = 0; lane < width; lane++)
                {
                    int x = startX + (vertical ? lane : segment);
                    int y = startY + (vertical ? segment : lane);
                    BridgePartSide expected = width == 1 ? BridgePartSide.NARROW
                        : lane == 0 ? BridgePartSide.RIGHT
                        : lane == width - 1 ? BridgePartSide.LEFT : BridgePartSide.CENTER;

                    object actual = getPartSide.Invoke(bridge,
                        new object[] { startX, startY, endX, endY, x, y, vertical });

                    Assert.That(actual, Is.EqualTo(expected), $"Segment {segment}, lane {lane}");
                }
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Width_WhenWiderThanLong_CountsAllLanes(bool vertical)
        {
            var bridge = (Bridge)FormatterServices.GetUninitializedObject(typeof(Bridge));
            typeof(Bridge).GetProperty("Data").SetValue(bridge,
                new BridgeData("Test", 3, 0, null, null, null, false));
            SetField(bridge, "verticalOrientation", vertical);
            SetField(bridge, "firstX", 4);
            SetField(bridge, "firstY", 7);
            SetField(bridge, "secondX", vertical ? 6 : 4);
            SetField(bridge, "secondY", vertical ? 7 : 9);

            MethodInfo getWidth = typeof(Bridge).GetMethod("GetBridgeWidth",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(getWidth.Invoke(bridge, null), Is.EqualTo(3));
        }

        private static void SetField(Bridge bridge, string name, object value)
        {
            typeof(Bridge).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(bridge, value);
        }
    }
}
