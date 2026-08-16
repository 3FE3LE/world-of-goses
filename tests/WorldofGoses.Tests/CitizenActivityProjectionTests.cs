using System.Linq;
using WorldofGoses.Domain;
using Xunit;
using static WorldofGoses.Tests.TestHelpers;

namespace WorldofGoses.Tests;

/// <summary>
/// Replaces <c>CitizenBehaviorFsmTests</c>. Those tests asserted a second
/// authority — <c>CitizenBehaviorState</c>, a parallel enum driven by its own
/// <c>FiniteStateMachine</c> and mutated from <c>SetLocation</c>, the stamina
/// mutators and the expedition hooks. Every question they asked has a canonical
/// owner already: where the citizen is (<see cref="CitizenLocation"/>), what
/// they are committed to (<see cref="CitizenCommitment"/>), what they are meant
/// to be doing (<see cref="CitizenWorkOrder"/>), and how much they can exert
/// (stamina). The same behaviours are asserted here against those facts and
/// against the one derived activity projection,
/// <see cref="CitizenRoutineSnapshot"/>.
///
/// <para>
/// Two of the old tests are deliberately not carried over as written, because
/// what they guarded no longer exists rather than because coverage was dropped:
/// <c>RestoreStamina_WhileWorking_DoesNotDemoteToResting</c> and
/// <c>ConsumeStamina_ToZero_WhileIdle_DoesNotChangeBehavior</c> both guarded
/// the shadow enum against being driven wrong by a stamina change. Stamina no
/// longer drives any activity value, so
/// <see cref="StaminaNeverMovesTheActivityProjection"/> asserts the stronger
/// property directly. See <c>docs/engineering/state-authority.md</c>.
/// </para>
/// </summary>
public sealed class CitizenActivityProjectionTests
{
    [Fact]
    public void FreshCitizen_IsUncommittedAndAtHome()
    {
        Citizen citizen = NewCitizen(1);

        Assert.Equal(CitizenCommitmentKind.None, citizen.Commitment.Kind);
        Assert.Equal(CitizenLocation.AtHome, citizen.CurrentLocation);
        Assert.Null(citizen.WorkOrder);
        Assert.True(citizen.IsAvailable);
    }

    [Fact]
    public void CommitToBuilding_RecordsBothTheCommitmentAndTheStandingOrder()
    {
        Citizen citizen = NewCitizen(1);

        Assert.True(citizen.TryCommitToBuilding(new BuildingId(1)));

        Assert.Equal(CitizenCommitmentKind.BuildingWork, citizen.Commitment.Kind);
        Assert.Equal(1, citizen.Commitment.EntityId);
        Assert.Equal(new BuildingId(1), citizen.CurrentAssignment);
        Assert.False(citizen.IsAvailable);
    }

    [Fact]
    public void SetLocation_MovesLocationOnlyAndLeavesTheOrderIntact()
    {
        Citizen citizen = NewCitizen(1);
        citizen.TryCommitToBuilding(new BuildingId(1));

        citizen.SetLocation(CitizenLocation.AtWork);
        Assert.Equal(CitizenLocation.AtWork, citizen.CurrentLocation);
        Assert.Equal(new BuildingId(1), citizen.CurrentAssignment);

        citizen.SetLocation(CitizenLocation.AtHome);
        Assert.Equal(CitizenLocation.AtHome, citizen.CurrentLocation);
        Assert.Equal(new BuildingId(1), citizen.CurrentAssignment);
    }

    /// <summary>
    /// Leaving transit clears the transit metadata that only makes sense while
    /// in it, so no stale journey can be read back off a settled citizen.
    /// </summary>
    [Fact]
    public void LeavingTransit_ClearsTheTransitMetadata()
    {
        Citizen citizen = NewCitizen(1);
        citizen.TryCommitToBuilding(new BuildingId(1));

        citizen.BeginTravelToAssignment(currentTick: 40, CityEconomyRules.AbstractTravelTicks);
        Assert.Equal(CitizenLocation.InTransit, citizen.CurrentLocation);
        Assert.Equal(40, citizen.TransitStartedAtTick);
        Assert.False(citizen.IsReturningHome);
        Assert.Equal(40 + CityEconomyRules.AbstractTravelTicks, citizen.TravelArrivalTick);

        citizen.SetLocation(CitizenLocation.AtWork);

        Assert.Null(citizen.TransitStartedAtTick);
        Assert.False(citizen.IsReturningHome);
        Assert.Null(citizen.TravelArrivalTick);
    }

