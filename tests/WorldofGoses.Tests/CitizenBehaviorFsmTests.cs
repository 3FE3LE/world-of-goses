using WorldofGoses.Domain;
using Xunit;
using static WorldofGoses.Tests.TestHelpers;

namespace WorldofGoses.Tests;

public class CitizenBehaviorFsmTests
{
    [Fact]
    public void Constructor_DefaultsToIdle()
    {
        var c = NewCitizen(1);
        Assert.Equal(CitizenBehaviorState.Idle, c.Behavior);
    }

    [Fact]
    public void SetLocation_AtWork_TransitionsToWorking()
    {
        var c = NewCitizen(1);
        c.AssignTo(new BuildingId(1));
        c.SetLocation(CitizenLocation.AtWork);
        Assert.Equal(CitizenBehaviorState.Working, c.Behavior);
    }

    [Fact]
    public void SetLocation_AtHome_WhileAssigned_TransitionsToResting()
    {
        var c = NewCitizen(1);
        c.AssignTo(new BuildingId(1));
        c.SetLocation(CitizenLocation.AtWork);
        c.SetLocation(CitizenLocation.AtHome);
        Assert.Equal(CitizenBehaviorState.Resting, c.Behavior);
    }

    [Fact]
    public void SetLocation_AtHome_Unassigned_StaysIdle()
    {
        var c = NewCitizen(1);
        c.SetLocation(CitizenLocation.AtHome);
        Assert.Equal(CitizenBehaviorState.Idle, c.Behavior);
    }

    [Fact]
    public void ConsumeStamina_ToZero_WhileWorking_TransitionsToInjured()
    {
        var c = NewCitizen(1);
        c.AssignTo(new BuildingId(1));
        c.SetLocation(CitizenLocation.AtWork);
        c.ConsumeStamina(c.MaxStamina);
        Assert.Equal(0, c.CurrentStamina);
        Assert.Equal(CitizenBehaviorState.Injured, c.Behavior);
    }

    [Fact]
    public void RestoreStamina_FromInjured_TransitionsToResting()
    {
        var c = NewCitizen(1);
        c.AssignTo(new BuildingId(1));
        c.SetLocation(CitizenLocation.AtWork);
        c.ConsumeStamina(c.MaxStamina);
        c.RestoreStamina(1);
        Assert.Equal(CitizenBehaviorState.Resting, c.Behavior);
    }

    [Fact]
    public void RestoreStamina_WhileWorking_DoesNotDemoteToResting()
    {
        // Regression guard: Working -> Resting IS a documented transition
        // (for the "day ends" trigger), so a naive unconditional
        // TryTransition on every regen tick would wrongly demote an
        // actively-working, undamaged citizen. RestoreStamina must only
        // drive the FSM when leaving Injured.
        var c = NewCitizen(1);
        c.AssignTo(new BuildingId(1));
        c.SetLocation(CitizenLocation.AtWork);
        c.ConsumeStamina(10);
        c.RestoreStamina(5);
        Assert.Equal(CitizenBehaviorState.Working, c.Behavior);
    }

    [Fact]
    public void ConsumeStamina_ToZero_WhileIdle_DoesNotChangeBehavior()
    {
        // Idle -> Injured is not a documented transition; the FSM
        // rejects it rather than throwing, leaving Behavior unchanged.
        var c = NewCitizen(1);
        c.ConsumeStamina(c.MaxStamina);
        Assert.Equal(CitizenBehaviorState.Idle, c.Behavior);
    }

    [Fact]
    public void StartExpedition_TransitionsHeroToOnExpedition()
    {
        CityWorld world = NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        Citizen hero = world.Hero!;
        Assert.Equal(CitizenBehaviorState.Idle, hero.Behavior);

        ExpeditionStartResult result = world.StartExpedition(ExpeditionRequest.Reconnaissance(hero.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(CitizenBehaviorState.OnExpedition, hero.Behavior);
    }

    [Fact]
    public void CompletedExpedition_ReturnsHeroToIdle()
    {
        CityWorld world = NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        Citizen hero = world.Hero!;
        var request = ExpeditionRequest.Reconnaissance(hero.Id);
        world.StartExpedition(request);

        for (int i = 0; i < request.DurationTicks; i++) world.AdvanceWorldTick();

        Assert.Equal(CitizenBehaviorState.Idle, hero.Behavior);
    }

    [Fact]
    public void CancelledExpedition_ReturnsHeroToIdle()
    {
        CityWorld world = NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        Citizen hero = world.Hero!;
        ExpeditionStartResult started = world.StartExpedition(ExpeditionRequest.Reconnaissance(hero.Id));

        Assert.True(world.CancelExpedition(started.ExpeditionId!.Value));

        Assert.Equal(CitizenBehaviorState.Idle, hero.Behavior);
    }
}
