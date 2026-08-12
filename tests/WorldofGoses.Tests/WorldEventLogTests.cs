using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using WorldofGoses.Presentation;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Tests for the event-log slice. Covers <see cref="WorldEventLog"/>
/// in isolation, integration with <see cref="CityWorld.AdvanceWorldTick"/>,
/// and the aggregate derivation in <see cref="OfflineProgression.ApplyAll"/>.
/// </summary>
public class WorldEventLogTests
{
    // ---------------- WorldEventLog ----------------

    [Fact]
    public void WorldEventLog_Record_AssignsSequentialIds()
    {
        var log = new WorldEventLog();
        var subject = WorldEventSubject.Building(new BuildingId(7), "Quarry");
        var first = log.Record(1, WorldEventKind.StockProduced, subject, 5);
        var second = log.Record(2, WorldEventKind.StockCapped, subject);

        Assert.Equal(new WorldEventId(1), first.Id);
        Assert.Equal(new WorldEventId(2), second.Id);
        Assert.Equal(2, log.Events.Count);
    }

    [Fact]
    public void WorldEventSubject_DistinguishesEntitiesWithTheSameDisplayName()
    {
        var first = WorldEventSubject.Building(new BuildingId(1), "Quarry");
        var second = WorldEventSubject.Building(new BuildingId(2), "Quarry");

        Assert.NotEqual(first, second);
        Assert.Equal(first.DisplayName, second.DisplayName);
    }

    [Fact]
    public void WorldEventSubject_KeepsIdentityWhenCapturedNameChanges()
    {
        var beforeRename = WorldEventSubject.Building(new BuildingId(1), "Old quarry");
        var afterRename = WorldEventSubject.Building(new BuildingId(1), "New quarry");

        Assert.Equal(beforeRename.Kind, afterRename.Kind);
        Assert.Equal(beforeRename.EntityId, afterRename.EntityId);
        Assert.NotEqual(beforeRename.DisplayName, afterRename.DisplayName);
    }

    [Fact]
    public void WorldEventLog_Clear_ResetsState()
    {
        var log = new WorldEventLog();
        log.Record(1, WorldEventKind.DayBegan, WorldEventSubject.World("Sun"));
        log.Record(2, WorldEventKind.NightBegan, WorldEventSubject.World("Sun"));

        log.Clear();

        Assert.Empty(log.Events);
        // Next id must restart at 1 so post-restore events keep a
        // deterministic numbering within the new world session.
        var fresh = log.Record(3, WorldEventKind.StockProduced,
            WorldEventSubject.Building(new BuildingId(3), "Farm"), 2);
        Assert.Equal(new WorldEventId(1), fresh.Id);
    }

    [Fact]
    public void WorldEventLog_Record_BuildsHumanSummary()
    {
        var log = new WorldEventLog();
        var evt = log.Record(7, WorldEventKind.StockProduced,
            WorldEventSubject.Building(new BuildingId(7), "Quarry"), 4);
        Assert.Equal("Quarry produced +4", WorldEventTextFormatter.Format(evt));
    }

    [Fact]
    public void WorldEventLog_Record_DayNightSummaries()
    {
        var log = new WorldEventLog();
        Assert.Equal("Sun rose — workers mobilised to their stations",
            WorldEventTextFormatter.Format(log.Record(1, WorldEventKind.DayBegan, WorldEventSubject.World("Sun"))));
        Assert.Equal("Sun set — workers returned home to rest",
            WorldEventTextFormatter.Format(log.Record(2, WorldEventKind.NightBegan, WorldEventSubject.World("Sun"))));
    }

    // ---------------- CityWorld integration ----------------

    [Fact]
    public void CityWorld_AdvanceWorldTick_RecordsProductionEvents()
    {
        var world = TestHelpers.NewProductionWorld();
        TestHelpers.AdvanceToNextProductionCycle(world);

        bool sawProduction = false;
        foreach (var evt in world.Log.Events)
        {
            if (evt.Kind == WorldEventKind.StockProduced)
            {
                sawProduction = true;
                Assert.True(evt.Amount > 0);
                Assert.False(string.IsNullOrEmpty(evt.SubjectName));
                break;
            }
        }
        Assert.True(sawProduction,
            "expected at least one StockProduced event in the first production cycle");
    }

