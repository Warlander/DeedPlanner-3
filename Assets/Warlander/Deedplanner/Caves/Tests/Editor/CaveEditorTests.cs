using System;
using NUnit.Framework;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Editing;
using static Warlander.Deedplanner.Caves.Tests.CaveTestData;

namespace Warlander.Deedplanner.Caves.Tests
{
    public class CaveEditorTests
    {
        private CaveData _stoneWall;
        private CaveData _reinforcedWall;
        private CaveData _stoneFloor;
        private FakeMutationTarget _target;
        private CaveEditor _editor;

        [SetUp]
        public void SetUp()
        {
            var database = new Database();
            _stoneWall = CreateTerrain("sw", true);
            _reinforcedWall = CreateTerrain("rw", true);
            _stoneFloor = CreateTerrain("sfl", false);
            database.AddCave(_stoneWall);
            database.AddCave(_reinforcedWall);
            database.AddCave(_stoneFloor);
            _target = new FakeMutationTarget(3, 3, _stoneFloor, new CommandManager(100));
            _editor = new CaveEditor(_target, new CaveDataResolver(database));
        }

        [Test]
        public void TerrainStrokeCoalescesTargetsIntoOneUndoStep()
        {
            int mutationCount = 0;
            _target.CommandManager.Mutated += () => mutationCount++;
            using (ICaveEditStroke stroke = _editor.BeginTerrainStroke(
                       _stoneWall, CaveOccupiedCellPolicy.PreserveAndHide))
            {
                Assert.That(stroke.ApplyAt(0, 0), Is.True);
                Assert.That(stroke.ApplyAt(0, 0), Is.False);
                Assert.That(stroke.ApplyAt(1, 0), Is.True);
                Assert.That(mutationCount, Is.Zero);
                stroke.Commit();
            }

            Assert.That(mutationCount, Is.EqualTo(1));
            Assert.That(_target.GetTerrain(0, 0), Is.SameAs(_stoneWall));
            Assert.That(_target.GetTerrain(1, 0), Is.SameAs(_stoneWall));

            _target.CommandManager.Undo();

            Assert.That(_target.GetTerrain(0, 0), Is.SameAs(_stoneFloor));
            Assert.That(_target.GetTerrain(1, 0), Is.SameAs(_stoneFloor));

            _target.CommandManager.Redo();

            Assert.That(_target.GetTerrain(0, 0), Is.SameAs(_stoneWall));
            Assert.That(_target.GetTerrain(1, 0), Is.SameAs(_stoneWall));
        }

        [Test]
        public void CompletedRegionPublishesOnceAfterLiveStroke()
        {
            int liveCount = 0;
            int completedCount = 0;
            CaveDirtyRegion completedRegion = null;
            _editor.Changed += _ => liveCount++;
            _editor.EditCompleted += region =>
            {
                completedCount++;
                completedRegion = region;
            };

            using (ICaveEditStroke stroke = _editor.BeginTerrainStroke(
                       _stoneWall, CaveOccupiedCellPolicy.PreserveAndHide))
            {
                stroke.ApplyAt(0, 0);
                stroke.ApplyAt(2, 2);
                Assert.That(completedCount, Is.Zero);
                stroke.Commit();
            }

            Assert.That(liveCount, Is.EqualTo(2));
            Assert.That(completedCount, Is.EqualTo(1));
            Assert.That(completedRegion.ContainsCell(0, 0), Is.True);
            Assert.That(completedRegion.ContainsCell(2, 2), Is.True);
        }

        [Test]
        public void TerrainChangeDirtiesCellAndCardinalNeighbors()
        {
            CaveDirtyRegion dirtyRegion = null;
            _editor.Changed += region => dirtyRegion = region;

            _editor.SetTerrain(1, 1, _stoneWall, CaveOccupiedCellPolicy.PreserveAndHide);

            Assert.That(dirtyRegion.CellCount, Is.EqualTo(5));
            Assert.That(dirtyRegion.ContainsCell(1, 1), Is.True);
            Assert.That(dirtyRegion.ContainsCell(0, 1), Is.True);
            Assert.That(dirtyRegion.ContainsCell(2, 1), Is.True);
            Assert.That(dirtyRegion.ContainsCell(1, 0), Is.True);
            Assert.That(dirtyRegion.ContainsCell(1, 2), Is.True);
            Assert.That(dirtyRegion.ContainsCell(0, 0), Is.False);
            Assert.That(dirtyRegion.TouchesChunk(2, 0, 1, 3), Is.True);
            Assert.That(dirtyRegion.TouchesChunk(0, 0, 1, 1), Is.False);
        }

