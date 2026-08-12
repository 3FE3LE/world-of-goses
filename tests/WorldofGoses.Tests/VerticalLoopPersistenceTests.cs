using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Live-versus-offline equivalence across save/reload boundaries.
///
/// <para>The world advances while the game is closed, so the two ways time
/// can pass — tick by tick with the player watching, and in a catch-up jump
/// after the game reopens — have to land on the same city. These tests are
/// the only place that claim is checked end to end.</para>
///
/// <para>They did not check it. The <c>Advance</c> helper read
/// <c>if (offline) world.AdvanceWorldTick(); else world.AdvanceWorldTick();</c>
/// — both branches identical — so every "offline" segment was a live
/// advance and <see cref="WorldTimeAdvance"/> was never entered. The suite
/// reported equivalence between the canonical path and itself. GitHub #1.</para>
///
/// <para>Two things follow from fixing it. The offline segments now go
/// through <see cref="WorldTimeAdvance.Advance"/>, and the tests assert on
/// its <see cref="WorldTimeAdvance.Result"/> that ticks were actually
/// <em>batched</em> — the one behaviour a live advance can never produce, so
/// a regression that quietly reduced the catch-up path to a per-tick loop
/// fails here. And the comparison had to stop being byte equality over the
/// serialized save: batching legitimately emits fewer intermediate
/// mobilisation events than stepping, so the auto-incremented event IDs
/// diverge while the city does not. Equivalence is now asserted on
/// gameplay semantics, which is what the invariant was ever about.</para>
/// </summary>
public sealed class VerticalLoopPersistenceTests
{
    [Fact]
    public void Expedition_ReloadedAtEveryPhaseBoundary_ResolvesExactlyOnce()
    {
        CityWorld seed = TestHelpers.NewHeroWorld();
        seed.SeedStartingForests();
        seed.GatherWood(new BuildingId(100), 2);
        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(seed.Hero!.Id) with
        {
            DurationTicks = 40,
        };
        ExpeditionStartResult started = seed.StartExpedition(request);
        Assert.True(started.IsSuccess);
        ExpeditionId expeditionId = started.ExpeditionId!.Value;

        CityWorld uninterrupted = Reload(seed);
        CityWorld reloaded = Reload(seed);
        AdvanceLive(uninterrupted, request.DurationTicks);

        int totalBatched = 0;

        totalBatched += AdvanceOffline(reloaded, 10);
        Assert.Equal(ExpeditionPhase.Encounter, reloaded.Expeditions[expeditionId].Phase);
        reloaded = Reload(reloaded);

        totalBatched += AdvanceOffline(reloaded, 10);
        Assert.Contains(
            reloaded.Expeditions[expeditionId].Phase,
            new[] { ExpeditionPhase.Objective, ExpeditionPhase.Retreating });
        reloaded = Reload(reloaded);

        totalBatched += AdvanceOffline(reloaded, 10);
        Assert.Equal(ExpeditionPhase.Returning, reloaded.Expeditions[expeditionId].Phase);
        reloaded = Reload(reloaded);

        totalBatched += AdvanceOffline(reloaded, 10);
        Assert.Equal(ExpeditionPhase.Resolved, reloaded.Expeditions[expeditionId].Phase);

        // The proof that the offline branch was the offline branch. A world
        // with an away expedition and no work assignments is quiescent, so
        // the catch-up path must batch; if this is ever zero, the "offline"
        // advance degenerated into the live one and the equivalence claimed
        // below is vacuous.
        Assert.True(
            totalBatched > 0,
            "No tick was batched, so the offline catch-up path was never "
                + "genuinely exercised and the live/offline equivalence below "
                + "compares the canonical path against itself.");

        AssertSameCity(uninterrupted, reloaded);

        // Exactly-once resolution: four reloads across four phase boundaries
        // must not let the expedition pay out twice.
        Assert.Single(reloaded.Log.Events, evt =>
            evt.Kind is WorldEventKind.ExpeditionReturned
                or WorldEventKind.ExpeditionRetreated
                or WorldEventKind.ExpeditionFailed);
    }

