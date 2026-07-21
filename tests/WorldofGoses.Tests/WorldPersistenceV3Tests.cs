using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Persistence v3/v4 schema: <see cref="WorldSave.CurrentVersion"/>
/// is now 4. The reactive policy triplet (MinStock/MaxStock/Priority)
/// was added in v3; explicit <see cref="GenderId"/> identity was added
/// in v4. Older saves must upgrade via MigrateV2ToV3 and
/// MigrateV3ToV4 so the load path is non-fatal.
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
        Assert.Equal(4, save.Version);

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

        var restored = CityWorld.FromSave(v4Save);
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