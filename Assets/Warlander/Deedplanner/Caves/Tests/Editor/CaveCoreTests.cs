using System;
using System.Xml;
using NUnit.Framework;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using static Warlander.Deedplanner.Caves.Tests.CaveTestData;

namespace Warlander.Deedplanner.Caves.Tests
{
    public class CaveCellTests
    {
        [Test]
        public void ConstructorUsesDp2HeightDefaults()
        {
            CaveData terrain = CreateTerrain("sw", true);

            var cell = new CaveCell(terrain);

            Assert.That(cell.Terrain, Is.SameAs(terrain));
            Assert.That(cell.FloorHeight, Is.EqualTo(5));
            Assert.That(cell.Clearance, Is.EqualTo(30));
            Assert.That(cell.IsSolid, Is.True);
        }

        [Test]
        public void SerializesAndDeserializesRecognizedCaveState()
        {
            Database database = CreateDatabase();
            CaveData floor = CreateTerrain("sfl", false);
            database.AddCave(floor);
            var resolver = new CaveDataResolver(database);
            var source = new CaveCell(floor);
            source.InitializeHeights(17, 42);
            var document = new XmlDocument();
            XmlElement tileElement = document.CreateElement("tile");
            document.AppendChild(tileElement);

            source.Serialize(document, tileElement, resolver);
            var loaded = new CaveCell(database.DefaultCaveData);
            loaded.Deserialize(tileElement, resolver);

            Assert.That(tileElement.GetAttribute("caveHeight"), Is.EqualTo("17"));
            Assert.That(tileElement.GetAttribute("caveSize"), Is.EqualTo("42"));
            Assert.That(tileElement["cave"].GetAttribute("id"), Is.EqualTo("sfl"));
            Assert.That(loaded.Terrain, Is.SameAs(floor));
            Assert.That(loaded.FloorHeight, Is.EqualTo(17));
            Assert.That(loaded.Clearance, Is.EqualTo(42));
        }

        [TestCase("unknown")]
        [TestCase("wcaDoorC")]
        public void DeserializationNormalizesUnsupportedTerrainToStoneWall(string shortName)
        {
            Database database = CreateDatabase();
            if (shortName != "unknown")
            {
                database.AddCave(CreateTerrain(shortName, false, true));
            }
            var document = new XmlDocument();
            document.LoadXml("<tile caveHeight='8' caveSize='25'><cave id='" + shortName + "'/></tile>");
            var cell = new CaveCell(database.DefaultCaveData);

            cell.Deserialize(document.DocumentElement, new CaveDataResolver(database));

            Assert.That(cell.Terrain.ShortName, Is.EqualTo("sw"));
        }

        [Test]
        public void DeserializationUsesDp2DefaultsForMissingCaveFields()
        {
            Database database = CreateDatabase();
            var document = new XmlDocument();
            document.LoadXml("<tile/>");
            var cell = new CaveCell(database.DefaultCaveData);

            cell.Deserialize(document.DocumentElement, new CaveDataResolver(database));

            Assert.That(cell.Terrain.ShortName, Is.EqualTo("sw"));
            Assert.That(cell.FloorHeight, Is.EqualTo(5));
            Assert.That(cell.Clearance, Is.EqualTo(30));
        }

        [Test]
        public void SerializationNormalizesUnsupportedTerrainToStoneWall()
        {
            Database database = CreateDatabase();
            CaveData entrance = CreateTerrain("wcaDoorC", false, true);
            database.AddCave(entrance);
            var document = new XmlDocument();
            XmlElement tileElement = document.CreateElement("tile");
            document.AppendChild(tileElement);
            var cell = new CaveCell(entrance);

            cell.Serialize(document, tileElement, new CaveDataResolver(database));

            Assert.That(tileElement["cave"].GetAttribute("id"), Is.EqualTo("sw"));
        }

        private static Database CreateDatabase()
        {
            var database = new Database();
            database.AddCave(CreateTerrain("sw", true));
            return database;
        }
    }

    public class CaveLevelTests
    {
        [TestCase(-1, 0, 0, 0f)]
        [TestCase(-2, 1, 30, 3f)]
        [TestCase(-6, 5, 150, 15f)]
        public void ConvertsNegativeLevelToCaveStorey(int level, int expectedIndex, int expectedHeight,
            float expectedWorldHeight)
        {
            Assert.That(CaveLevel.GetStoreyIndex(level), Is.EqualTo(expectedIndex));
            Assert.That(CaveLevel.GetHeightOffset(level), Is.EqualTo(expectedHeight));
            Assert.That(CaveLevel.GetWorldHeightOffset(level), Is.EqualTo(expectedWorldHeight));
        }