    [Fact]
    public void Recovery_ReloadedHalfway_ConsumesAndCompletesExactlyOnce()
    {
        // Previously skipped: the assertion was byte equality over the
        // serialized save, which compared auto-incremented event IDs and so
        // broke whenever an unrelated change shifted how many setup events
        // fired. Unskipped by replacing that comparison with the semantics
        // it was standing in for — the wound clears once, the treatment is
        // paid for once, and the two worlds agree on state rather than on
        // ID arithmetic.
        CityWorld seed = TestHelpers.WorldWithHome();
        Citizen hero = seed.Hero!;
        hero.SetLocation(CitizenLocation.AtHome);
        WorldEvent woundEvent = seed.Log.Record(
            seed.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(hero.Id, hero.Name),
            (int)WoundSeverity.Moderate);
        hero.SustainWound(WoundSeverity.Moderate, woundEvent.Id);
        seed.DepositFood(WoundRules.ModerateFoodCost);
        int foodBeforeTreatment = seed.FoodStock;
        Assert.True(seed.TryBeginWoundRecovery(hero.Id).IsSuccess);
        Assert.Equal(foodBeforeTreatment - WoundRules.ModerateFoodCost, seed.FoodStock);

        CityWorld uninterrupted = Reload(seed);
        CityWorld reloaded = Reload(seed);
        int halfway = WoundRules.ModerateRecoveryTicks / 2;
        AdvanceLive(uninterrupted, WoundRules.ModerateRecoveryTicks);

        WorldTimeAdvance.Result firstCatchUp = WorldTimeAdvance.Advance(reloaded, halfway);
        Assert.True(firstCatchUp.BatchedTicks > 0);
        Assert.Equal(halfway, firstCatchUp.BatchedTicks + firstCatchUp.SteppedTicks);

        // The reload in the middle of recovery is the point: the remaining
        // ticks have to survive the save round-trip, not be recomputed.
        reloaded = Reload(reloaded);
        Assert.Equal(
            WoundRules.ModerateRecoveryTicks - halfway,
            reloaded.Hero!.Wound!.RecoveryTicksRemaining);

        WorldTimeAdvance.Result secondCatchUp = WorldTimeAdvance.Advance(
            reloaded,
            WoundRules.ModerateRecoveryTicks - halfway);
        Assert.True(secondCatchUp.BatchedTicks > 0);
        Assert.Equal(
            WoundRules.ModerateRecoveryTicks - halfway,
            secondCatchUp.BatchedTicks + secondCatchUp.SteppedTicks);

        AssertSameCity(uninterrupted, reloaded);

        Assert.Null(reloaded.Hero!.Wound);
        Assert.Single(reloaded.Log.Events, evt => evt.Kind == WorldEventKind.WoundRecoveryStarted);
        Assert.Single(reloaded.Log.Events, evt => evt.Kind == WorldEventKind.WoundRecoveryCompleted);
        // Resources consumed exactly once: the reload must not re-charge the
        // treatment, and the catch-up must not refund it.
        Assert.Equal(foodBeforeTreatment - WoundRules.ModerateFoodCost, reloaded.FoodStock);
    }

    /// <summary>
    /// The canonical live path: one tick at a time, exactly as a watching
    /// player experiences it.
    /// </summary>
    private static void AdvanceLive(CityWorld world, int tickCount)
    {
        for (int tick = 0; tick < tickCount; tick++)
        {
            world.AdvanceWorldTick();
        }
    }

    /// <summary>
    /// The canonical offline path: one catch-up call across the whole
    /// elapsed range, which batches quiescent stretches and only steps where
    /// the rules require it. Returns the number of ticks that were batched,
    /// so callers can prove the batching branch actually ran.
    /// </summary>
    private static int AdvanceOffline(CityWorld world, int tickCount)
    {
        WorldTimeAdvance.Result result = WorldTimeAdvance.Advance(world, tickCount);
        Assert.Equal(tickCount, result.TicksElapsed);
        Assert.Equal(tickCount, result.BatchedTicks + result.SteppedTicks);
        return result.BatchedTicks;
    }

