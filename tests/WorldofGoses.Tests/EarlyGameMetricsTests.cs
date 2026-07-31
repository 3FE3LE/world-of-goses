using System.Collections.Generic;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Locks the EG-0 measurement contract
/// (`docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §15). These numbers
/// exist to decide whether the EG-A0 balance hypothesis survives contact with
/// a real run, so a measurement that silently drifts is worse than no
/// measurement at all: it would be used to approve numbers it never actually
/// observed.
/// </summary>
public sealed class EarlyGameMetricsTests
{
    [Fact]
    public void Gathering_CountsTowardGatheredNotConsumed()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();

        world.GatherWood(new BuildingId(100), 2);

        Assert.Equal(2, world.Metrics.Gathered[ResourceType.Wood]);
        Assert.False(world.Metrics.Consumed.ContainsKey(ResourceType.Wood));
    }

    [Fact]
    public void Spending_CountsTowardConsumed()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 4);

        Assert.True(world.Resources.TryConsume(ResourceType.Wood, 3));

        Assert.Equal(4, world.Metrics.Gathered[ResourceType.Wood]);
        Assert.Equal(3, world.Metrics.Consumed[ResourceType.Wood]);
    }

    [Fact]
    public void Reload_DoesNotBookRestoredStockAsFreshlyGathered()
    {
        // The regression that matters most. Restoring re-deposits every stored
        // resource through the same ledger gathering uses, so without an
        // explicit suspension a reload would count the player's whole
        // stockpile as newly gathered — and any future playtest of EG-1+
        // that asks for several relaunches would compound that error every
        // single time.
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 6);
        int gatheredBefore = world.Metrics.Gathered[ResourceType.Wood];

        WorldSave save = WorldPersistence.Capture(world);
        CityWorld reloaded = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(save)));

        Assert.Equal(gatheredBefore, reloaded.Metrics.Gathered[ResourceType.Wood]);

        // And the observer must be live again afterwards, or every
        // post-reload session would silently measure nothing.
        reloaded.GatherWood(new BuildingId(100), 2);
        Assert.Equal(
            gatheredBefore + 2,
            reloaded.Metrics.Gathered[ResourceType.Wood]);
    }

    [Fact]
    public void Roundtrip_PreservesEveryMeasuredQuantity()
    {
        var metrics = new EarlyGameMetrics();
        metrics.RecordGathered(ResourceType.Wood, 11);
        metrics.RecordConsumed(ResourceType.Food, 4);
        metrics.RecordFirstShelterCompleted(1234);
        metrics.RecordExpeditionDispatched(2000);
        metrics.RecordExpeditionAbsence(600, 2);
        metrics.SampleDawn(foodStock: 8, residentCount: 2, idleCitizenCount: 1);

        var restored = new EarlyGameMetrics();
        WorldPersistence.RestoreEarlyGameMetrics(
            restored,
            WorldPersistence.CaptureEarlyGameMetrics(metrics));

        Assert.Equal(1234, restored.FirstShelterCompletedAtTick);
        Assert.Equal(2000, restored.FirstExpeditionDispatchedAtTick);
        Assert.Equal(1, restored.ExpeditionsDispatched);
        Assert.Equal(1200, restored.ExpeditionAbsenceTicks);
        Assert.Equal(1, restored.DawnSamples);
        Assert.Equal(1, restored.IdleCitizenDays);
        Assert.Equal(2, restored.ObservedCitizenDays);
        Assert.Equal(40, restored.MinFoodHorizonTenths);
        Assert.Equal(11, restored.Gathered[ResourceType.Wood]);
        Assert.Equal(4, restored.Consumed[ResourceType.Food]);
    }

    [Fact]
    public void MigrateV19ToV20_StartsEmptyRatherThanInventingHistory()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        WorldSave save = WorldPersistence.Capture(world);
        save.Version = 19;
        save.EarlyGameMetrics = null;

        WorldSave migrated = WorldPersistence.MigrateV19ToV20(save);

        Assert.Equal(20, migrated.Version);
        Assert.NotNull(migrated.EarlyGameMetrics);
        // Zero samples is what marks a city whose opening was never measured.
        // Back-filling plausible numbers here would corrupt the dataset EG-0
        // exists to collect.
        Assert.Equal(0, migrated.EarlyGameMetrics!.DawnSamples);
        Assert.Null(migrated.EarlyGameMetrics.FirstShelterCompletedAtTick);
        Assert.Empty(migrated.EarlyGameMetrics.Gathered);
        // Roll forward through every subsequent migration so the assertion
        // is about the migration chain, not about which schema Validate
        // currently happens to accept. As of EG-1 the next hop is V21.
        WorldSave current = WorldPersistence.MigrateToCurrent(migrated);
        Assert.Equal(WorldSave.CurrentVersion, current.Version);
        WorldPersistence.Validate(current);
    }

    [Fact]
    public void FirstShelter_IsNotOverwrittenByALaterOne()
    {
        var metrics = new EarlyGameMetrics();

        metrics.RecordFirstShelterCompleted(500);
        metrics.RecordFirstShelterCompleted(90_000);

        Assert.Equal(500, metrics.FirstShelterCompletedAtTick);
    }

    [Fact]
    public void ExpeditionAbsence_ScalesWithTeamSize()
    {
        // A two-person sortie removes twice the labour a solo trip does; the
        // opportunity cost EG-0 reports has to reflect that.
        var solo = new EarlyGameMetrics();
        var pair = new EarlyGameMetrics();

        solo.RecordExpeditionAbsence(600, 1);
        pair.RecordExpeditionAbsence(600, 2);

        Assert.Equal(600, solo.ExpeditionAbsenceTicks);
        Assert.Equal(1200, pair.ExpeditionAbsenceTicks);
    }

    [Fact]
    public void FoodHorizon_TracksTheWorstMomentNotTheLatest()
    {
        var metrics = new EarlyGameMetrics();

        metrics.SampleDawn(foodStock: 10, residentCount: 1, idleCitizenCount: 0);
        metrics.SampleDawn(foodStock: 2, residentCount: 1, idleCitizenCount: 0);
        metrics.SampleDawn(foodStock: 9, residentCount: 1, idleCitizenCount: 0);

        // The tightest moment is what says whether the opening was ever
        // genuinely at risk; the final value would hide it entirely.
        Assert.Equal(20, metrics.MinFoodHorizonTenths);
        Assert.Equal(3, metrics.DawnSamples);
    }

    [Fact]
    public void FoodHorizon_IgnoresAnEmptyCityInsteadOfReportingStarvation()
    {
        var metrics = new EarlyGameMetrics();

        metrics.SampleDawn(foodStock: 0, residentCount: 0, idleCitizenCount: 0);

        Assert.Null(metrics.MinFoodHorizonTenths);
        Assert.Equal(1, metrics.DawnSamples);
    }

    [Fact]
    public void FoodHorizonAtFirstShelter_SnapshotsTheDawnAfterCompletion()
    {
        var metrics = new EarlyGameMetrics();

        // Before any shelter exists there is nothing to snapshot.
        metrics.SampleDawn(foodStock: 20, residentCount: 1, idleCitizenCount: 0);
        Assert.Null(metrics.FoodHorizonTenthsAtFirstShelter);

        metrics.RecordFirstShelterCompleted(3600);
        metrics.SampleDawn(foodStock: 5, residentCount: 1, idleCitizenCount: 0);
        metrics.SampleDawn(foodStock: 1, residentCount: 1, idleCitizenCount: 0);

        // The first dawn after completion, not the latest one.
        Assert.Equal(50, metrics.FoodHorizonTenthsAtFirstShelter);
    }

    [Fact]
    public void Report_NeverShowsZeroesAsIfTheyWereObservations()
    {
        // Rendering an unsampled city as "0.0 days of Food" would read as a
        // measured starving city and could justify a balance change nobody
        // ever observed.
        string report = EarlyGameMetricsReport.Format(new EarlyGameMetrics());

        Assert.Contains("No dawn has been sampled yet", report);
        Assert.DoesNotContain("Tightest moment", report);
    }

    [Fact]
    public void Report_TellsAYoungRunApartFromAnUnmeasurableOne()
    {
        // Both produce zero samples, and reading one as the other wastes a
        // whole measurement run: a young city just needs more play, a migrated
        // one will never fill in and has to be replaced by a clean slot.
        string young = EarlyGameMetricsReport.Format(
            new EarlyGameMetrics(),
            currentTick: GameClock.WorkdayStartTick - 364);
        string migrated = EarlyGameMetricsReport.Format(
            new EarlyGameMetrics(),
            currentTick: GameClock.TicksPerInGameDay * 30);

        Assert.Contains("has not reached its first dawn", young);
        Assert.Contains("Keep playing", young);
        Assert.DoesNotContain("clean slot", young);

        Assert.Contains("was migrated", migrated);
        Assert.Contains("clean slot", migrated);
    }

    [Fact]
    public void Report_StatesTheRawNumbersBehindEveryRatio()
    {
        var metrics = new EarlyGameMetrics();
        metrics.RecordFirstShelterCompleted(GameClock.TicksPerInGameDay * 2);
        metrics.RecordGathered(ResourceType.Wood, 14);
        metrics.SampleDawn(foodStock: 5, residentCount: 2, idleCitizenCount: 1);

        string report = EarlyGameMetricsReport.Format(metrics);

        Assert.Contains("2.0 in-game days", report);   // time to first shelter
        Assert.Contains("2.5", report);                 // 5 Food / 2 residents
        Assert.Contains("50.0%", report);               // 1 idle of 2 observed
        Assert.Contains("Wood", report);
    }

    [Fact]
    public void Restore_ToleratesResourceNamesThisBuildNoLongerKnows()
    {
        // A measurement is never worth failing a load over: an unknown
        // resource row is skipped, the city still opens.
        var save = new EarlyGameMetricsSave
        {
            DawnSamples = 2,
            Gathered = new Dictionary<string, int>
            {
                ["Wood"] = 5,
                ["Unobtainium"] = 99,
            },
        };
        var metrics = new EarlyGameMetrics();

        WorldPersistence.RestoreEarlyGameMetrics(metrics, save);

        Assert.Equal(5, metrics.Gathered[ResourceType.Wood]);
        Assert.Single(metrics.Gathered);
        Assert.Equal(2, metrics.DawnSamples);
    }
}