    [Fact]
    public void BeginTravelHome_MarksTheReturnDirection()
    {
        Citizen citizen = NewCitizen(1);
        citizen.TryCommitToBuilding(new BuildingId(1));
        citizen.SetLocation(CitizenLocation.AtWork);

        citizen.BeginTravelHome(currentTick: 90, CityEconomyRules.AbstractTravelTicks);

        Assert.Equal(CitizenLocation.InTransit, citizen.CurrentLocation);
        Assert.True(citizen.IsReturningHome);
        Assert.Equal(90, citizen.TransitStartedAtTick);
    }

    /// <summary>
    /// Stamina is a capacity, not an activity. Depleting it must not silently
    /// reclassify what the citizen is doing — the old shadow enum flipped to
    /// <c>Injured</c> here, which also collided with the real wound condition.
    /// </summary>
    [Fact]
    public void StaminaNeverMovesTheActivityProjection()
    {
        CityWorld world = NewProductionWorld();
        Citizen worker = world.Citizens.Values.First(
            citizen => citizen.CurrentAssignment is not null);
        CitizenRoutineSnapshot before = world.GetCitizenRoutine(worker.Id)!;

        worker.ConsumeStamina(worker.MaxStamina);
        Assert.Equal(0, worker.CurrentStamina);
        CitizenRoutineSnapshot drained = world.GetCitizenRoutine(worker.Id)!;

        worker.RestoreStamina(worker.MaxStamina);
        CitizenRoutineSnapshot restored = world.GetCitizenRoutine(worker.Id)!;

        Assert.Equal(before.Activity, drained.Activity);
        Assert.Equal(before.Activity, restored.Activity);
        Assert.False(worker.IsWounded);
    }

    [Fact]
    public void StartExpedition_MakesTheCommitmentTheOnlyRecordOfBeingAway()
    {
        CityWorld world = NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        Citizen hero = world.Hero!;
        Assert.Equal(CitizenCommitmentKind.None, hero.Commitment.Kind);

        ExpeditionStartResult result = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(hero.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(CitizenCommitmentKind.Expedition, hero.Commitment.Kind);
        Assert.Equal(result.ExpeditionId!.Value.Value, hero.Commitment.EntityId);
        Assert.Equal(CitizenAvailabilityReason.OnExpedition, hero.AvailabilityReason);
        Assert.Equal(
            CitizenRoutineActivity.OnExpedition,
            world.GetCitizenRoutine(hero.Id)!.Activity);
    }

    [Fact]
    public void CompletedExpedition_ReleasesTheCommitment()
    {
        CityWorld world = NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        Citizen hero = world.Hero!;
        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(hero.Id);
        world.StartExpedition(request);

        for (int tick = 0; tick < request.DurationTicks; tick++) world.AdvanceWorldTick();

        Assert.Equal(CitizenCommitmentKind.None, hero.Commitment.Kind);
        Assert.NotEqual(
            CitizenRoutineActivity.OnExpedition,
            world.GetCitizenRoutine(hero.Id)!.Activity);
    }

    [Fact]
    public void CancelledExpedition_ReleasesTheCommitment()
    {
        CityWorld world = NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        Citizen hero = world.Hero!;
        ExpeditionStartResult started = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(hero.Id));

        Assert.True(world.CancelExpedition(started.ExpeditionId!.Value));

        Assert.Equal(CitizenCommitmentKind.None, hero.Commitment.Kind);
        Assert.Equal(CitizenLocation.AtHome, hero.CurrentLocation);
        Assert.True(hero.IsAvailable);
    }
}
