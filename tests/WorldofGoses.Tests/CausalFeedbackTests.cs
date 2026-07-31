using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Mechanical fixture for the M-25 causal-feedback todo. M-25 commits
/// the system to surface a feedback reaction for three high-impact
/// transitions: obra completada (ProjectCompleted/BuildingCreated),
/// regreso expedicionario (ExpeditionReturned) y llegada/aceptación
/// de citizen (MigrantArrived). The human signature of M-25 reads
/// "is the feedback big enough?"; this fixture proves the domain
/// fires the right causal events and that they survive the retention
/// filter so the player sees them in the Chronicle.
///
/// The Notifier/feedback-grande presentation cut is the second part
/// of M-25; it requires a real Godot window and lives outside the
/// mechanical scope.
/// </summary>
public sealed class CausalFeedbackTests
{
    [Fact]
    public void ConstructionCompletion_FiresProjectCompletedAndBuildingCreated()
    {
        CityWorld world = TestHelpers.NewConstructionWorld();
        // Fast-forward the in-flight Basic Shelter project so the
        // completion path runs and the two completion events land
        // in the log within the bounded significant-events window.
        BuildingId projectId = world.Projects.Values.First().Id;
        TestHelpers.FastForwardToCompletion(world, projectId);

        var log = world.Log.Events;
        Assert.Contains(log, evt => evt.Kind == WorldEventKind.ProjectCompleted);
        Assert.Contains(log, evt => evt.Kind == WorldEventKind.BuildingCreated);
    }

    [Fact]
    public void ConstructionCompletion_EventsSurviveSignificantFilter()
    {
        // The Chronicle and the offline report both call
        // WorldEventRetention.SelectForPersistence; the completion
        // events must survive that filter so the player can read
        // them after a save/reload cycle.
        CityWorld world = TestHelpers.NewConstructionWorld();
        BuildingId projectId = world.Projects.Values.First().Id;
        TestHelpers.FastForwardToCompletion(world, projectId);

        var persisted = WorldEventRetention.SelectForPersistence(world.Log.Events);
        Assert.Contains(persisted, evt => evt.Kind == WorldEventKind.ProjectCompleted);
        Assert.Contains(persisted, evt => evt.Kind == WorldEventKind.BuildingCreated);
    }

    [Fact]
    public void ConstructionCompletion_FormatterProducesNonEmptyText()
    {
        // The presentation layer needs a non-empty string for each
        // event kind so the feedback reads as more than a placeholder.
        // Without these, the project-complete feedback would silently
        // render an empty label.
        var evt = new WorldEvent(
            default,
            tick: 100,
            kind: WorldEventKind.ProjectCompleted,
            subject: WorldEventSubject.ConstructionProject(new BuildingId(1), "Basic Shelter"),
            amount: 0,
            causeEventId: null);

        string formatted = WorldofGoses.Presentation.WorldEventTextFormatter.Format(evt);

        Assert.False(string.IsNullOrWhiteSpace(formatted));
        Assert.NotEqual(evt.Kind.ToString(), formatted);
    }

    [Fact]
    public void ExpeditionReturn_FiresExpeditionReturnedEvent()
    {
        // Dispatch a Reconnaissance template via the existing helper
        // and advance through the resolved phase; the return path
        // must record the causal event. The duration is shortened so
        // the test does not depend on the long FirstLoopDurationTicks
        // default (mirrors VerticalSliceRepetitionTests' recipe).
        CityWorld world = TestHelpers.WorldWithHome();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 4);
        Citizen hero = world.Hero!;
        hero.RestoreStamina(hero.MaxStamina);

        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(hero.Id) with
        {
            DurationTicks = 40,
        };
        ExpeditionStartResult started = world.StartExpedition(request);
        Assert.True(started.IsSuccess, $"Expedition dispatch failed: {started.Outcome}");

        WorldTimeAdvance.Advance(world, request.DurationTicks);

        Assert.Contains(
            world.Log.Events,
            evt => evt.Kind == WorldEventKind.ExpeditionReturned);
    }

    [Fact]
    public void ExpeditionReturn_EventSurvivesSignificantFilter()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 4);
        Citizen hero = world.Hero!;
        hero.RestoreStamina(hero.MaxStamina);

        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(hero.Id) with
        {
            DurationTicks = 40,
        };
        ExpeditionStartResult started = world.StartExpedition(request);
        Assert.True(started.IsSuccess);

        WorldTimeAdvance.Advance(world, request.DurationTicks);

        var persisted = WorldEventRetention.SelectForPersistence(world.Log.Events);
        Assert.Contains(
            persisted,
            evt => evt.Kind == WorldEventKind.ExpeditionReturned);
    }

    [Fact]
    public void MigrantAcceptance_FiresMigrantArrivedEvent()
    {
        // The hosted prospect becomes PendingProspect; the acceptance
        // path is the one M-25 cares about, because that is when the
        // citizen becomes a real resident and the player's Chronicle
        // should record a MigrantArrived causal event.
        CityWorld world = TestHelpers.WorldWithHome();
        AddTownHall(world);

        Assert.Equal(
            CityWorld.MigrantOutcome.Success,
            world.TryHostExpeditionProspect("Inara"));
        Assert.NotNull(world.PendingProspect);

        CityWorld.MigrantResult acceptance = world.TryAcceptPendingProspect();
        Assert.True(acceptance.IsSuccess);

        Assert.Contains(
            world.Log.Events,
            evt => evt.Kind == WorldEventKind.MigrantArrived);
    }

    [Fact]
    public void MigrantArrived_EventSurvivesSignificantFilter()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        AddTownHall(world);

        Assert.Equal(
            CityWorld.MigrantOutcome.Success,
            world.TryHostExpeditionProspect("Inara"));
        Assert.True(world.TryAcceptPendingProspect().IsSuccess);

        var persisted = WorldEventRetention.SelectForPersistence(world.Log.Events);
        Assert.Contains(
            persisted,
            evt => evt.Kind == WorldEventKind.MigrantArrived);
    }

    private static void AddTownHall(CityWorld world)
    {
        world.RegisterBuilding(TestHelpers.NewBuilding(
            id: new BuildingId(51),
            kind: BuildingKind.TownHall,
            workerCapacity: 0,
            visualCapacity: 0,
            baseProductionPerWorker: 0,
            storageCapacity: 0,
            displayName: "Test Town Hall",
            resourceLabel: "Prospect",
            resourceUnit: "prospect"));
    }

    [Fact]
    public void CausalFeedbackEvents_AreAllMarkedSignificant()
    {
        // Lock the M-25 commitment: the three event kinds driving the
        // "feedback grande" must remain in the significant-events
        // filter. If a future refactor drops one, the player's
        // Chronicle would silently lose the transition it was
        // supposed to surface.
        Assert.True(WorldEventRetention.IsSignificant(WorldEventKind.ProjectCompleted));
        Assert.True(WorldEventRetention.IsSignificant(WorldEventKind.BuildingCreated));
        Assert.True(WorldEventRetention.IsSignificant(WorldEventKind.ExpeditionReturned));
        Assert.True(WorldEventRetention.IsSignificant(WorldEventKind.MigrantArrived));
    }
}