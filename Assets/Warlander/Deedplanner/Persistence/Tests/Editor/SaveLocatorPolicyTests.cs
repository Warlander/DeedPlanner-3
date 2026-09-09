using System;
using NUnit.Framework;

namespace Warlander.Deedplanner.Persistence.Tests
{
    public class SaveLocatorPolicyTests
    {
        private readonly SaveNameSanitizer _nameSanitizer = new SaveNameSanitizer();

        [Test]
        public void GetAvailable_NewNameReturnsSanitizedLocator()
        {
            string locator = SaveLocatorPolicy.GetAvailable("A:B", _nameSanitizer, _ => false);

            Assert.That(locator, Is.EqualTo("A_B.MAP"));
        }

        [Test]
        public void GetAvailable_SanitizedNameCollisionIsRejected()
        {
            string existingLocator = SaveLocatorPolicy.GetAvailable("A:B", _nameSanitizer, _ => false);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                SaveLocatorPolicy.GetAvailable("A/B", _nameSanitizer, locator => locator == existingLocator));

            Assert.That(exception.Message, Does.Contain("A/B"));
        }
    }
}