    [Fact]
    public void CityWorld_AdvanceWorldTick_LogPreservesChronologicalOrder()
    {
        var world = TestHelpers.NewProductionWorld();
        for (int i = 0; i < 8; i++) world.AdvanceWorldTick();

        var log = world.Log.Events;
        for (int i = 1; i < log.Count; i++)
        {
            Assert.True(log[i].Tick >= log[i - 1].Tick,
                $"event {log[i].Id} (tick {log[i].Tick}) is out of order with previous (tick {log[i - 1].Tick})");
        }
    }

    [Fact]
    public void CityWorld_RecordsStockCappedOnlyOnTransitionToFull()
    {
        var world = TestHelpers.NewProductionWorld();
        for (int i = 0; i < 100; i++) world.AdvanceWorldTick();

        var cappedByBuilding = world.Log.Events
            .Where(evt => evt.Kind == WorldEventKind.StockCapped)
            .GroupBy(evt => evt.SubjectName);

        Assert.All(cappedByBuilding, group => Assert.Single(group));
    }

    [Fact]
    public void CityWorld_Restore_ClearsLog()
    {
        var world = TestHelpers.NewProductionWorld();
        TestHelpers.AdvanceToNextProductionCycle(world);
        Assert.NotEmpty(world.Log.Events);

        WorldPersistence.ApplyTo(world, WorldPersistence.Capture(world));

        Assert.Empty(world.Log.Events);
    }

    // ---------------- OfflineProgression derivation ----------------

    [Fact]
    public void OfflineProgression_ApplyAll_ReturnsEventsRecordedDuringBatch()
    {
        var world = TestHelpers.NewProductionWorld();
        int tickBefore = world.CurrentTick;
        var report = OfflineProgression.ApplyAll(world, ticksToApply: 12);

        Assert.True(report.HadProgression);
        Assert.NotEmpty(report.Events);

        // Every event in the report must have a tick that lies
        // within the simulated window. The window is relative to
        // the world's starting tick because the 2026-07-30 workday
        // shift moves the dawn from tick 0 to tick 1200.
        foreach (var evt in report.Events)
        {
            Assert.InRange(evt.Tick, tickBefore, tickBefore + 12);
        }
    }

    [Fact]
    public void OfflineProgression_ApplyAll_ClockOnlyAdvanceReportsElapsedTicks()
    {
        var world = new CityWorld();
        // No hero, no buildings, no projects → idle fast-forward.
        var report = OfflineProgression.ApplyAll(world, ticksToApply: 5);
        Assert.True(report.HadProgression);
        Assert.Equal(5, report.TicksApplied);
        Assert.Empty(report.Events);
    }

    [Fact]
    public void OfflineProgression_ApplyAll_SecondCallDoesNotReplayPreviousEvents()
    {
        var world = TestHelpers.NewProductionWorld();
        var first = OfflineProgression.ApplyAll(world, ticksToApply: CityEconomyRules.ProductionCycleTicks);
        Assert.NotEmpty(first.Events);

        var second = OfflineProgression.ApplyAll(world, ticksToApply: CityEconomyRules.ProductionCycleTicks);

        // The second batch should return only events recorded after
        // the first batch — never the events from the first call.
        foreach (var evt in second.Events)
        {
            Assert.True(evt.Tick > CityEconomyRules.ProductionCycleTicks,
                $"event at tick {evt.Tick} leaked from the first batch");
        }
    }

    [Fact]
    public void OfflineProgression_ApplyAll_DoesNotReplayPreexistingNonProductionEvent()
    {
        var world = TestHelpers.NewProductionWorld();
        world.Log.Record(world.CurrentTick, WorldEventKind.ForestDemolished,
            WorldEventSubject.Building(new BuildingId(99), "Old forest"));

        var report = OfflineProgression.ApplyAll(world, ticksToApply: 1);

        Assert.DoesNotContain(report.Events,
            evt => evt.Kind == WorldEventKind.ForestDemolished && evt.SubjectName == "Old forest");
    }

    [Fact]
    public void OfflineProgression_ApplyAll_StockEventsMatchStockAdded()
    {
        var world = TestHelpers.NewProductionWorld();
        var report = OfflineProgression.ApplyAll(world, ticksToApply: 12);
        int logged = 0;
        foreach (var evt in report.Events)
        {
            if (evt.Kind == WorldEventKind.StockProduced) logged += evt.Amount;
        }
        Assert.Equal(report.StockAdded, logged);
    }
}
