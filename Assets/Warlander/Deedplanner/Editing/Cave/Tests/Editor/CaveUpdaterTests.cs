using System;
using NUnit.Framework;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class CaveUpdaterTests
    {
        [Test]
        public void SwitchingClickTargetSelectsItsAssignedTerrainWithoutChangingAssignments()
        {
            var database = CreateDatabase();
            var view = new CaveView();
            var updater = new CaveUpdater(view, null, null, database, null, null,
                new CaveRenderOptions(), null, null);
            updater.Initialize();
            view.TargetPrimary(false);
            view.Choose(database.GetCave("rs"));
            view.TargetPrimary(true);
            view.Choose(database.GetCave("sw"));

            view.TargetPrimary(false);

            Assert.That(view.Selected, Is.SameAs(database.GetCave("rs")));
            Assert.That(view.Primary, Is.SameAs(database.GetCave("sw")));
            Assert.That(view.Secondary, Is.SameAs(database.GetCave("rs")));
            view.TargetPrimary(true);
            Assert.That(view.Selected, Is.SameAs(database.GetCave("sw")));
        }

        [Test]
        public void EnablingCavesRestoresTheActiveClickTargetsSelection()
        {
            var database = CreateDatabase();
            var view = new CaveView();
            var updater = new CaveUpdater(view, null, new TabContext(null), database, null, null,
                new CaveRenderOptions(), null, null);
            updater.Initialize();
            view.TargetPrimary(false);
            view.Choose(database.GetCave("rs"));
            view.SelectCaveEntry(database.GetCave("sw"));

            updater.Enable();

            Assert.That(view.Selected, Is.SameAs(database.GetCave("rs")));
        }

        private static Database CreateDatabase()
        {
            var database = new Database();
            database.AddCave(new CaveData(null, "Stone", "sw", Array.Empty<string[]>(), true, true, false));
            database.AddCave(new CaveData(null, "Floor", "sfl", Array.Empty<string[]>(), false, true, false));
            database.AddCave(new CaveData(null, "Rocksalt", "rs", Array.Empty<string[]>(), true, true, false));
            return database;
        }

        private sealed class CaveView : ICaveUpdaterView
        {
            public event Action<CaveData> CaveSelected;
            public event Action<CaveTool> ToolChanged { add { } remove { } }
            public event Action<bool> PrimaryTargetChanged;
            public event Action<bool> CullingChanged { add { } remove { } }
            public CaveData Primary { get; private set; }
            public CaveData Secondary { get; private set; }
            public CaveData Selected { get; private set; }
            public void TargetPrimary(bool value) => PrimaryTargetChanged?.Invoke(value);
            public void Choose(CaveData data) { Selected = data; CaveSelected?.Invoke(data); }
            public void AddCaveEntry(CaveData data, string[] category) { }
            public void SetPrimaryData(CaveData data) => Primary = data;
            public void SetSecondaryData(CaveData data) => Secondary = data;
            public void SelectCaveEntry(CaveData data) => Selected = data;
            public void SetCullingEnabled(bool enabled) { }
        }
    }
}
