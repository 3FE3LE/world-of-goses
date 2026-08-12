using System;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public class CityResourceLedgerTests
{
    [Fact]
    public void MigrateV10ToV11_MovesLegacyForestStockIntoCityInventory()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        WorldSave legacy = WorldPersistence.Capture(world);
        legacy.Version = 10;
        BuildingSave forest = legacy.Buildings.Find(
            building => building.Kind == BuildingKind.Forest.ToString())!;
        forest.Stock = 7;
        legacy.CityInventory.Clear();

        WorldSave migrated = WorldPersistence.MigrateV10ToV11(legacy);
        Assert.Equal(11, migrated.Version);
        var toCurrent = WorldPersistence.MigrateV11ToV12(migrated);
        toCurrent = WorldPersistence.MigrateV12ToV13(toCurrent);
        toCurrent = WorldPersistence.MigrateV13ToV14(toCurrent);
        toCurrent = WorldPersistence.MigrateV14ToV15(toCurrent);
        toCurrent = WorldPersistence.MigrateV15ToV16(toCurrent);
        toCurrent = WorldPersistence.MigrateV16ToV17(toCurrent);
        toCurrent = WorldPersistence.MigrateV17ToV18(toCurrent);
        toCurrent = WorldPersistence.MigrateToCurrent(toCurrent);

        Assert.Equal(WorldSave.CurrentVersion, toCurrent.Version);
        Assert.Equal(0, forest.Stock);
        Assert.Equal(7, toCurrent.CityInventory[ResourceType.Wood.ToString()]);
        WorldPersistence.Validate(toCurrent);
    }

    [Fact]
    public void MigrateV12ToV13_AppendsEmptyExpeditionList()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Version = 12;
        save.Expeditions = null!;

        WorldSave migrated = WorldPersistence.MigrateV12ToV13(save);
        migrated = WorldPersistence.MigrateV13ToV14(migrated);
        migrated = WorldPersistence.MigrateV14ToV15(migrated);
        migrated = WorldPersistence.MigrateV15ToV16(migrated);
        migrated = WorldPersistence.MigrateV16ToV17(migrated);
        migrated = WorldPersistence.MigrateV17ToV18(migrated);
        migrated = WorldPersistence.MigrateToCurrent(migrated);

        Assert.Equal(WorldSave.CurrentVersion, migrated.Version);
        Assert.NotNull(migrated.Expeditions);
        Assert.Empty(migrated.Expeditions);
        WorldPersistence.Validate(migrated);
    }

    [Fact]
    public void Entries_PreserveStorageLocationAndExcludeNaturalReserveFromAvailableStock()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        Building forest = world.GetBuilding(new BuildingId(100))!;
        world.GatherWood(forest.Id, 3);

        var entries = world.Resources.Entries();

        Assert.Contains(entries, entry =>
            entry.Location == new ResourceLocation(
                ResourceLocationKind.CityInventory,
                new BuildingId(0))
            && entry.Resource == ResourceType.Wood && entry.Amount == 3);
        Assert.Contains(entries, entry =>
            entry.Location == new ResourceLocation(ResourceLocationKind.NaturalReserve, forest.Id)
            && entry.Resource == ResourceType.Wood
            && entry.Amount == CityWorld.StartingForestWoodReserve - 3);
        Assert.Equal(3, world.Resources.Total(ResourceType.Wood));
    }

    [Fact]
    public void TryConsumeBatch_WhenOneResourceIsMissing_LeavesEveryLocationUntouched()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        world.GatherWood(new BuildingId(100), 3);
        int woodBefore = world.TotalStockOf(ResourceType.Wood);

        bool consumed = world.Resources.TryConsume(new[]
        {
            new RecipeInput(ResourceType.Wood, 2),
            new RecipeInput(ResourceType.Wood, 1),
            new RecipeInput(ResourceType.Food, 1),
        }, out ResourceType? missing);

        Assert.False(consumed);
        Assert.Equal(ResourceType.Food, missing);
        Assert.Equal(woodBefore, world.TotalStockOf(ResourceType.Wood));
    }

    [Fact]
    public void Reservation_ReducesAvailabilityWithoutMovingStock_ThenCommitsAtomically()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 5);
        var owner = new ResourceReservationOwner(ResourceReservationOwnerKind.Expedition, 42);

        bool reserved = world.Resources.TryReserve(
            ResourceType.Wood, 4, owner, out ResourceReservation? reservation);

        Assert.True(reserved);
        Assert.NotNull(reservation);
        Assert.Equal(5, world.Resources.Total(ResourceType.Wood));
        Assert.Equal(1, world.Resources.Available(ResourceType.Wood));
        Assert.False(world.Resources.TryConsume(ResourceType.Wood, 2));

        Assert.True(world.Resources.Commit(reservation!.Id));
        Assert.Equal(1, world.Resources.Total(ResourceType.Wood));
        Assert.Equal(1, world.Resources.Available(ResourceType.Wood));
        Assert.Empty(world.Resources.Reservations);
    }

    [Fact]
    public void Reservation_ReleaseMakesStockAvailableAgain()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 4);
        world.Resources.TryReserve(ResourceType.Wood, 3,
            new ResourceReservationOwner(ResourceReservationOwnerKind.ConstructionProject, 9),
            out ResourceReservation? reservation);

        Assert.True(world.Resources.Release(reservation!.Id));
        Assert.Equal(4, world.Resources.Available(ResourceType.Wood));
    }

    [Fact]
    public void Reservation_CanTransferOwnershipWithoutMovingOrReleasingGoods()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 4);
        world.Resources.TryReserve(ResourceType.Wood, 3,
            new ResourceReservationOwner(ResourceReservationOwnerKind.ConstructionProject, 9),
            out ResourceReservation? reservation);
        var expedition = new ResourceReservationOwner(ResourceReservationOwnerKind.Expedition, 21);

        Assert.True(world.Resources.TransferReservation(reservation!.Id, expedition));

        Assert.Equal(expedition, Assert.Single(world.Resources.Reservations).Owner);
        Assert.Equal(1, world.Resources.Available(ResourceType.Wood));
        Assert.Equal(4, world.Resources.Total(ResourceType.Wood));
    }

    [Fact]
    public void Validate_RejectsOrphanExpeditionReservation()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.ResourceReservations.Add(new ResourceReservationSave
        {
            Id = save.ResourceReservations.Count + 1,
            Resource = ResourceType.Iron.ToString(),
            Amount = 1,
            OwnerKind = ResourceReservationOwnerKind.Expedition.ToString(),
            OwnerEntityId = 9999,
        });

        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void StartExpedition_ReservesSuppliesAndCommitsOnReturn()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);

        Citizen hero = world.Hero!;
        var request = ExpeditionRequest.Reconnaissance(hero.Id);
        ExpeditionStartResult result = world.StartExpedition(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, world.Resources.Available(ResourceType.Wood));
        for (int i = 0; i < request.DurationTicks; i++)
        {
            world.AdvanceWorldTick();
        }

        Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];
        Assert.Equal(ExpeditionStatus.Returned, expedition.Status);
        Assert.Empty(world.Resources.Reservations);
        Assert.Equal(1, world.Resources.Available(ResourceType.Stone));
    }

    [Fact]
    public void ActiveExpedition_RemovesLeaderFromCityWorkAndMacroStage()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        Citizen hero = world.Hero!;

        ExpeditionStartResult result =
            world.StartExpedition(ExpeditionRequest.Reconnaissance(hero.Id));

        Assert.True(result.IsSuccess);
        Assert.True(world.IsCitizenOnActiveExpedition(hero.Id));
        Assert.DoesNotContain(world.AvailableCitizens(), citizen => citizen.Id == hero.Id);
        Assert.Equal(0, world.GatherWood(new BuildingId(100), unitId: 0, amount: 1));

        CityMacroSnapshot snapshot = CityMacroSnapshot.From(world);
        CityMacroSnapshot.CitizenItem projectedHero = Assert.Single(snapshot.Citizens);
        Assert.True(projectedHero.IsOnExpedition);
        Assert.False(projectedHero.IsAvailable);
    }

    [Fact]
    public void ActiveExpedition_RoundTripsAndCancellationRestoresLeaderAvailability()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        CitizenId heroId = world.Hero!.Id;
        ExpeditionStartResult started =
            world.StartExpedition(ExpeditionRequest.Reconnaissance(heroId));
        Assert.True(started.IsSuccess);

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.DeserializeFromJson(
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));

        Assert.True(restored.IsCitizenOnActiveExpedition(heroId));
        Assert.Equal(CitizenCommitmentKind.Expedition, restored.Hero!.Commitment.Kind);
        Assert.Equal(
            started.ExpeditionId.GetValueOrDefault().Value,
            restored.Hero.Commitment.EntityId);
        Assert.Equal(CitizenAvailabilityReason.OnExpedition, restored.Hero.AvailabilityReason);
        Assert.True(restored.CancelExpedition(started.ExpeditionId!.Value));
        Assert.False(restored.IsCitizenOnActiveExpedition(heroId));
        Assert.Equal(CitizenCommitment.None, restored.Hero.Commitment);
        Assert.Contains(restored.AvailableCitizens(), citizen => citizen.Id == heroId);
        Assert.False(CityMacroSnapshot.From(restored).Citizens[0].IsOnExpedition);
    }

    [Fact]
    public void LegacyV14WithoutCommitmentFields_InfersActiveExpeditionCommitment()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        ExpeditionStartResult started = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(world.Hero!.Id));
        WorldSave legacyV14 = WorldPersistence.Capture(world);
        CitizenSave heroSave = Assert.Single(legacyV14.Citizens);
        heroSave.CommitmentKind = null;
        heroSave.CommitmentEntityId = null;

        WorldPersistence.Validate(legacyV14);
        CityWorld restored = WorldPersistence.FromSave(legacyV14);

        Assert.Equal(CitizenCommitmentKind.Expedition, restored.Hero!.Commitment.Kind);
        Assert.Equal(started.ExpeditionId!.Value.Value, restored.Hero.Commitment.EntityId);
        Assert.True(restored.IsCitizenOnActiveExpedition(restored.Hero.Id));
    }

    [Fact]
    public void Persistence_RestoresReservationsIronStockAndNextReservationId()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        int ironBefore = world.TotalStockOf(ResourceType.Iron);
        var owner = new ResourceReservationOwner(ResourceReservationOwnerKind.ConstructionProject, 9);
        Assert.True(world.Resources.TryReserve(ResourceType.Iron, 7, owner,
            out ResourceReservation? first));

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.DeserializeFromJson(
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));

        Assert.Equal(ironBefore, restored.TotalStockOf(ResourceType.Iron));
        ResourceReservation persisted = Assert.Single(restored.Resources.Reservations);
        Assert.Equal(first, persisted);
        Assert.Equal(ironBefore - 7, restored.Resources.Available(ResourceType.Iron));
        Assert.True(restored.Resources.TryReserve(ResourceType.Iron, 1, owner,
            out ResourceReservation? second));
        Assert.Equal(new ResourceReservationId(2), second!.Id);
    }

    [Fact]
    public void Validate_RejectsReservationsThatExceedStoredStock()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        WorldSave save = WorldPersistence.Capture(world);
        save.ResourceReservations.Add(new ResourceReservationSave
        {
            Id = 1,
            Resource = ResourceType.Wood.ToString(),
            Amount = 3,
            OwnerKind = ResourceReservationOwnerKind.Expedition.ToString(),
            OwnerEntityId = 4,
        });

        Assert.Throws<System.InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void MigrateV5ToV6_AddsEmptyReservationCollection()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Version = 5;
        save.ResourceReservations = null!;

        WorldSave migrated = WorldPersistence.MigrateV5ToV6(save);

        Assert.Equal(6, migrated.Version);
        Assert.Empty(migrated.ResourceReservations);
    }
}
