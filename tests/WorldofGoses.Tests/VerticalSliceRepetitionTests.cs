using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class VerticalSliceRepetitionTests
{
    [Fact]
    public void RecoveredCity_CanCompleteSecondExpeditionWithoutReset()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 4);
        world.DepositFood(30);
        CitizenId founderId = world.Hero!.Id;

        Expedition first = CompleteHealthyExpedition(world);
        Assert.Equal(ExpeditionStatus.Returned, first.Status);
        Assert.Equal(ParcelTerritoryState.Available, world.Parcels[first.TargetParcelId!.Value].TerritoryState);

        world = CityWorld.FromSave(WorldPersistence.Capture(world));
        Citizen founder = world.GetCitizen(founderId)!;
        int safety = GameClock.TicksPerInGameDay * 2;
        while (!founder.CanJoinExpedition && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }
        Assert.True(founder.CanJoinExpedition);

        Expedition second = CompleteHealthyExpedition(world);

        Assert.Equal(ExpeditionStatus.Returned, second.Status);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(founderId, world.Hero!.Id);
        Assert.Equal(2, world.Expeditions.Count);
        Assert.Equal(2, world.Log.Events.Count(evt => evt.Kind == WorldEventKind.ExpeditionReturned));
    }

    private static Expedition CompleteHealthyExpedition(CityWorld world)
    {
        Citizen hero = world.Hero!;
        hero.RestoreStamina(hero.MaxStamina);
        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(hero.Id) with
        {
            DurationTicks = 40,
        };
        ExpeditionStartResult result = world.StartExpedition(request);
        Assert.True(result.IsSuccess);

        WorldTimeAdvance.Advance(world, request.DurationTicks);

        return world.Expeditions[result.ExpeditionId!.Value];
    }
}
