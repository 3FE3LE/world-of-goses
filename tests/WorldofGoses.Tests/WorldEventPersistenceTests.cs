using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public class WorldEventPersistenceTests
{
    [Fact]
    public void Capture_PersistsOnlySignificantEventsAndPreservedCauses()
    {
        var world = TestHelpers.NewProductionWorld();
        var subject = WorldEventSubject.Building(new BuildingId(1), "Quarry");
        var noise = world.Log.Record(0, WorldEventKind.StockProduced, subject, 3);
        var capped = world.Log.Record(0, WorldEventKind.StockCapped, subject);
        world.Log.Record(0, WorldEventKind.ProductionBlocked, subject,
            causeEventId: capped.Id);

        var save = WorldPersistence.Capture(world);

        Assert.Equal(2, save.Events.Count);
        Assert.DoesNotContain(save.Events, evt => evt.Id == noise.Id.Value);
        Assert.Equal(capped.Id.Value, save.Events[1].CauseEventId);
    }

    [Fact]
    public void Capture_CompactsRepeatedState()
    {
        var world = TestHelpers.NewProductionWorld();
        var subject = WorldEventSubject.Building(new BuildingId(1), "Quarry");
        WorldEvent first = world.Log.Record(0, WorldEventKind.StockCapped, subject);
        WorldEvent repeated = world.Log.Record(0, WorldEventKind.StockCapped, subject);

        var save = WorldPersistence.Capture(world);

        Assert.Single(save.Events);
        Assert.Equal(repeated.Id.Value, save.Events[0].Id);
        Assert.DoesNotContain(save.Events, evt => evt.Id == first.Id.Value);
    }

    [Fact]
    public void Capture_BoundsHistory()
    {
        var world = TestHelpers.NewProductionWorld();
        for (int i = 0; i < WorldEventRetention.MaximumPersistedEvents + 5; i++)
        {
            world.Log.Record(0, WorldEventKind.ProjectCompleted,
                WorldEventSubject.ConstructionProject(new BuildingId(100 + i), $"Project {i}"));
        }

        var save = WorldPersistence.Capture(world);

        Assert.Equal(WorldEventRetention.MaximumPersistedEvents, save.Events.Count);
        Assert.Equal(6, save.Events[0].Id);
    }

    [Fact]
    public void Roundtrip_RestoresDurableHistoryAndContinuesIds()
    {
        var world = TestHelpers.NewProductionWorld();
        var subject = WorldEventSubject.Building(new BuildingId(1), "Quarry");
        var capped = world.Log.Record(0, WorldEventKind.StockCapped, subject);
        world.Log.Record(0, WorldEventKind.ProductionBlocked, subject,
            causeEventId: capped.Id);

        CityWorld restored = CityWorld.FromSave(WorldPersistence.DeserializeFromJson(
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));

        Assert.Equal(2, restored.Log.Events.Count);
        Assert.Equal(capped.Id, restored.Log.Events[1].CauseEventId);
        WorldEvent next = restored.Log.Record(0, WorldEventKind.ForestDemolished,
            WorldEventSubject.Building(new BuildingId(99), "Forest"));
        Assert.Equal(new WorldEventId(3), next.Id);
    }

    [Fact]
    public void MigrateV4ToV5_AddsEmptyHistory()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Version = 4;
        save.Events = null!;

        WorldSave migrated = WorldPersistence.MigrateV4ToV5(save);

        Assert.Equal(5, migrated.Version);
        Assert.Empty(migrated.Events);
    }
}