        [Test]
        public void VertexChangeDirtiesFourSharingCells()
        {
            CaveDirtyRegion dirtyRegion = null;
            _editor.Changed += region => dirtyRegion = region;

            _editor.SetFloorHeightAtVertex(1, 1, 17);

            Assert.That(dirtyRegion.CellCount, Is.EqualTo(4));
            Assert.That(dirtyRegion.ContainsCell(0, 0), Is.True);
            Assert.That(dirtyRegion.ContainsCell(1, 0), Is.True);
            Assert.That(dirtyRegion.ContainsCell(0, 1), Is.True);
            Assert.That(dirtyRegion.ContainsCell(1, 1), Is.True);
        }

        [Test]
        public void BoundaryVertexDirtyRegionIsClippedToVisibleCells()
        {
            CaveDirtyRegion dirtyRegion = null;
            _editor.Changed += region => dirtyRegion = region;

            _editor.SetClearanceAtVertex(0, 0, 0);

            Assert.That(dirtyRegion.CellCount, Is.EqualTo(1));
            Assert.That(dirtyRegion.ContainsCell(0, 0), Is.True);
        }

        [Test]
        public void TwoCollapsedVerticesCreateEntranceAndUndoRemovesIt()
        {
            var caveMap = new CaveMap(3, 3, _target.GetCell, _stoneWall);
            using (ICaveEditStroke stroke = _editor.BeginClearanceStroke(0))
            {
                stroke.ApplyAt(0, 0);
                stroke.ApplyAt(1, 0);
                stroke.Commit();
            }

            Assert.That(caveMap.IsEntrance(0, 0, CaveEdge.South), Is.True);

            _target.CommandManager.Undo();

            Assert.That(caveMap.IsEntrance(0, 0, CaveEdge.South), Is.False);
        }

        [Test]
        public void PreservePolicyKeepsContentWhenSolidifying()
        {
            _target.SetOccupied(0, 0, true);

            Assert.That(_editor.SetTerrain(0, 0, _stoneWall,
                CaveOccupiedCellPolicy.PreserveAndHide), Is.True);

            Assert.That(_target.HasCaveContent(0, 0), Is.True);
        }

        [Test]
        public void DeletePolicyRemovesContentAndUndoRestoresIt()
        {
            _target.SetOccupied(0, 0, true);

            _editor.SetTerrain(0, 0, _stoneWall, CaveOccupiedCellPolicy.DeleteCellContent);

            Assert.That(_target.HasCaveContent(0, 0), Is.False);
            Assert.That(_target.LastRemoval.RemoveCount, Is.EqualTo(1));

            _target.CommandManager.Undo();

            Assert.That(_target.HasCaveContent(0, 0), Is.True);
            Assert.That(_target.LastRemoval.RestoreCount, Is.EqualTo(1));

            _target.CommandManager.Redo();

            Assert.That(_target.HasCaveContent(0, 0), Is.False);
            Assert.That(_target.LastRemoval.RemoveCount, Is.EqualTo(2));
        }

        [Test]
        public void DeletedContentIsDestroyedWhenAppliedCommandExpires()
        {
            var commandManager = new CommandManager(1);
            _target = new FakeMutationTarget(1, 1, _stoneFloor, commandManager);
            var database = new Database();
            database.AddCave(_stoneWall);
            database.AddCave(_stoneFloor);
            _editor = new CaveEditor(_target, new CaveDataResolver(database));
            _target.SetOccupied(0, 0, true);

            _editor.SetTerrain(0, 0, _stoneWall, CaveOccupiedCellPolicy.DeleteCellContent);
            FakeContentRemoval removal = _target.LastRemoval;
            _editor.SetFloorHeightAtVertex(0, 0, 6);

            Assert.That(removal.DestroyCount, Is.EqualTo(1));
        }

        [Test]
        public void PreventPolicyRejectsOccupiedOpenToSolidChange()
        {
            _target.SetOccupied(0, 0, true);

            bool changed = _editor.SetTerrain(0, 0, _stoneWall,
                CaveOccupiedCellPolicy.PreventSolidifying);

            Assert.That(changed, Is.False);
            Assert.That(_target.GetTerrain(0, 0), Is.SameAs(_stoneFloor));
        }

