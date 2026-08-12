using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// An expedition is a real 1-2 citizen team, not a hero-only reservation.
/// Covers plan validation (missing/
/// duplicate/unavailable members, team size), atomic dispatch/rollback,
/// and that every member — not just the first — is removed from city
/// availability while away and restored on return/cancel.
/// </summary>
public class ExpeditionTeamTests
{
    [Fact]
    public void FirstLoopTemplates_LastFourSimulatedHours()
    {
        var memberId = new CitizenId(1);

        ExpeditionRequest reconnaissance = ExpeditionRequest.Reconnaissance(memberId);
        ExpeditionRequest communityContact = ExpeditionRequest.SeekProspect(memberId);

        Assert.Equal(600, ExpeditionRequest.FirstLoopDurationTicks);
        Assert.Equal(ExpeditionRequest.FirstLoopDurationTicks, reconnaissance.DurationTicks);
        Assert.Equal(ExpeditionRequest.FirstLoopDurationTicks, communityContact.DurationTicks);
        Assert.Equal(0, reconnaissance.DurationTicks % 4);
    }

    [Fact]
    public void StartExpedition_WithTwoAvailableCitizens_DispatchesBoth()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        world.Resources.DepositToCityInventory(ResourceType.Wood, 2);
        var memberA = new CitizenId(4);
        var memberB = new CitizenId(5);
        IncorporateHeroes(world, memberA, memberB);

        ExpeditionStartResult result = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(new[] { memberA, memberB }));

        Assert.True(result.IsSuccess);
        Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];
        Assert.Equal(new[] { memberA, memberB }, expedition.MemberIds);
        Assert.True(world.IsCitizenOnActiveExpedition(memberA));
        Assert.True(world.IsCitizenOnActiveExpedition(memberB));
        Assert.Equal(CitizenAvailabilityReason.OnExpedition, world.GetCitizen(memberA)!.AvailabilityReason);
        Assert.Equal(CitizenAvailabilityReason.OnExpedition, world.GetCitizen(memberB)!.AvailabilityReason);
    }

    [Fact]
    public void StartExpedition_RejectsDuplicateMember()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        var member = new CitizenId(4);

        ExpeditionStartResult result = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(new[] { member, member }));

        Assert.Equal(ExpeditionStartOutcome.DuplicateMember, result.Outcome);
        Assert.False(world.IsCitizenOnActiveExpedition(member));
    }

    [Fact]
    public void StartExpedition_RejectsUnknownMember()
    {
        CityWorld world = TestHelpers.NewProductionWorld();

        ExpeditionStartResult result = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(new[] { new CitizenId(999) }));

        Assert.Equal(ExpeditionStartOutcome.MemberNotFound, result.Outcome);
    }

    [Fact]
    public void StartExpedition_RejectsEmptyTeam()
    {
        CityWorld world = TestHelpers.NewProductionWorld();

        ExpeditionStartResult result = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(System.Array.Empty<CitizenId>()));

        Assert.Equal(ExpeditionStartOutcome.InvalidRequest, result.Outcome);
    }

    [Fact]
    public void StartExpedition_RejectsTeamLargerThanMax()
    {
        CityWorld world = TestHelpers.NewProductionWorld();

        ExpeditionStartResult result = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(
                new[] { new CitizenId(2), new CitizenId(4), new CitizenId(5) }));

        Assert.Equal(ExpeditionStartOutcome.InvalidRequest, result.Outcome);
    }

    [Fact]
    public void StartExpedition_WithOneUnavailableMember_RollsBackTheAvailableOneToo()
    {
        // Being merely assigned to a building (citizen 2, Quarry) is not
        // "unavailable" here — an expedition is allowed to interrupt a work
        // order (it is preserved and resumed on return). Recovery is the
        // genuine block. Citizen 5 is recovering; citizen 4 is free.
        // Dispatch must be all-or-nothing: citizen 4 must not end up
        // committed to an expedition that never started.
        CityWorld world = TestHelpers.NewProductionWorld();
        world.Resources.DepositToCityInventory(ResourceType.Wood, 2);
        var availableMember = new CitizenId(4);
        var recoveringMember = new CitizenId(5);
        IncorporateHeroes(world, availableMember, recoveringMember);
        Citizen recovering = world.GetCitizen(recoveringMember)!;
        recovering.ConsumeStamina(recovering.MaxStamina);
        Assert.True(recovering.BeginVitalRecovery(world.CurrentTick));

        ExpeditionStartResult result = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(new[] { availableMember, recoveringMember }));

        Assert.Equal(ExpeditionStartOutcome.MemberUnavailable, result.Outcome);
        Assert.True(world.GetCitizen(availableMember)!.IsAvailable);
        Assert.False(world.IsCitizenOnActiveExpedition(availableMember));
        Assert.Empty(world.Expeditions);
    }

    [Fact]
    public void CancelExpedition_ReturnsEveryTeamMember()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        world.Resources.DepositToCityInventory(ResourceType.Wood, 2);
        var memberA = new CitizenId(4);
        var memberB = new CitizenId(5);
        IncorporateHeroes(world, memberA, memberB);
        ExpeditionStartResult started = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(new[] { memberA, memberB }));

        Assert.True(world.CancelExpedition(started.ExpeditionId!.Value));

        Assert.True(world.GetCitizen(memberA)!.IsAvailable);
        Assert.True(world.GetCitizen(memberB)!.IsAvailable);
    }

    [Fact]
    public void CompletedExpedition_ReturnsEveryTeamMember()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        world.Resources.DepositToCityInventory(ResourceType.Wood, 2);
        var memberA = new CitizenId(4);
        var memberB = new CitizenId(5);
        IncorporateHeroes(world, memberA, memberB);
        var request = ExpeditionRequest.Reconnaissance(new[] { memberA, memberB });
        world.StartExpedition(request);

        for (int i = 0; i < request.DurationTicks; i++) world.AdvanceWorldTick();

        Assert.False(world.IsCitizenOnActiveExpedition(memberA));
        Assert.False(world.IsCitizenOnActiveExpedition(memberB));
    }

    [Fact]
    public void SavedAndRestoredTeamExpedition_KeepsBothMembersCommitted()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        world.Resources.DepositToCityInventory(ResourceType.Wood, 2);
        var memberA = new CitizenId(4);
        var memberB = new CitizenId(5);
        IncorporateHeroes(world, memberA, memberB);
        world.StartExpedition(ExpeditionRequest.Reconnaissance(new[] { memberA, memberB }));

        CityWorld restored = WorldPersistence.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(
                    WorldPersistence.Capture(world))));

        Expedition restoredExpedition = Assert.Single(restored.Expeditions.Values);
        Assert.Equal(new[] { memberA, memberB }, restoredExpedition.MemberIds);
        Assert.True(restored.IsCitizenOnActiveExpedition(memberA));
        Assert.True(restored.IsCitizenOnActiveExpedition(memberB));
    }

    private static void IncorporateHeroes(CityWorld world, params CitizenId[] citizenIds)
    {
        foreach (CitizenId citizenId in citizenIds)
        {
            Assert.True(world.TryIncorporateHero(citizenId).IsSuccess);
        }
    }
}
