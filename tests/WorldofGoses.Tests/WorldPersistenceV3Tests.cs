using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Persistence v3-v6 schema. The reactive policy triplet (MinStock/MaxStock/Priority)
/// was added in v3; explicit <see cref="GenderId"/> identity was added
/// in v4. Older saves must upgrade via MigrateV2ToV3 and
/// MigrateV3ToV4, MigrateV4ToV5, and MigrateV5ToV6 so the load path is non-fatal.
/// </summary>
public class WorldPersistenceV3Tests
{
    [Fact]
    public void Roundtrip_PreservesMinMaxPriorityAndGender()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        quarry.ConfigureProductionPolicy(true, minStock: 4, maxStock: 12, priority: 7);

        var save = WorldPersistence.Capture(world);
        Assert.Equal(WorldSave.CurrentVersion, save.Version);

        var restored = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(WorldPersistence.SerializeToJson(save)));

        Assert.Equal(4, restored.GetBuilding(new BuildingId(1))!.MinStock);
        Assert.Equal(12, restored.GetBuilding(new BuildingId(1))!.MaxStock);
        Assert.Equal(7, restored.GetBuilding(new BuildingId(1))!.Priority);
        Assert.Equal(GenderId.Masculine, restored.Hero!.Profile.Gender);
    }

    [Fact]
    public void LoadV2Save_DefaultsMinMaxPriorityAfterUpgrade()
    {
        // Capture a v4 save then downgrade to v2 and strip the new
        // fields via JSON mutation. MigrateV2ToV3 + MigrateV3ToV4
        // upgrade it back so the load path is non-fatal.
        var world = TestHelpers.NewProductionWorld();
        var save = WorldPersistence.Capture(world);
        save.Version = 2;

        var json = WorldPersistence.SerializeToJson(save);
        json = System.Text.RegularExpressions.Regex.Replace(
            json, "\"MinStock\":\\s*\\d+,?", string.Empty);
        json = System.Text.RegularExpressions.Regex.Replace(
            json, "\"MaxStock\":\\s*\\d+,?", string.Empty);
        json = System.Text.RegularExpressions.Regex.Replace(
            json, "\"Priority\":\\s*\\d+,?", string.Empty);
        json = System.Text.RegularExpressions.Regex.Replace(
            json, "\"DepositedInputs\":\\s*\\{\\s*\\},?", string.Empty);
        json = System.Text.RegularExpressions.Regex.Replace(
            json, "\"RemainingInputs\":\\s*\\{\\s*\\},?", string.Empty);
        json = System.Text.RegularExpressions.Regex.Replace(
            json, "\"Gender\":\\s*\"[^\"]*\",?", string.Empty);

        var v2Save = WorldPersistence.DeserializeFromJson(json);
        Assert.Equal(2, v2Save.Version);

        var v3Save = WorldPersistence.MigrateV2ToV3(v2Save);
        Assert.Equal(3, v3Save.Version);

        var v4Save = WorldPersistence.MigrateV3ToV4(v3Save);
        Assert.Equal(4, v4Save.Version);

        var v5Save = WorldPersistence.MigrateV4ToV5(v4Save);
        Assert.Equal(5, v5Save.Version);

        var v6Save = WorldPersistence.MigrateV5ToV6(v5Save);
        Assert.Equal(6, v6Save.Version);

        var v7Save = WorldPersistence.MigrateV6ToV7(v6Save);
        Assert.Equal(7, v7Save.Version);

        var v8Save = WorldPersistence.MigrateV7ToV8(v7Save);
        Assert.Equal(8, v8Save.Version);

        var v9Save = WorldPersistence.MigrateV8ToV9(v8Save);
        Assert.Equal(9, v9Save.Version);

        var v10Save = WorldPersistence.MigrateV9ToV10(v9Save);
        Assert.Equal(10, v10Save.Version);

        var v11Save = WorldPersistence.MigrateV10ToV11(v10Save);
        Assert.Equal(11, v11Save.Version);

        var v12Save = WorldPersistence.MigrateV11ToV12(v11Save);
        Assert.Equal(12, v12Save.Version);
        Assert.All(v12Save.Citizens, c => Assert.Equal("standard", c.AppearanceVariant));

        var v13Save = WorldPersistence.MigrateV12ToV13(v12Save);
        Assert.Equal(13, v13Save.Version);
        Assert.NotNull(v13Save.Expeditions);
        Assert.Empty(v13Save.Expeditions);

        var v14Save = WorldPersistence.MigrateV13ToV14(v13Save);
        Assert.Equal(14, v14Save.Version);
        Assert.DoesNotContain(v14Save.Buildings,
            b => b.Kind == BuildingKind.Forest.ToString());

        var v15Save = WorldPersistence.MigrateV14ToV15(v14Save);
        Assert.Equal(15, v15Save.Version);

        var v16Save = WorldPersistence.MigrateV15ToV16(v15Save);
        Assert.Equal(16, v16Save.Version);

        var v17Save = WorldPersistence.MigrateV16ToV17(v16Save);
        Assert.Equal(17, v17Save.Version);
        var v18Save = WorldPersistence.MigrateV17ToV18(v17Save);
        var v19Save = WorldPersistence.MigrateV18ToV19(v18Save);
        Assert.Equal(WorldSave.CurrentVersion, v19Save.Version);

        var restored = CityWorld.FromSave(v19Save);
        var quarry = restored.GetBuilding(new BuildingId(1))!;
        Assert.Equal(0, quarry.MinStock);
        Assert.Equal(quarry.StorageCapacity, quarry.MaxStock);
        Assert.Equal(0, quarry.Priority);
        Assert.Equal(GenderId.Masculine, restored.Hero!.Profile.Gender);
    }

    [Fact]
    public void Validate_MinGreaterThanMax_Throws()
    {
        var save = MakeCurrentSave();
        save.Buildings[0].MinStock = 10;
        save.Buildings[0].MaxStock = 5;

        Assert.Throws<System.InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void Validate_NegativePriority_Throws()
    {
        var save = MakeCurrentSave();
        save.Buildings[0].Priority = -1;

        Assert.Throws<System.InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    private static WorldSave MakeCurrentSave()
    {
        var world = TestHelpers.NewProductionWorld();
        var save = WorldPersistence.Capture(world);
        save.Version = WorldSave.CurrentVersion;
        return save;
    }
}
