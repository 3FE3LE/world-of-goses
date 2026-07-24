using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public class WorldPersistenceTests
{
    [Fact]
    public void Capture_ExplicitProductionScenario_RecordsCitizensAndBuildings()
    {
        var world = TestHelpers.NewProductionWorld();
        var save = WorldPersistence.Capture(world);

        Assert.Equal(5, save.Citizens.Count);
        Assert.Equal(3, save.Buildings.Count);
        Assert.Equal(0, save.CurrentTick);
    }

    [Fact]
    public void Capture_AfterAssignmentsAndProduction_ReflectsCurrentState()
    {
        var world = TestHelpers.NewProductionWorld();
        var freeCitizen = world.AvailableCitizens()[0];
        world.TryAssignCitizen(world.PrimaryBuilding.Id, freeCitizen.Id);
        world.AdvanceProduction(world.PrimaryBuilding.Id);
        world.AdvanceProduction(world.PrimaryBuilding.Id);

        var save = WorldPersistence.Capture(world);

        Assert.True(save.CurrentTick >= 2);
        Assert.Equal(world.PrimaryBuilding.AssignedCount, save.Buildings[0].AssignedCitizenIds.Count);
        Assert.Equal(world.PrimaryBuilding.Stock, save.Buildings[0].Stock);
        Assert.Contains(save.Citizens, c => c.Id == freeCitizen.Id.Value && c.CurrentAssignment == 1);
    }

    [Fact]
    public void Capture_MultiBuilding_RecordsKindResourceAndCompetency()
    {
        var world = TestHelpers.NewProductionWorld();
        var save = WorldPersistence.Capture(world);

        var quarrySave = save.Buildings.Single(b => b.Id == 1);
        var farmSave = save.Buildings.Single(b => b.Id == 2);

        Assert.Equal(BuildingKind.Quarry.ToString(), quarrySave.Kind);
        Assert.Equal(BuildingKind.Farm.ToString(), farmSave.Kind);
        Assert.Equal(ResourceType.Stone.ToString(), quarrySave.ProducedResourceType);
        Assert.Equal(ResourceType.Food.ToString(), farmSave.ProducedResourceType);
        Assert.Equal(CompetencyId.Mining.Value, quarrySave.ProducedCompetencyId);
        Assert.Equal(CompetencyId.Farming.Value, farmSave.ProducedCompetencyId);
        Assert.Equal("Stone", quarrySave.ResourceLabel);
        Assert.Equal("Food", farmSave.ResourceLabel);
    }

    [Fact]
    public void Roundtrip_PreservesProductionPolicyIncludingZeroRange()
    {
        var world = TestHelpers.NewProductionWorld();
        world.PrimaryBuilding.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: 0, priority: 0);

        var save = WorldPersistence.Capture(world);
        var restored = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(WorldPersistence.SerializeToJson(save)));

        Assert.False(restored.PrimaryBuilding.ProductionEnabled);
        Assert.Equal(0, restored.PrimaryBuilding.MinStock);
        Assert.Equal(0, restored.PrimaryBuilding.MaxStock);
        Assert.Equal(0, restored.PrimaryBuilding.Priority);
    }

    [Fact]
    public void Capture_StampsCurrentSchemaVersion()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        Assert.Equal(WorldSave.CurrentVersion, save.Version);
        Assert.Equal(WorldSave.CurrentVersion, save.Version);
    }

    [Fact]
    public void Roundtrip_VersionPersists()
    {
        var world = TestHelpers.NewProductionWorld();
        var save = WorldPersistence.Capture(world);
        var json = WorldPersistence.SerializeToJson(save);
        var restored = WorldPersistence.DeserializeFromJson(json);
        Assert.Equal(WorldSave.CurrentVersion, restored.Version);
    }

    [Fact]
    public void Roundtrip_SerializeDeserialize_PreservesWorldSave()
    {
        var world = TestHelpers.NewProductionWorld();
        var freeCitizen = world.AvailableCitizens()[0];
        world.TryAssignCitizen(world.PrimaryBuilding.Id, freeCitizen.Id);
        for (int i = 0; i < 5; i++)
        {
            world.AdvanceProduction(world.PrimaryBuilding.Id);
        }

        var save = WorldPersistence.Capture(world);
        var json = WorldPersistence.SerializeToJson(save);
        var restored = WorldPersistence.DeserializeFromJson(json);

        AssertWorldSaveEquals(save, restored);
    }

    [Fact]
    public void Roundtrip_FromSave_ProducesIdenticalLiveCityWorld()
    {
        var world = TestHelpers.NewProductionWorld();
        var freeCitizen = world.AvailableCitizens()[0];
        world.TryAssignCitizen(world.PrimaryBuilding.Id, freeCitizen.Id);
        for (int i = 0; i < 3; i++)
        {
            world.AdvanceProduction(world.PrimaryBuilding.Id);
        }

        var save = WorldPersistence.Capture(world);
        var json = WorldPersistence.SerializeToJson(save);
        var save2 = WorldPersistence.DeserializeFromJson(json);
        var restored = CityWorld.FromSave(save2);

        AssertCityWorldEquals(world, restored);
    }

    [Fact]
    public void Validate_V1Save_ThrowsIncompatibleVersion()
    {
        var save = new WorldSave { Version = 1 };

        var error = Assert.Throws<IncompatibleSaveVersionException>(
            () => WorldPersistence.Validate(save));

        Assert.Equal(1, error.FoundVersion);
        Assert.Equal(WorldSave.CurrentVersion, error.ExpectedVersion);
    }

    [Fact]
    public void Deserialize_EmptyString_Throws()
    {
        Assert.Throws<JsonException>(() => WorldPersistence.DeserializeFromJson(""));
    }

    [Fact]
    public void Deserialize_MalformedJson_Throws()
    {
        Assert.Throws<JsonException>(() => WorldPersistence.DeserializeFromJson("not valid json"));
    }

    [Fact]
    public void Deserialize_NullJsonDocument_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => WorldPersistence.DeserializeFromJson("null"));
    }

    [Fact]
    public void Validate_ValidSave_DoesNotThrow()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        WorldPersistence.Validate(save);
    }

    [Fact]
    public void Validate_NullSave_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(null!));
    }

    [Fact]
    public void Validate_NullBuildingsList_Throws()
    {
        var save = new WorldSave { Buildings = null! };
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void Validate_NullBuildingEntry_Throws()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Buildings.Add(null!);
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void Validate_NullCitizenEntry_Throws()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Citizens.Add(null!);
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void Validate_NoBuildings_Throws()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Buildings.Clear();
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void Validate_DuplicateBuildingId_Throws()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Buildings[1].Id = save.Buildings[0].Id;
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void Validate_AssignmentMismatch_Throws()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Citizens.Single(c => c.Id == 1).CurrentAssignment = null;
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void Validate_NullCompetencyEntry_Throws()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Citizens[0].Competencies.Add(null!);
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void FromSave_InvalidSave_DoesNotMutatePartially()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.CurrentTick = -1;
        Assert.Throws<InvalidOperationException>(() => CityWorld.FromSave(save));
    }

    [Fact]
    public void Roundtrip_PreservesNamesCompetenciesRoles()
    {
        var world = TestHelpers.NewProductionWorld();
        world.AdvanceProduction(world.PrimaryBuilding.Id);
        world.AdvanceProduction(world.PrimaryBuilding.Id);

        var save = WorldPersistence.Capture(world);
        var json = WorldPersistence.SerializeToJson(save);
        var save2 = WorldPersistence.DeserializeFromJson(json);

        var branSave = save2.Citizens.First(c => c.Id == 1);
        Assert.Equal("Aster", branSave.Name);
        Assert.NotEmpty(branSave.Competencies);
        Assert.Contains(branSave.Competencies, c => c.Id == "mining" && c.Experience > 0);
        Assert.Contains(branSave.Roles, r => r.Id == "miner");
    }

    [Fact]
    public void WriteToFile_AtomicWrite_NoTempLingersOnSuccess()
    {
        var dir = NewTempDir();
        try
        {
            var path = Path.Combine(dir, "save.json");
            var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
            WorldPersistence.WriteToFile(save, path);

            Assert.True(File.Exists(path));
            Assert.False(File.Exists(path + ".tmp"),
                ".tmp file should be cleaned up after a successful write.");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void WriteToFile_OverwritesExisting_LeavesBakSidecar()
    {
        var dir = NewTempDir();
        try
        {
            var path = Path.Combine(dir, "save.json");
            var save1 = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
            WorldPersistence.WriteToFile(save1, path);

            var save2 = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
            save2.CurrentTick = 999;
            WorldPersistence.WriteToFile(save2, path);

            Assert.True(File.Exists(path));
            Assert.False(File.Exists(path + ".tmp"));
            Assert.True(File.Exists(path + ".bak"));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void WriteToFile_CreatatesParentDirectoryIfMissing()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var nestedPath = Path.Combine(tempRoot, "sub", "save.json");
            var world = TestHelpers.NewProductionWorld();
            WorldPersistence.WriteToFile(WorldPersistence.Capture(world), nestedPath);
            Assert.True(File.Exists(nestedPath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void SlotPath_DefaultEndsWithJsonAndContainsSlotNumber()
    {
        Assert.EndsWith("save_slot_0.json", WorldPersistence.SlotPath(0));
        Assert.EndsWith("save_slot_1.json", WorldPersistence.SlotPath(1));
        Assert.Contains("slots", WorldPersistence.SlotPath(0));
    }

    [Fact]
    public void DeleteSlot_RemovesOnlyRequestedSlotAndItsSidecars()
    {
        string slotsDir = Path.Combine(Path.GetTempPath(), $"wog-delete-slot-{Guid.NewGuid():N}");
        Directory.CreateDirectory(slotsDir);
        try
        {
            string slot0 = Path.Combine(slotsDir, "save_slot_0.json");
            string slot1 = Path.Combine(slotsDir, "save_slot_1.json");
            File.WriteAllText(slot0, "{}");
            File.WriteAllText(slot0 + ".bak", "{}");
            File.WriteAllText(slot0 + ".tmp", "{}");
            File.WriteAllText(slot1, "{}");

            Assert.True(WorldPersistence.DeleteSlot(0, slotsDir));

            Assert.False(File.Exists(slot0));
            Assert.False(File.Exists(slot0 + ".bak"));
            Assert.False(File.Exists(slot0 + ".tmp"));
            Assert.True(File.Exists(slot1));
        }
        finally
        {
            if (Directory.Exists(slotsDir)) Directory.Delete(slotsDir, recursive: true);
        }
    }

    [Fact]
    public void SaveToSlot_RoundtripsViaLoadFromSlot()
    {
        var slotsDir = NewTempDir();
        try
        {
            var world = TestHelpers.NewProductionWorld();
            world.AdvanceProduction(world.PrimaryBuilding.Id);
            world.AdvanceProduction(world.PrimaryBuilding.Id);

            WorldPersistence.SaveToSlot(world, slot: 0, slotsDirectory: slotsDir);

            var restored = WorldPersistence.LoadFromSlot(slot: 0, slotsDirectory: slotsDir);
            Assert.Equal(WorldSave.CurrentVersion, restored.Version);
            Assert.Equal(world.CurrentTick, restored.CurrentTick);
        }
        finally
        {
            if (Directory.Exists(slotsDir)) Directory.Delete(slotsDir, recursive: true);
        }
    }

    [Fact]
    public void SlotFiles_AreIndependentAcrossSlots()
    {
        var slotsDir = NewTempDir();
        try
        {
            var w1 = TestHelpers.NewProductionWorld();
            var w2 = TestHelpers.NewProductionWorld();
            w2.AdvanceProduction(w2.PrimaryBuilding.Id);
            w2.AdvanceProduction(w2.PrimaryBuilding.Id);
            w2.AdvanceProduction(w2.PrimaryBuilding.Id);

            WorldPersistence.SaveToSlot(w1, slot: 0, slotsDirectory: slotsDir);
            WorldPersistence.SaveToSlot(w2, slot: 1, slotsDirectory: slotsDir);

            Assert.True(WorldPersistence.SlotExists(0, slotsDir));
            Assert.True(WorldPersistence.SlotExists(1, slotsDir));

            var r1 = WorldPersistence.LoadFromSlot(0, slotsDir);
            var r2 = WorldPersistence.LoadFromSlot(1, slotsDir);
            Assert.Equal(0, r1.CurrentTick);
            Assert.Equal(3, r2.CurrentTick);
        }
        finally
        {
            if (Directory.Exists(slotsDir)) Directory.Delete(slotsDir, recursive: true);
        }
    }

    [Fact]
    public void LoadFromSlot_MissingSlot_Throws()
    {
        var slotsDir = NewTempDir();
        try
        {
            Assert.Throws<FileNotFoundException>(
                () => WorldPersistence.LoadFromSlot(99, slotsDir));
        }
        finally
        {
            if (Directory.Exists(slotsDir)) Directory.Delete(slotsDir, recursive: true);
        }
    }

    [Fact]
    public void LoadFromSlot_CorruptJson_Throws()
    {
        var slotsDir = NewTempDir();
        try
        {
            File.WriteAllText(Path.Combine(slotsDir, "save_slot_0.json"), "{ this is not json");
            Assert.Throws<JsonException>(
                () => WorldPersistence.LoadFromSlot(0, slotsDir));
        }
        finally
        {
            if (Directory.Exists(slotsDir)) Directory.Delete(slotsDir, recursive: true);
        }
    }

    [Fact]
    public void Roundtrip_PreservesStaminaCurrentAndMax()
    {
        var world = TestHelpers.NewProductionWorld();
        var bran = world.GetCitizen(new CitizenId(1))!;
        bran.ConsumeStamina(40); // -> 60 / 100
        var branBeforeStamina = bran.CurrentStamina;
        var branBeforeMax = bran.MaxStamina;

        var save = WorldPersistence.Capture(world);
        var json = WorldPersistence.SerializeToJson(save);
        var restored = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(json));

        var restoredBran = restored.GetCitizen(bran.Id)!;
        Assert.Equal(branBeforeStamina, restoredBran.CurrentStamina);
        Assert.Equal(branBeforeMax, restoredBran.MaxStamina);
    }

    [Fact]
    public void Validate_V2CitizenWithoutProfile_Throws()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewHeroWorld());
        save.Citizens[0].Profile = null;

        var error = Assert.Throws<InvalidOperationException>(
            () => WorldPersistence.Validate(save));

        Assert.Contains("profile is missing", error.Message);
    }

    [Fact]
    public void Validate_NegativeStaminaCurrent_Throws()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Citizens[0].StaminaCurrent = -1;
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void Validate_StaminaCurrentExceedsStaminaMax_Throws()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Citizens[0].StaminaMax = 50;
        save.Citizens[0].StaminaCurrent = 51;
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void Validate_NullStaminaMax_IsAllowed()
    {
        // Old saves have StaminaCurrent = 0 (default int) and
        // StaminaMax = null. Validation must accept that.
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        foreach (var c in save.Citizens)
        {
            c.StaminaMax = null;
            c.StaminaCurrent = 0;
        }
        WorldPersistence.Validate(save);
    }

    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wog-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void AssertWorldSaveEquals(WorldSave expected, WorldSave actual)
    {
        Assert.Equal(expected.CurrentTick, actual.CurrentTick);
        Assert.Equal(expected.Buildings.Count, actual.Buildings.Count);
        Assert.Equal(expected.Citizens.Count, actual.Citizens.Count);
        Assert.Equal(expected.CityInventory, actual.CityInventory);

        for (int i = 0; i < expected.Buildings.Count; i++)
        {
            var em = expected.Buildings[i];
            var am = actual.Buildings[i];
            Assert.Equal(em.Id, am.Id);
            Assert.Equal(em.Kind, am.Kind);
            Assert.Equal(em.ProducedResourceType, am.ProducedResourceType);
            Assert.Equal(em.ProducedCompetencyId, am.ProducedCompetencyId);
            Assert.Equal(em.ResourceLabel, am.ResourceLabel);
            Assert.Equal(em.ResourceUnit, am.ResourceUnit);
            Assert.Equal(em.DisplayName, am.DisplayName);
            Assert.Equal(em.WorkerCapacity, am.WorkerCapacity);
            Assert.Equal(em.StorageCapacity, am.StorageCapacity);
            Assert.Equal(em.Stock, am.Stock);
            Assert.Equal(em.ProductionEnabled, am.ProductionEnabled);
            Assert.Equal(em.MinStock, am.MinStock);
            Assert.Equal(em.MaxStock, am.MaxStock);
            Assert.Equal(em.Priority, am.Priority);
            Assert.Equal(em.WoodUnitReserves, am.WoodUnitReserves);
            Assert.Equal(em.AssignedCitizenIds, am.AssignedCitizenIds);
        }

        for (int i = 0; i < expected.Citizens.Count; i++)
        {
            var ec = expected.Citizens[i];
            var ac = actual.Citizens[i];
            Assert.Equal(ec.Id, ac.Id);
            Assert.Equal(ec.Name, ac.Name);
            Assert.Equal(ec.CurrentAssignment, ac.CurrentAssignment);
            Assert.Equal(ec.Competencies.Count, ac.Competencies.Count);
            Assert.Equal(ec.Roles.Count, ac.Roles.Count);
            Assert.Equal(
                ec.LastVisitedResourceBuildingId,
                ac.LastVisitedResourceBuildingId);
            Assert.Equal(ec.LastVisitedResourceUnitId, ac.LastVisitedResourceUnitId);
            Assert.Equal(
                ec.LastVisitedResourcePositionIndex,
                ac.LastVisitedResourcePositionIndex);
        }
    }

    private static void AssertCityWorldEquals(CityWorld expected, CityWorld actual)
    {
        Assert.Equal(expected.CurrentTick, actual.CurrentTick);
        Assert.Equal(expected.Buildings.Count, actual.Buildings.Count);
        Assert.Equal(expected.Citizens.Count, actual.Citizens.Count);
        Assert.Equal(
            expected.Resources.Total(ResourceType.Wood),
            actual.Resources.Total(ResourceType.Wood));

        foreach (var em in expected.Buildings.Values)
        {
            var am = actual.GetBuilding(em.Id);
            Assert.NotNull(am);
            Assert.Equal(em.Kind, am!.Kind);
            Assert.Equal(em.ProducedResourceType, am.ProducedResourceType);
            Assert.Equal(em.ProducedCompetencyId, am.ProducedCompetencyId);
            Assert.Equal(em.ResourceLabel, am.ResourceLabel);
            Assert.Equal(em.ResourceUnit, am.ResourceUnit);
            Assert.Equal(em.DisplayName, am.DisplayName);
            Assert.Equal(em.WorkerCapacity, am.WorkerCapacity);
            Assert.Equal(em.StorageCapacity, am.StorageCapacity);
            Assert.Equal(em.Stock, am.Stock);
            Assert.Equal(em.ProductionEnabled, am.ProductionEnabled);
            Assert.Equal(em.MinStock, am.MinStock);
            Assert.Equal(em.MaxStock, am.MaxStock);
            Assert.Equal(em.Priority, am.Priority);
            Assert.Equal(em.AssignedCitizenIds, am.AssignedCitizenIds);
        }

        foreach (var ec in expected.Citizens.Values)
        {
            var ac = actual.GetCitizen(ec.Id);
            Assert.NotNull(ac);
            Assert.Equal(ec.Name, ac!.Name);
            Assert.Equal(ec.CurrentAssignment, ac.CurrentAssignment);
            Assert.Equal(ec.Roles.Count, ac.Roles.Count);

            foreach (var entry in ec.Competencies.Values)
            {
                Assert.Equal(
                    ec.GetExperience(entry.Id),
                    ac.GetExperience(entry.Id));
            }
        }
    }
}