        [Test]
        public void RejectsSurfaceLevel()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CaveLevel.GetStoreyIndex(0));
        }
    }

    public class CaveDataResolverTests
    {
        [Test]
        public void KeepsRecognizedTerrain()
        {
            var database = CreateDatabase();
            CaveData floor = CreateTerrain("sfl", false);
            database.AddCave(floor);
            var resolver = new CaveDataResolver(database);

            Assert.That(resolver.Resolve("sfl"), Is.SameAs(floor));
        }

        [TestCase("unknown")]
        [TestCase("wcaDoorC")]
        [TestCase("mcaDoorC")]
        [TestCase("scaDoorC")]
        [TestCase("gcaDoorC")]
        public void UsesStoneWallForUnsupportedTerrain(string shortName)
        {
            var database = CreateDatabase();
            if (shortName != "unknown")
            {
                database.AddCave(CreateTerrain(shortName, false, true));
            }
            var resolver = new CaveDataResolver(database);

            Assert.That(resolver.Resolve(shortName).ShortName, Is.EqualTo("sw"));
        }

        private static Database CreateDatabase()
        {
            var database = new Database();
            database.AddCave(CreateTerrain("sw", true));
            return database;
        }
    }

    public class CaveMapTests
    {
        private CaveData _stoneWall;
        private CaveData _stoneFloor;
        private CaveCell[,] _cells;
        private CaveMap _map;

        [SetUp]
        public void SetUp()
        {
            _stoneWall = CreateTerrain("sw", true);
            _stoneFloor = CreateTerrain("sfl", false);
            _cells = new CaveCell[3, 3];
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    _cells[x, y] = new CaveCell(_stoneFloor);
                }
            }

            _map = new CaveMap(2, 2, (x, y) =>
            {
                if (x < 0 || y < 0 || x >= 3 || y >= 3)
                {
                    return null;
                }
                return _cells[x, y];
            }, _stoneWall);
        }

        [Test]
        public void ReadsSharedCornerFloorAndCeiling()
        {
            _cells[1, 1].InitializeHeights(12, 34);

            Assert.That(_map.GetFloorHeight(0, 0, CaveCorner.NorthEast), Is.EqualTo(12));
            Assert.That(_map.GetClearance(0, 0, CaveCorner.NorthEast), Is.EqualTo(34));
            Assert.That(_map.GetCeilingHeight(0, 0, CaveCorner.NorthEast), Is.EqualTo(46));
        }

        [Test]
        public void OutsideMapIsDefaultSolidStone()
        {
            Assert.That(_map.IsBoundarySolid(0, 0, CaveEdge.West), Is.True);
            Assert.That(_map.GetBoundaryTerrain(0, 0, CaveEdge.West), Is.SameAs(_stoneWall));
        }

        [Test]
        public void BoundaryTerrainComesFromSolidNeighbor()
        {
            _cells[1, 0].InitializeTerrain(_stoneWall);

            Assert.That(_map.IsBoundarySolid(0, 0, CaveEdge.East), Is.True);
            Assert.That(_map.GetBoundaryTerrain(0, 0, CaveEdge.East), Is.SameAs(_stoneWall));
        }

        [TestCase(CaveEdge.South)]
        [TestCase(CaveEdge.East)]
        [TestCase(CaveEdge.North)]
        [TestCase(CaveEdge.West)]
        public void TwoZeroClearanceCornersFormEntrance(CaveEdge edge)
        {
            SetEdgeClearance(edge, 0);

            Assert.That(_map.IsEntrance(0, 0, edge), Is.True);
        }

        [Test]
        public void MultipleCollapsedEdgesFormMultipleEntrances()
        {
            _cells[0, 0].InitializeHeights(5, 0);
            _cells[1, 0].InitializeHeights(5, 0);
            _cells[0, 1].InitializeHeights(5, 0);

            Assert.That(_map.IsEntrance(0, 0, CaveEdge.South), Is.True);
            Assert.That(_map.IsEntrance(0, 0, CaveEdge.West), Is.True);
        }

        [Test]
        public void SolidCellCannotHaveEntrance()
        {
            _cells[0, 0].InitializeTerrain(_stoneWall);
            SetEdgeClearance(CaveEdge.South, 0);

            Assert.That(_map.IsEntrance(0, 0, CaveEdge.South), Is.False);
        }

        private void SetEdgeClearance(CaveEdge edge, int clearance)
        {
            switch (edge)
            {
                case CaveEdge.South:
                    _cells[0, 0].InitializeHeights(5, clearance);
                    _cells[1, 0].InitializeHeights(5, clearance);
                    break;
                case CaveEdge.East:
                    _cells[1, 0].InitializeHeights(5, clearance);
                    _cells[1, 1].InitializeHeights(5, clearance);
                    break;
                case CaveEdge.North:
                    _cells[0, 1].InitializeHeights(5, clearance);
                    _cells[1, 1].InitializeHeights(5, clearance);
                    break;
                case CaveEdge.West:
                    _cells[0, 0].InitializeHeights(5, clearance);
                    _cells[0, 1].InitializeHeights(5, clearance);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
            }
        }
    }

    internal static class CaveTestData
    {
        public static CaveData CreateTerrain(string shortName, bool wall, bool entrance = false)
        {
            return new CaveData(null, shortName, shortName, Array.Empty<string[]>(), wall, true, entrance);
        }
    }
}