        [Test]
        public void PreventPolicyAllowsOccupiedSolidTextureChange()
        {
            _target.SetTerrain(0, 0, _stoneWall);
            _target.SetOccupied(0, 0, true);

            bool changed = _editor.SetTerrain(0, 0, _reinforcedWall,
                CaveOccupiedCellPolicy.PreventSolidifying);

            Assert.That(changed, Is.True);
            Assert.That(_target.GetTerrain(0, 0), Is.SameAs(_reinforcedWall));
        }

        [Test]
        public void HeightAndClearanceChangesUndoIndependently()
        {
            _editor.SetFloorHeightAtVertex(1, 1, 17);
            _editor.SetClearanceAtVertex(1, 1, 42);

            Assert.That(_target.GetFloorHeightAtVertex(1, 1), Is.EqualTo(17));
            Assert.That(_target.GetClearanceAtVertex(1, 1), Is.EqualTo(42));

            _target.CommandManager.Undo();

            Assert.That(_target.GetFloorHeightAtVertex(1, 1), Is.EqualTo(17));
            Assert.That(_target.GetClearanceAtVertex(1, 1), Is.EqualTo(CaveCell.DefaultClearance));

            _target.CommandManager.Undo();

            Assert.That(_target.GetFloorHeightAtVertex(1, 1), Is.EqualTo(CaveCell.DefaultFloorHeight));
        }

        [Test]
        public void CancelRestoresStrokeWithoutRecordingUndo()
        {
            using (ICaveEditStroke stroke = _editor.BeginFloorHeightStroke(17))
            {
                stroke.ApplyAt(0, 0);
                stroke.ApplyAt(1, 1);
                stroke.Cancel();
            }

            Assert.That(_target.GetFloorHeightAtVertex(0, 0), Is.EqualTo(CaveCell.DefaultFloorHeight));
            Assert.That(_target.GetFloorHeightAtVertex(1, 1), Is.EqualTo(CaveCell.DefaultFloorHeight));

            _target.CommandManager.Undo();

            Assert.That(_target.GetFloorHeightAtVertex(0, 0), Is.EqualTo(CaveCell.DefaultFloorHeight));
        }

        [Test]
        public void HeightEditKeepsLatestPreviewInOneUndoStep()
        {
            int completedCount = 0;
            _editor.EditCompleted += _ => completedCount++;
            using (ICaveHeightEdit edit = _editor.BeginFloorHeightEdit())
            {
                edit.SetAt(0, 0, 10);
                edit.SetAt(0, 0, 20);
                edit.SetAt(1, 0, 15);
                edit.Commit();
            }

            Assert.That(_target.GetFloorHeightAtVertex(0, 0), Is.EqualTo(20));
            Assert.That(_target.GetFloorHeightAtVertex(1, 0), Is.EqualTo(15));
            Assert.That(completedCount, Is.EqualTo(1));

            _target.CommandManager.Undo();

            Assert.That(_target.GetFloorHeightAtVertex(0, 0), Is.EqualTo(CaveCell.DefaultFloorHeight));
            Assert.That(_target.GetFloorHeightAtVertex(1, 0), Is.EqualTo(CaveCell.DefaultFloorHeight));
        }

        [Test]
        public void FloorHeightEditPreservesClearanceByDefault()
        {
            using (ICaveHeightEdit edit = _editor.BeginFloorHeightEdit())
            {
                edit.SetAt(0, 0, -50);
                edit.Commit();
            }

            Assert.That(_target.GetFloorHeightAtVertex(0, 0), Is.EqualTo(-50));
            Assert.That(_target.GetClearanceAtVertex(0, 0), Is.EqualTo(CaveCell.DefaultClearance));
        }

        [Test]
        public void FloorHeightEditCanPreserveCeilingInOneUndoStep()
        {
            int originalCeiling = CaveCell.DefaultFloorHeight + CaveCell.DefaultClearance;
            using (ICaveHeightEdit edit = _editor.BeginFloorHeightEdit(true))
            {
                edit.SetAt(0, 0, -50);
                edit.SetAt(0, 0, -60);
                edit.Commit();
            }

            Assert.That(_target.GetFloorHeightAtVertex(0, 0), Is.EqualTo(-60));
            Assert.That(_target.GetFloorHeightAtVertex(0, 0) + _target.GetClearanceAtVertex(0, 0),
                Is.EqualTo(originalCeiling));

            _target.CommandManager.Undo();

            Assert.That(_target.GetFloorHeightAtVertex(0, 0), Is.EqualTo(CaveCell.DefaultFloorHeight));
            Assert.That(_target.GetClearanceAtVertex(0, 0), Is.EqualTo(CaveCell.DefaultClearance));
        }

