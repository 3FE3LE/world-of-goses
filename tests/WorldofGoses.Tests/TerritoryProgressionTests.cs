using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class TerritoryProgressionTests
{
    [Fact]
    public void MigrateV18ToV19_AddsLockedTargetAndExplicitTerritoryState()
    {
        WorldSave legacy = WorldPersistence.Capture(TestHelpers.NewHeroWorld());
        legacy.Version = 18;
        legacy.Parcels.Clear();
        for (int id = 1; id <= 8; id++)
        {
            legacy.Parcels.Add(new ParcelSave
            {
                Id = id,
                LogicalColumn = (id - 1) % 4,
                LogicalRow = (id - 1) / 4,
                IsUnlocked = true,
                TerritoryState = null,
            });
        }

        WorldSave migrated = WorldPersistence.MigrateV18ToV19(legacy);

        // The single step under test lands on its own version, not on
        // whatever the current schema happens to be.
        Assert.Equal(19, migrated.Version);
        Assert.Equal(9, migrated.Parcels.Count);
        ParcelSave target = Assert.Single(migrated.Parcels, parcel => !parcel.IsUnlocked);
        Assert.Equal(ParcelTerritoryState.Locked.ToString(), target.TerritoryState);

        WorldSave current = WorldPersistence.MigrateToCurrent(migrated);
        Assert.Equal(WorldSave.CurrentVersion, current.Version);
        WorldPersistence.Validate(current);
    }

    [Fact]
    public void FirstSuccessfulReconnaissance_RevealsAvailableLotThroughCausalStates()
    {
        CityWorld world = NewTerritoryWorld();
        CityParcel target = world.NextTerritoryTarget!;
        Assert.Equal(ParcelTerritoryState.Locked, target.TerritoryState);

        CompleteSuccessfulReconnaissance(world);

        Assert.Equal(ParcelTerritoryState.Available, target.TerritoryState);
        WorldEvent[] advances = world.Log.Events
            .Where(evt => evt.Kind == WorldEventKind.TerritoryAdvanced)
            .ToArray();
        Assert.Equal(3, advances.Length);
        Assert.Equal(
            new[]
            {
                (int)ParcelTerritoryState.Reconnoitred,
                (int)ParcelTerritoryState.RouteSecured,
                (int)ParcelTerritoryState.Available,
            },
            advances.Select(evt => evt.Amount));
        Assert.All(advances, advanced =>
        {
            Assert.Equal(target.Id.Value, advanced.Subject.EntityId);
            Assert.NotNull(advanced.CauseEventId);
        });
        Assert.Contains(
            world.AvailableConstructionLots(),
            lot => lot.ParcelId == target.Id);
    }

    [Fact]
    public void FirstSuccessfulReconnaissance_MakesTargetLotPersistentlyAvailable()
    {
        CityWorld world = NewTerritoryWorld();
        CityParcel target = world.NextTerritoryTarget!;
        Assert.DoesNotContain(
            world.AvailableConstructionLots(),
            lot => lot.ParcelId == target.Id);

        CompleteSuccessfulReconnaissance(world);

        Assert.Equal(ParcelTerritoryState.Available, target.TerritoryState);
        Assert.Contains(
            world.AvailableConstructionLots(),
            lot => lot.ParcelId == target.Id);

        CityWorld restored = CityWorld.FromSave(WorldPersistence.Capture(world));
        CityParcel restoredTarget = restored.Parcels[target.Id];
        Assert.Equal(ParcelTerritoryState.Available, restoredTarget.TerritoryState);
        Assert.Contains(
            restored.AvailableConstructionLots(),
            lot => lot.ParcelId == target.Id);
    }

    private static CityWorld NewTerritoryWorld()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 4);
        world.RegisterBuilding(TestHelpers.NewBuilding(
            id: new BuildingId(9001),
            kind: BuildingKind.Farm,
            producedCompetencyId: CompetencyId.Farming,
            producedResourceType: ResourceType.Food,
            storageCapacity: 100,
            displayName: "Territory test farm",
            resourceLabel: "Food",
            resourceUnit: "food"));
        world.DepositFood(30);
        return world;
    }

    private static void CompleteSuccessfulReconnaissance(CityWorld world)
    {
        Citizen hero = world.Hero!;
        world.DepositFood(10);
        int safety = GameClock.TicksPerInGameDay * 2;
        while (!hero.CanJoinExpedition && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }
        Assert.True(
            hero.CanJoinExpedition,
            $"Cannot rejoin: wound={hero.IsWounded}, commitment={hero.Commitment.Kind}, "
            + $"vital={hero.VitalStatus}, stamina={hero.CurrentStamina}, food={world.FoodStock}.");
        hero.RestoreStamina(hero.MaxStamina);
        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(hero.Id);
        ExpeditionStartResult result = world.StartExpedition(request);
        Assert.True(result.IsSuccess);
        for (int tick = 0; tick < request.DurationTicks; tick++)
        {
            world.AdvanceWorldTick();
        }
        Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];
        Assert.Equal(ExpeditionStatus.Returned, expedition.Status);
        Assert.NotEqual(ExpeditionEncounterOutcome.Setback, expedition.EncounterOutcome);
    }
}
