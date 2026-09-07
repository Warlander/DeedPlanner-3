using System;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Domain.Entities.Grounds.Tests
{
    public class GroundDataResolverTests
    {
        private Database _database;
        private GroundData _grass;
        private RecordingLogger _logger;
        private GroundDataResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _database = new Database();
            _grass = CreateGround("gr");
            _database.AddGround(_grass);
            _logger = new RecordingLogger();
            _resolver = new GroundDataResolver(_database, _logger);
        }

        [Test]
        public void KeepsRecognizedGroundData()
        {
            GroundData sand = CreateGround("sand");
            _database.AddGround(sand);

            Assert.That(_resolver.Resolve("sand"), Is.SameAs(sand));
            Assert.That(_logger.LastWarning, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        public void UsesDefaultForMissingIdWithoutWarning(string shortName)
        {
            Assert.That(_resolver.Resolve(shortName), Is.SameAs(_grass));
            Assert.That(_logger.LastWarning, Is.Null);
        }

        [Test]
        public void UsesDefaultAndWarnsForUnknownId()
        {
            Assert.That(_resolver.Resolve("unknown"), Is.SameAs(_grass));
            Assert.That(_logger.LastWarning, Is.EqualTo("Unable to load ground unknown, using default instead"));
        }

        private static GroundData CreateGround(string shortName)
        {
            return new GroundData(shortName, shortName, Array.Empty<string[]>(), null, null, false);
        }

        private sealed class RecordingLogger : ICategoryLogger
        {
            public string LastWarning { get; private set; }

            public void Message(string message) { }

            public void Warning(string message)
            {
                LastWarning = message;
            }

            public void Error(string message) { }

            public void Exception(Exception exception) { }

            public void Write(LogType type, string message) { }
        }
    }
}