        [Test]
        public void CancelHeightEditRestoresOriginalValues()
        {
            using (ICaveHeightEdit edit = _editor.BeginClearanceEdit())
            {
                edit.SetAt(0, 0, 10);
                edit.SetAt(0, 0, 20);
                edit.Cancel();
            }

            Assert.That(_target.GetClearanceAtVertex(0, 0), Is.EqualTo(CaveCell.DefaultClearance));
        }

        [Test]
        public void HeightEditReturningToOriginalRecordsNothing()
        {
            int mutationCount = 0;
            _target.CommandManager.Mutated += () => mutationCount++;
            using (ICaveHeightEdit edit = _editor.BeginClearanceEdit())
            {
                edit.SetAt(0, 0, 10);
                edit.SetAt(0, 0, CaveCell.DefaultClearance);
                edit.Commit();
            }

            Assert.That(mutationCount, Is.Zero);
            Assert.That(_target.GetClearanceAtVertex(0, 0), Is.EqualTo(CaveCell.DefaultClearance));
        }

        private sealed class FakeMutationTarget : ICaveMutationTarget
        {
            private readonly int _width;
            private readonly int _height;
            private readonly CaveCell[,] _cells;
            private readonly bool[,] _occupied;

            public CommandManager CommandManager { get; }
            public FakeContentRemoval LastRemoval { get; private set; }

            public FakeMutationTarget(int width, int height, CaveData initialTerrain, CommandManager commandManager)
            {
                _width = width;
                _height = height;
                CommandManager = commandManager;
                _cells = new CaveCell[width + 1, height + 1];
                _occupied = new bool[width, height];
                for (int x = 0; x <= width; x++)
                {
                    for (int y = 0; y <= height; y++)
                    {
                        _cells[x, y] = new CaveCell(initialTerrain);
                    }
                }
            }

            public bool ContainsCell(int x, int y)
            {
                return x >= 0 && y >= 0 && x < _width && y < _height;
            }

            public bool ContainsVertex(int x, int y)
            {
                return x >= 0 && y >= 0 && x <= _width && y <= _height;
            }

            public CaveData GetTerrain(int x, int y)
            {
                return _cells[x, y].Terrain;
            }

            public int GetFloorHeightAtVertex(int x, int y)
            {
                return _cells[x, y].FloorHeight;
            }

            public int GetClearanceAtVertex(int x, int y)
            {
                return _cells[x, y].Clearance;
            }

            public bool HasCaveContent(int x, int y)
            {
                return _occupied[x, y];
            }

            public ICaveContentRemoval CreateCaveContentRemoval(int x, int y)
            {
                LastRemoval = new FakeContentRemoval(_occupied, x, y);
                return LastRemoval;
            }

            public void SetTerrain(int x, int y, CaveData terrain)
            {
                _cells[x, y].InitializeTerrain(terrain);
            }

            public void SetFloorHeightAtVertex(int x, int y, int height)
            {
                _cells[x, y].InitializeHeights(height, _cells[x, y].Clearance);
            }

            public void SetClearanceAtVertex(int x, int y, int clearance)
            {
                _cells[x, y].InitializeHeights(_cells[x, y].FloorHeight, clearance);
            }

            public void Record(IReversibleCommand command)
            {
                CommandManager.AddToStack(command);
            }

            public void SetOccupied(int x, int y, bool occupied)
            {
                _occupied[x, y] = occupied;
            }

            public CaveCell GetCell(int x, int y)
            {
                return x < 0 || y < 0 || x > _width || y > _height ? null : _cells[x, y];
            }
        }

        private sealed class FakeContentRemoval : ICaveContentRemoval
        {
            private readonly bool[,] _occupied;
            private readonly int _x;
            private readonly int _y;

            public int RemoveCount { get; private set; }
            public int RestoreCount { get; private set; }
            public int DestroyCount { get; private set; }

            public FakeContentRemoval(bool[,] occupied, int x, int y)
            {
                _occupied = occupied;
                _x = x;
                _y = y;
            }

            public void Remove()
            {
                _occupied[_x, _y] = false;
                RemoveCount++;
            }

            public void Restore()
            {
                _occupied[_x, _y] = true;
                RestoreCount++;
            }

            public void DestroyRemoved()
            {
                DestroyCount++;
            }
        }
    }
}
