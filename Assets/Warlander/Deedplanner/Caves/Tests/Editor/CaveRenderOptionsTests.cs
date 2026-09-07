using NUnit.Framework;

namespace Warlander.Deedplanner.Caves.Tests
{
    public class CaveRenderOptionsTests
    {
        [Test]
        public void CullingIsEnabledByDefault()
        {
            var options = new CaveRenderOptions();

            Assert.That(options.IsCullingEnabled(), Is.True);
        }

        [Test]
        public void CullingChangePublishesOnlyForNewValue()
        {
            var options = new CaveRenderOptions();
            int changes = 0;
            options.Changed += () => changes++;

            options.SetCullingEnabled(true);
            options.SetCullingEnabled(false);

            Assert.That(options.IsCullingEnabled(), Is.False);
            Assert.That(changes, Is.EqualTo(1));
        }
    }
}