    /// <summary>
    /// Gameplay-semantic equivalence between two worlds.
    ///
    /// <para>This replaces byte equality over the serialized save. That
    /// comparison was strictly stronger and strictly wrong: it also compared
    /// auto-incremented <see cref="WorldEvent.Id"/> values, which are an
    /// implementation detail of the log and legitimately differ between a
    /// batched catch-up and a stepped advance that reach the same city. What
    /// the invariant actually claims is that the player's city is the same —
    /// same clock, same stock, same people in the same states, same
    /// buildings, same expeditions, and the same sequence of things that
    /// happened.</para>
    /// </summary>
    private static void AssertSameCity(CityWorld expected, CityWorld actual)
    {
        Assert.Equal(expected.CurrentTick, actual.CurrentTick);

        foreach (ResourceType resource in Enum.GetValues<ResourceType>())
        {
            Assert.Equal(
                expected.Resources.Available(resource),
                actual.Resources.Available(resource));
        }
        Assert.Equal(expected.Tools.OrderBy(t => t), actual.Tools.OrderBy(t => t));

        Assert.Equal(
            expected.Citizens.Keys.OrderBy(id => id.Value),
            actual.Citizens.Keys.OrderBy(id => id.Value));
        foreach (CitizenId id in expected.Citizens.Keys.OrderBy(id => id.Value))
        {
            Assert.Equal(
                DescribeCitizen(expected.Citizens[id]),
                DescribeCitizen(actual.Citizens[id]));
        }

        Assert.Equal(
            expected.Buildings.Keys.OrderBy(id => id.Value),
            actual.Buildings.Keys.OrderBy(id => id.Value));
        foreach (BuildingId id in expected.Buildings.Keys.OrderBy(id => id.Value))
        {
            Assert.Equal(
                DescribeBuilding(expected.Buildings[id]),
                DescribeBuilding(actual.Buildings[id]));
        }

        Assert.Equal(
            expected.Projects.Keys.OrderBy(id => id.Value),
            actual.Projects.Keys.OrderBy(id => id.Value));
        foreach (BuildingId id in expected.Projects.Keys.OrderBy(id => id.Value))
        {
            ConstructionProject left = expected.Projects[id];
            ConstructionProject right = actual.Projects[id];
            Assert.Equal(left.Progress, right.Progress);
            Assert.Equal(left.RequiredWork, right.RequiredWork);
            Assert.Equal(left.Enabled, right.Enabled);
            Assert.Equal(
                left.AssignedCitizenIds.OrderBy(c => c.Value),
                right.AssignedCitizenIds.OrderBy(c => c.Value));
        }

        Assert.Equal(
            expected.Expeditions.Keys.OrderBy(id => id.Value),
            actual.Expeditions.Keys.OrderBy(id => id.Value));
        foreach (ExpeditionId id in expected.Expeditions.Keys.OrderBy(id => id.Value))
        {
            Assert.Equal(
                DescribeExpedition(expected.Expeditions[id]),
                DescribeExpedition(actual.Expeditions[id]));
        }

        // The durable chronicle, compared by what happened rather than by
        // which integers the log handed out.
        //
        // Filtered through WorldEventRetention because only significant
        // events are meant to survive a save: DayBegan/NightBegan are
        // routine clock ticks that SelectForPersistence deliberately drops.
        // The offline world reloads mid-run and the live one does not, so
        // an unfiltered comparison would report a divergence that is really
        // the retention policy working as designed — which is precisely the
        // kind of incidental difference that got the sibling test skipped
        // for byte-comparing event IDs. What must match is the history the
        // player can still read after reopening the game.
        Assert.Equal(
            DurableChronicle(expected),
            DurableChronicle(actual));
    }

    private static string DescribeCitizen(Citizen citizen) => string.Join(
        "|",
        citizen.Id.Value,
        citizen.Name,
        citizen.VitalStatus,
        citizen.CurrentLocation,
        citizen.Commitment.Kind,
        citizen.Commitment.EntityId?.ToString() ?? "-",
        citizen.IsReturningHome,
        citizen.ResumeWorkNotBeforeTick,
        citizen.CurrentStamina,
        citizen.MaxStamina,
        citizen.WellFedRemainingTicks,
        citizen.Wound is null
            ? "no-wound"
            : $"{citizen.Wound.Severity}:{citizen.Wound.RecoveryTicksRemaining}");

    private static string DescribeBuilding(Building building) => string.Join(
        "|",
        building.Id.Value,
        building.Kind,
        building.Stock,
        building.WoodReserve,
        building.ProductionEnabled,
        string.Join(",", building.AssignedCitizenIds.Select(c => c.Value).OrderBy(v => v)));

    private static string DescribeExpedition(Expedition expedition) => string.Join(
        "|",
        expedition.Id.Value,
        expedition.Status,
        expedition.Phase,
        expedition.ReturnedAmount?.ToString() ?? "-",
        expedition.Reward.Kind,
        expedition.EncounterOutcome?.ToString() ?? "-",
        expedition.RetreatPosture,
        expedition.ObjectiveReachedAtTick?.ToString() ?? "-",
        string.Join(",", expedition.MemberIds.Select(c => c.Value).OrderBy(v => v)));

    /// <summary>
    /// An event by its meaning. Deliberately excludes
    /// <see cref="WorldEvent.Id"/> and <see cref="WorldEvent.CauseEventId"/>,
    /// which are log-assigned counters, and the tick, which differs by
    /// construction between a batched and a stepped path that end at the
    /// same clock.
    /// </summary>
    private static string[] DurableChronicle(CityWorld world) => world.Log.Events
        .Where(evt => WorldEventRetention.IsSignificant(evt.Kind))
        .Select(DescribeEvent)
        .ToArray();

    private static string DescribeEvent(WorldEvent evt) => string.Join(
        "|",
        evt.Kind,
        evt.Subject.Kind,
        evt.Subject.EntityId?.ToString() ?? "-",
        evt.Subject.DisplayName,
        evt.Amount);

    private static CityWorld Reload(CityWorld world) => WorldPersistence.FromSave(
        WorldPersistence.DeserializeFromJson(Snapshot(world)));

    private static string Snapshot(CityWorld world) =>
        WorldPersistence.SerializeToJson(
            WorldPersistence.Capture(world, DateTimeOffset.UnixEpoch));
}
