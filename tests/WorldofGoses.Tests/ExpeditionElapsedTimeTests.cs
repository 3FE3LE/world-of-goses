using System.Linq;
using WorldofGoses.Tests.Combat;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// An expedition's return is a projection, not a schedule. It is estimated from
/// distance and pace at dispatch, and what happens on the road moves it — so the
/// difference between the two is a fact the player can be shown rather than a
/// number nobody kept.
/// </summary>
public class ExpeditionElapsedTimeTests
{
    [Fact]
    public void ADispatchedExpeditionStartsOnItsEstimate()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        Expedition expedition = world.Expeditions[expeditionId];

        Assert.Equal(expedition.EstimatedEndTick, expedition.EndTick);
        Assert.Equal(0, expedition.EstimateDeltaTicks);
        Assert.Empty(expedition.TimeEvents);
    }

    /// <summary>
    /// The estimate itself comes from the trail's length at walking pace, so a
    /// longer trail projects a longer trip without anyone editing a duration.
    /// </summary>
    [Fact]
    public void TheEstimateIsTheTrailsLengthAtWalkingPace()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        Expedition expedition = world.Expeditions[expeditionId];

        Assert.Equal(
            (int)(ExpeditionTiming.SpiritTrailLengthPx / CityTravel.WalkPixelsPerTick),
            expedition.EstimatedEndTick - expedition.StartTick);
    }

    /// <summary>
    /// The symptom the whole model exists for: a fight is time not spent
    /// walking, so it lands on the return rather than vanishing.
    /// </summary>
    [Fact]
    public void AnEncounterChargesTheRoadForHowLongItTook()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        int estimated = world.Expeditions[expeditionId].EstimatedEndTick;

        TestHelpers.AdvanceUntilExpeditionSettles(world, expeditionId);
        Expedition expedition = world.Expeditions[expeditionId];

        ExpeditionTimeEvent encounter = Assert.Single(
            expedition.TimeEvents.Where(
                entry => entry.Kind == ExpeditionTimeEventKind.Encounter));
        Assert.True(encounter.Ticks > 0, "an encounter that took no time is not an encounter");
        Assert.Equal(estimated, expedition.EstimatedEndTick);
        Assert.Equal(encounter.Ticks, expedition.EstimateDeltaTicks);
        Assert.True(expedition.EndTick > expedition.EstimatedEndTick);
    }

    /// <summary>
    /// The ledger is what a return summary reads, so it has to survive the
    /// player closing the game mid-journey.
    /// </summary>
    [Fact]
    public void TheLedgerAndTheEstimateSurviveASaveLoad()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        TestHelpers.AdvanceUntilExpeditionSettles(world, expeditionId);
        Expedition before = world.Expeditions[expeditionId];

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(world));
        Expedition after = restored.Expeditions[expeditionId];

        Assert.Equal(before.EstimatedEndTick, after.EstimatedEndTick);
        Assert.Equal(before.EndTick, after.EndTick);
        Assert.Equal(before.EstimateDeltaTicks, after.EstimateDeltaTicks);
        Assert.Equal(
            before.TimeEvents.Select(entry => (entry.Kind, entry.Ticks, entry.AtTick)),
            after.TimeEvents.Select(entry => (entry.Kind, entry.Ticks, entry.AtTick)));
    }

    /// <summary>
    /// A v36 expedition could not be delayed, so its arrival was its estimate.
    /// Migrating must not invent a delta for a journey that never had one.
    /// </summary>
    [Fact]
    public void MigratingFromV36TreatsTheOldArrivalAsTheEstimate()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        WorldSave save = WorldPersistence.Capture(world);
        save.Version = 36;
        foreach (ExpeditionSave expedition in save.Expeditions)
        {
            expedition.EstimatedEndTick = 0;
            expedition.TimeEvents.Clear();
            expedition.EncounterStartedAtTick = null;
        }

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.MigrateToCurrent(save));
        Expedition migrated = restored.Expeditions[expeditionId];

        Assert.Equal(migrated.EndTick, migrated.EstimatedEndTick);
        Assert.Equal(0, migrated.EstimateDeltaTicks);
        Assert.Empty(migrated.TimeEvents);
    }
}
