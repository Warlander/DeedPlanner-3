using System;
using System.Collections.Generic;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Editing;

namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveEditor : ICaveEditor
    {
        private readonly ICaveMutationTarget _target;
        private readonly ICaveDataResolver _dataResolver;

        public event Action<CaveDirtyRegion> Changed = delegate { };
        public event Action<CaveDirtyRegion> EditCompleted = delegate { };

        public CaveEditor(ICaveMutationTarget target, ICaveDataResolver dataResolver)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _dataResolver = dataResolver ?? throw new ArgumentNullException(nameof(dataResolver));
        }

        public bool SetTerrain(int x, int y, CaveData terrain, CaveOccupiedCellPolicy occupiedCellPolicy)
        {
            return ApplySingle(BeginTerrainStroke(terrain, occupiedCellPolicy), x, y);
        }

        public bool SetFloorHeightAtVertex(int x, int y, int height)
        {
            return ApplySingle(BeginFloorHeightStroke(height), x, y);
        }

        public bool SetClearanceAtVertex(int x, int y, int clearance)
        {
            return ApplySingle(BeginClearanceStroke(clearance), x, y);
        }

        public ICaveEditStroke BeginTerrainStroke(CaveData terrain, CaveOccupiedCellPolicy occupiedCellPolicy)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            return new CaveEditStroke(this, CaveEditKind.Terrain, _dataResolver.Resolve(terrain.ShortName), 0,
                occupiedCellPolicy);
        }

        public ICaveEditStroke BeginFloorHeightStroke(int height)
        {
            return new CaveEditStroke(this, CaveEditKind.FloorHeight, null, height,
                CaveOccupiedCellPolicy.PreserveAndHide);
        }

        public ICaveEditStroke BeginClearanceStroke(int clearance)
        {
            return new CaveEditStroke(this, CaveEditKind.Clearance, null, clearance,
                CaveOccupiedCellPolicy.PreserveAndHide);
        }

        public ICaveHeightEdit BeginFloorHeightEdit(bool preserveCeilingHeight = false)
        {
            return new CaveHeightEdit(this, CaveEditKind.FloorHeight, preserveCeilingHeight);
        }

        public ICaveHeightEdit BeginClearanceEdit()
        {
            return new CaveHeightEdit(this, CaveEditKind.Clearance, false);
        }

        private static bool ApplySingle(ICaveEditStroke stroke, int x, int y)
        {
            using (stroke)
            {
                bool changed = stroke.ApplyAt(x, y);
                if (changed)
                {
                    stroke.Commit();
                }

                return changed;
            }
        }

        private void ApplyLive(CaveEditChange change)
        {
            ApplyLive(new[] { change });
        }

        private void ApplyLive(CaveEditChange[] changes)
        {
            foreach (CaveEditChange change in changes)
            {
                ApplyChange(change, true);
            }
            Changed(CreateDirtyRegion(changes));
        }

        private void Apply(CaveEditChange[] changes, bool forward)
        {
            if (forward)
            {
                foreach (CaveEditChange change in changes)
                {
                    ApplyChange(change, true);
                }
            }
            else
            {
                for (int i = changes.Length - 1; i >= 0; i--)
                {
                    ApplyChange(changes[i], false);
                }
            }

            CaveDirtyRegion region = CreateDirtyRegion(changes);
            Changed(region);
            EditCompleted(region);
        }

        private void ApplyChange(CaveEditChange change, bool forward)
        {
            switch (change.Kind)
            {
                case CaveEditKind.Terrain:
                    _target.SetTerrain(change.X, change.Y, forward ? change.NewTerrain : change.OldTerrain);
                    if (change.ContentRemoval != null)
                    {
                        if (forward)
                        {
                            change.ContentRemoval.Remove();
                        }
                        else
                        {
                            change.ContentRemoval.Restore();
                        }
                    }
                    break;
                case CaveEditKind.FloorHeight:
                    _target.SetFloorHeightAtVertex(change.X, change.Y,
                        forward ? change.NewValue : change.OldValue);
                    break;
                case CaveEditKind.Clearance:
                    _target.SetClearanceAtVertex(change.X, change.Y,
                        forward ? change.NewValue : change.OldValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private CaveDirtyRegion CreateDirtyRegion(IEnumerable<CaveEditChange> changes)
        {
            var region = new CaveDirtyRegion();
            foreach (CaveEditChange change in changes)
            {
                if (change.Kind == CaveEditKind.Terrain)
                {
                    AddTerrainDirtyCells(region, change.X, change.Y);
                }
                else
                {
                    AddVertexDirtyCells(region, change.X, change.Y);
                }
            }

            return region;
        }

        private void AddTerrainDirtyCells(CaveDirtyRegion region, int x, int y)
        {
            AddIfCell(region, x, y);
            AddIfCell(region, x - 1, y);
            AddIfCell(region, x + 1, y);
            AddIfCell(region, x, y - 1);
            AddIfCell(region, x, y + 1);
        }

        private void AddVertexDirtyCells(CaveDirtyRegion region, int x, int y)
        {
            AddIfCell(region, x, y);
            AddIfCell(region, x - 1, y);
            AddIfCell(region, x, y - 1);
            AddIfCell(region, x - 1, y - 1);
        }

        private void AddIfCell(CaveDirtyRegion region, int x, int y)
        {
            if (_target.ContainsCell(x, y))
            {
                region.Add(x, y);
            }
        }

        private sealed class CaveEditStroke : ICaveEditStroke
        {
            private readonly CaveEditor _editor;
            private readonly CaveEditKind _kind;
            private readonly CaveData _terrain;
            private readonly int _value;
            private readonly CaveOccupiedCellPolicy _occupiedCellPolicy;
            private readonly HashSet<CaveCellCoordinate> _visited = new HashSet<CaveCellCoordinate>();
            private readonly List<CaveEditChange> _changes = new List<CaveEditChange>();
            private bool _completed;

            public CaveEditStroke(CaveEditor editor, CaveEditKind kind, CaveData terrain, int value,
                CaveOccupiedCellPolicy occupiedCellPolicy)
            {
                _editor = editor;
                _kind = kind;
                _terrain = terrain;
                _value = value;
                _occupiedCellPolicy = occupiedCellPolicy;
            }

            public bool ApplyAt(int x, int y)
            {
                if (_completed)
                {
                    throw new InvalidOperationException("The cave edit stroke is already complete.");
                }

                bool valid = _kind == CaveEditKind.Terrain
                    ? _editor._target.ContainsCell(x, y)
                    : _editor._target.ContainsVertex(x, y);
                var coordinate = new CaveCellCoordinate(x, y);
                if (!valid || !_visited.Add(coordinate))
                {
                    return false;
                }

                CaveEditChange change = CreateChange(x, y);
                if (change == null)
                {
                    return false;
                }

                _changes.Add(change);
                _editor.ApplyLive(change);
                return true;
            }

            public void Commit()
            {
                if (_completed)
                {
                    return;
                }

                if (_changes.Count > 0)
                {
                    _editor._target.Record(new CaveEditCommand(_editor, _changes.ToArray()));
                    _editor.EditCompleted(_editor.CreateDirtyRegion(_changes));
                }

                _completed = true;
            }

            public void Cancel()
            {
                if (_completed)
                {
                    return;
                }

                if (_changes.Count > 0)
                {
                    _editor.Apply(_changes.ToArray(), false);
                }

                _completed = true;
            }

            public void Dispose()
            {
                Cancel();
            }

            private CaveEditChange CreateChange(int x, int y)
            {
                switch (_kind)
                {
                    case CaveEditKind.Terrain:
                        return CreateTerrainChange(x, y);
                    case CaveEditKind.FloorHeight:
                    {
                        int oldHeight = _editor._target.GetFloorHeightAtVertex(x, y);
                        return oldHeight == _value
                            ? null
                            : CaveEditChange.CreateHeight(_kind, x, y, oldHeight, _value);
                    }
                    case CaveEditKind.Clearance:
                    {
                        int oldClearance = _editor._target.GetClearanceAtVertex(x, y);
                        return oldClearance == _value
                            ? null
                            : CaveEditChange.CreateHeight(_kind, x, y, oldClearance, _value);
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private CaveEditChange CreateTerrainChange(int x, int y)
            {
                CaveData oldTerrain = _editor._target.GetTerrain(x, y);
                if (oldTerrain == _terrain)
                {
                    return null;
                }

                ICaveContentRemoval contentRemoval = null;
                bool solidifying = !oldTerrain.Wall && _terrain.Wall;
                if (solidifying && _editor._target.HasCaveContent(x, y))
                {
                    switch (_occupiedCellPolicy)
                    {
                        case CaveOccupiedCellPolicy.PreserveAndHide:
                            break;
                        case CaveOccupiedCellPolicy.DeleteCellContent:
                            contentRemoval = _editor._target.CreateCaveContentRemoval(x, y);
                            break;
                        case CaveOccupiedCellPolicy.PreventSolidifying:
                            return null;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return CaveEditChange.CreateTerrain(x, y, oldTerrain, _terrain, contentRemoval);
            }
        }

        private sealed class CaveHeightEdit : ICaveHeightEdit
        {
            private readonly CaveEditor _editor;
            private readonly CaveEditKind _kind;
            private readonly bool _preserveCeilingHeight;
            private readonly Dictionary<CaveCellCoordinate, CaveHeightEditEntry> _entries =
                new Dictionary<CaveCellCoordinate, CaveHeightEditEntry>();
            private readonly List<CaveCellCoordinate> _order = new List<CaveCellCoordinate>();
            private bool _completed;

            public CaveHeightEdit(CaveEditor editor, CaveEditKind kind, bool preserveCeilingHeight)
            {
                _editor = editor;
                _kind = kind;
                _preserveCeilingHeight = preserveCeilingHeight;
            }

            public bool SetAt(int x, int y, int value)
            {
                if (_completed)
                {
                    throw new InvalidOperationException("The cave height edit is already complete.");
                }
                if (!_editor._target.ContainsVertex(x, y))
                {
                    return false;
                }

                var coordinate = new CaveCellCoordinate(x, y);
                int currentValue = GetCurrentValue(x, y);
                if (!_entries.TryGetValue(coordinate, out CaveHeightEditEntry entry))
                {
                    int originalClearance = _editor._target.GetClearanceAtVertex(x, y);
                    entry = new CaveHeightEditEntry(x, y, currentValue, value,
                        originalClearance, originalClearance);
                    _entries.Add(coordinate, entry);
                    _order.Add(coordinate);
                }
                else
                {
                    entry.CurrentValue = value;
                }

                int targetClearance = entry.CurrentClearance;
                if (_kind == CaveEditKind.FloorHeight && _preserveCeilingHeight)
                {
                    targetClearance = Math.Max(0,
                        entry.OriginalValue + entry.OriginalClearance - value);
                    entry.CurrentClearance = targetClearance;
                }

                bool valueChanged = currentValue != value;
                int currentClearance = _editor._target.GetClearanceAtVertex(x, y);
                bool clearanceChanged = _kind == CaveEditKind.FloorHeight && _preserveCeilingHeight
                    && currentClearance != targetClearance;
                if (!valueChanged && !clearanceChanged)
                {
                    return false;
                }

                var liveChanges = new List<CaveEditChange>();
                if (valueChanged)
                {
                    liveChanges.Add(CaveEditChange.CreateHeight(_kind, x, y, currentValue, value));
                }
                if (clearanceChanged)
                {
                    liveChanges.Add(CaveEditChange.CreateHeight(CaveEditKind.Clearance, x, y,
                        currentClearance, targetClearance));
                }
                _editor.ApplyLive(liveChanges.ToArray());
                return true;
            }

            public void Commit()
            {
                if (_completed)
                {
                    return;
                }

                CaveEditChange[] changes = CreateChanges();
                if (changes.Length > 0)
                {
                    _editor._target.Record(new CaveEditCommand(_editor, changes));
                    _editor.EditCompleted(_editor.CreateDirtyRegion(changes));
                }

                _completed = true;
            }

            public void Cancel()
            {
                if (_completed)
                {
                    return;
                }

                CaveEditChange[] changes = CreateChanges();
                if (changes.Length > 0)
                {
                    _editor.Apply(changes, false);
                }

                _completed = true;
            }

            public void Dispose()
            {
                Cancel();
            }

            private int GetCurrentValue(int x, int y)
            {
                return _kind == CaveEditKind.FloorHeight
                    ? _editor._target.GetFloorHeightAtVertex(x, y)
                    : _editor._target.GetClearanceAtVertex(x, y);
            }

            private CaveEditChange[] CreateChanges()
            {
                var changes = new List<CaveEditChange>();
                foreach (CaveCellCoordinate coordinate in _order)
                {
                    CaveHeightEditEntry entry = _entries[coordinate];
                    if (entry.OriginalValue != entry.CurrentValue)
                    {
                        changes.Add(CaveEditChange.CreateHeight(_kind, entry.X, entry.Y,
                            entry.OriginalValue, entry.CurrentValue));
                    }
                    if (_kind == CaveEditKind.FloorHeight && _preserveCeilingHeight
                        && entry.OriginalClearance != entry.CurrentClearance)
                    {
                        changes.Add(CaveEditChange.CreateHeight(CaveEditKind.Clearance, entry.X, entry.Y,
                            entry.OriginalClearance, entry.CurrentClearance));
                    }
                }
                return changes.ToArray();
            }
        }

        private sealed class CaveHeightEditEntry
        {
            public int X { get; }
            public int Y { get; }
            public int OriginalValue { get; }
            public int CurrentValue { get; set; }
            public int OriginalClearance { get; }
            public int CurrentClearance { get; set; }

            public CaveHeightEditEntry(int x, int y, int originalValue, int currentValue,
                int originalClearance, int currentClearance)
            {
                X = x;
                Y = y;
                OriginalValue = originalValue;
                CurrentValue = currentValue;
                OriginalClearance = originalClearance;
                CurrentClearance = currentClearance;
            }
        }

        private sealed class CaveEditCommand : IReversibleCommand
        {
            private readonly CaveEditor _editor;
            private readonly CaveEditChange[] _changes;

            public CaveEditCommand(CaveEditor editor, CaveEditChange[] changes)
            {
                _editor = editor;
                _changes = changes;
            }

            public void Execute()
            {
                _editor.Apply(_changes, true);
            }

            public void Undo()
            {
                _editor.Apply(_changes, false);
            }

            public void DisposeUndo()
            {
                foreach (CaveEditChange change in _changes)
                {
                    change.ContentRemoval?.DestroyRemoved();
                }
            }

            public void DisposeRedo() { }
        }

        private sealed class CaveEditChange
        {
            public CaveEditKind Kind { get; private set; }
            public int X { get; private set; }
            public int Y { get; private set; }
            public CaveData OldTerrain { get; private set; }
            public CaveData NewTerrain { get; private set; }
            public int OldValue { get; private set; }
            public int NewValue { get; private set; }
            public ICaveContentRemoval ContentRemoval { get; private set; }

            public static CaveEditChange CreateTerrain(int x, int y, CaveData oldTerrain, CaveData newTerrain,
                ICaveContentRemoval contentRemoval)
            {
                return new CaveEditChange
                {
                    Kind = CaveEditKind.Terrain,
                    X = x,
                    Y = y,
                    OldTerrain = oldTerrain,
                    NewTerrain = newTerrain,
                    ContentRemoval = contentRemoval
                };
            }

            public static CaveEditChange CreateHeight(CaveEditKind kind, int x, int y, int oldValue, int newValue)
            {
                return new CaveEditChange
                {
                    Kind = kind,
                    X = x,
                    Y = y,
                    OldValue = oldValue,
                    NewValue = newValue
                };
            }
        }

        private enum CaveEditKind
        {
            Terrain,
            FloorHeight,
            Clearance
        }
    }
}
