using System;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests.Combat;

public sealed class ExpeditionCombatSessionIntegrationTests
{
    [Fact]
    public void SpiritTrailDispatch_PersistsOneProvisionalFounderWeapon()
    {
        (CityWorld world, ExpeditionId expeditionId) = StartSpiritTrail();
        WeaponChannelProfile weapon = Assert.IsType<WeaponChannelProfile>(
            world.Hero!.EquipmentLoadout.Weapon);

        CityWorld restored = CityWorld.FromSave(WorldPersistence.Capture(world));

        Assert.Equal(weapon, restored.Hero!.EquipmentLoadout.Weapon);
        Assert.Equal(expeditionId, Assert.Single(restored.Expeditions).Key);
    }

    [Fact]
    public void CombatSession_IsWorldOwnedAndSurvivesRepeatedLiveProjections()
    {
        (CityWorld world, ExpeditionId expeditionId) = StartSpiritTrail();
        AdvanceToCombat(world, expeditionId);
        CombatSessionSnapshot before = Assert.IsType<CombatSessionSnapshot>(
            world.GetCombatSessionSnapshot(expeditionId));

        _ = ExpeditionLiveSnapshot.From(world, expeditionId);
        _ = ExpeditionLiveSnapshot.From(world, expeditionId);
        CombatSessionSnapshot after = Assert.IsType<CombatSessionSnapshot>(
            world.GetCombatSessionSnapshot(expeditionId));

        Assert.Equal(before.Step, after.Step);
        Assert.Equal(before.Log.Count, after.Log.Count);
        world.AdvanceWorldTick();
        Assert.Equal(before.Step + 1, world.GetCombatSessionSnapshot(expeditionId)!.Step);
    }

    [Fact]
    public void ActiveSession_SaveLoad_ReplaysTheSameStateAndOutcome()
    {
        (CityWorld live, ExpeditionId expeditionId) = StartSpiritTrail();
        AdvanceToCombat(live, expeditionId);
        Assert.True(live.SetCombatAutoSkillsEnabled(expeditionId, false));
        Assert.True(live.TryActivateMemberSkill(expeditionId, 0));
        live.AdvanceWorldTick();

        CityWorld restored = CityWorld.FromSave(WorldPersistence.Capture(live));
        AssertSessionEquivalent(
            live.GetCombatSessionSnapshot(expeditionId)!,
            restored.GetCombatSessionSnapshot(expeditionId)!);

        int safety = 64;
        while (live.Expeditions[expeditionId].EncounterOutcome is null && safety-- > 0)
        {
            live.AdvanceWorldTick();
            restored.AdvanceWorldTick();
        }

        Assert.True(safety > 0);
        Assert.Equal(
            live.Expeditions[expeditionId].EncounterOutcome,
            restored.Expeditions[expeditionId].EncounterOutcome);
        AssertSessionEquivalent(
            live.GetCombatSessionSnapshot(expeditionId)!,
            restored.GetCombatSessionSnapshot(expeditionId)!);
    }

    [Fact]
    public void WorldTimeAdvance_ActiveSession_MatchesCanonicalWorldTicks()
    {
        (CityWorld source, ExpeditionId expeditionId) = StartSpiritTrail();
        AdvanceToCombat(source, expeditionId);
        WorldSave snapshot = WorldPersistence.Capture(source);
        CityWorld live = CityWorld.FromSave(snapshot);
        CityWorld offline = CityWorld.FromSave(snapshot);

        const int observedTicks = 4;
        for (int tick = 0; tick < observedTicks; tick++) live.AdvanceWorldTick();
        WorldTimeAdvance.Advance(offline, observedTicks);

        AssertSessionEquivalent(
            live.GetCombatSessionSnapshot(expeditionId)!,
            offline.GetCombatSessionSnapshot(expeditionId)!);

        int remainingTicks = live.Expeditions[expeditionId].EndTick - live.CurrentTick;
        for (int tick = 0; tick < remainingTicks; tick++) live.AdvanceWorldTick();
        WorldTimeAdvance.Advance(offline, remainingTicks);

        Expedition liveExpedition = live.Expeditions[expeditionId];
        Expedition offlineExpedition = offline.Expeditions[expeditionId];
        Assert.Equal(liveExpedition.Status, offlineExpedition.Status);
        Assert.Equal(liveExpedition.Phase, offlineExpedition.Phase);
        Assert.Equal(liveExpedition.EncounterOutcome, offlineExpedition.EncounterOutcome);
        Assert.Equal(liveExpedition.ReturnedAmount, offlineExpedition.ReturnedAmount);
        Assert.Equal(
            live.Hero!.CurrentHealthAndCondition,
            offline.Hero!.CurrentHealthAndCondition);
    }

    [Fact]
    public void SpiritTrail_RejectsAnyTeamOtherThanFounderAlone()
    {
        (CityWorld world, ResourceOpportunity opportunity) = PrepareSpiritTrailWorld();
        Citizen companion = TestHelpers.NewCitizen(2);
        world.RegisterCitizen(companion);
        Assert.True(world.TryIncorporateHero(companion.Id).IsSuccess);

        ExpeditionStartResult duo = world.StartResourceExpedition(
            opportunity.Id,
            new[] { world.Hero!.Id, companion.Id },
            ExpeditionRetreatPosture.ContinueAfterSetback);
        ExpeditionStartResult companionOnly = world.StartResourceExpedition(
            opportunity.Id,
            new[] { companion.Id },
            ExpeditionRetreatPosture.ContinueAfterSetback);

        Assert.Equal(ExpeditionStartOutcome.InvalidRequest, duo.Outcome);
        Assert.Equal(ExpeditionStartOutcome.InvalidRequest, companionOnly.Outcome);
        Assert.Empty(world.Expeditions);
        Assert.Equal(ResourceOpportunityState.Available, opportunity.State);
    }

    [Fact]
    public void ArmedNonSpiritExpedition_KeepsLegacyAggregateEncounter()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.Resources.DepositToCityInventory(ResourceType.Wood, 2);
        ExpeditionCombatSessionFactory.EnsureProvisionalFounderWeapon(
            world.Hero!,
            new ExpeditionId(77),
            world.CurrentTick);
        Citizen founder = world.Hero!;
        ExpeditionStartResult started = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(founder.Id));
        Assert.True(started.IsSuccess, started.Outcome.ToString());

        int encounterTick = ExpeditionRequest.FirstLoopDurationTicks / 4;
        for (int tick = 0; tick < encounterTick; tick++) world.AdvanceWorldTick();

        Expedition expedition = world.Expeditions[started.ExpeditionId!.Value];
        Assert.Null(world.GetCombatSessionSnapshot(expedition.Id));
        Assert.NotNull(expedition.EncounterOutcome);
    }

    [Fact]
    public void CurrentFounderSpiritTrailSave_RejectsMissingWeaponAndOutcomeLessReturn()
    {
        (CityWorld world, _) = StartSpiritTrail();
        WorldSave missingWeapon = WorldPersistence.Capture(world);
        missingWeapon.Citizens.Single(citizen => citizen.Id == world.Hero!.Id.Value)
            .EquipmentLoadout!.Weapon = null;
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(missingWeapon));

        WorldSave invalidPhase = WorldPersistence.Capture(world);
        ExpeditionSave expedition = Assert.Single(invalidPhase.Expeditions);
        expedition.Phase = ExpeditionPhase.Returning.ToString();
        expedition.EncounterOutcome = null;
        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(invalidPhase));
    }

    [Fact]
    public void Restore_RejectsEncounterOutcomeThatDisagreesWithCombatReplay()
    {
        (CityWorld world, ExpeditionId expeditionId) = StartSpiritTrail();
        AdvanceToCombat(world, expeditionId);
        int safety = 64;
        while (world.Expeditions[expeditionId].EncounterOutcome is null && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }
        Assert.True(safety > 0);

        WorldSave save = WorldPersistence.Capture(world);
        ExpeditionSave expedition = Assert.Single(save.Expeditions);
        expedition.EncounterOutcome = expedition.EncounterOutcome ==
            ExpeditionEncounterOutcome.FullSuccess.ToString()
                ? ExpeditionEncounterOutcome.Setback.ToString()
                : ExpeditionEncounterOutcome.FullSuccess.ToString();

        CityWorld existing = TestHelpers.NewProductionWorld();
        string before = WorldPersistence.SerializeToJson(
            WorldPersistence.Capture(existing, DateTimeOffset.UnixEpoch));

        Assert.Throws<InvalidOperationException>(() => existing.Restore(save));

        string after = WorldPersistence.SerializeToJson(
            WorldPersistence.Capture(existing, DateTimeOffset.UnixEpoch));
        Assert.Equal(before, after);
    }

    private static void AssertSessionEquivalent(
        CombatSessionSnapshot expected,
        CombatSessionSnapshot actual)
    {
        Assert.Equal(expected.Active, actual.Active);
        Assert.Equal(expected.AutoSkillsEnabled, actual.AutoSkillsEnabled);
        Assert.Equal(expected.Step, actual.Step);
        Assert.Equal(expected.Outcome, actual.Outcome);
        Assert.Equal(expected.EnemyCount, actual.EnemyCount);
        Assert.Equal(expected.BattlefieldMinimumX, actual.BattlefieldMinimumX);
        Assert.Equal(expected.BattlefieldMaximumX, actual.BattlefieldMaximumX);
        Assert.Equal(expected.Party, actual.Party);
        Assert.Equal(expected.Enemies, actual.Enemies);
        Assert.Equal(expected.MemberSkills, actual.MemberSkills);
        Assert.Equal(
            expected.Log.Select(LogSignature),
            actual.Log.Select(LogSignature));
    }

    private static string LogSignature(CombatLogEntry entry) =>
        $"{entry.Step}:{entry.Kind}:{entry.ActorId}:{entry.TargetId}:"
        + $"{entry.Detail}:{entry.Resolution?.FinalResult:0.####}";

    private static void AdvanceToCombat(CityWorld world, ExpeditionId expeditionId)
    {
        int safety = 256;
        while (world.GetCombatSessionSnapshot(expeditionId) is null && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }
        Assert.True(safety > 0);
    }

    internal static (CityWorld World, ExpeditionId ExpeditionId) StartSpiritTrail()
    {
        (CityWorld world, ResourceOpportunity opportunity) = PrepareSpiritTrailWorld();
        ExpeditionStartResult result = world.StartResourceExpedition(
            opportunity.Id,
            new[] { world.Hero!.Id },
            ExpeditionRetreatPosture.ContinueAfterSetback);
        Assert.True(result.IsSuccess, result.Outcome.ToString());
        return (world, result.ExpeditionId!.Value);
    }

    internal static (CityWorld World, ResourceOpportunity Opportunity) PrepareSpiritTrailWorld()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        DriveNightToDawn(world);
        CompleteCache(world);
        world.Resources.DepositToCityInventory(ResourceType.Food, 4);
        ResourceOpportunity opportunity = world.ResourceOpportunities.Values.Single(
            item => item.Kind == ResourceOpportunityKind.SpiritTrailSearch);
        return (world, opportunity);
    }

    private static void DriveNightToDawn(CityWorld world)
    {
        FirstNightState night = world.FirstNight!;
        ConstructionProject? project = null;
        int safety = 32;
        while (night.Stage < FirstNightStage.Sleeping && safety-- > 0)
        {
            if (FirstNightRules.WaitsForModule(night.Stage))
            {
                FoundingSiteModule module = FirstNightRules.ModuleFor(night.Stage);
                if (project is null)
                {
                    DepositCost(world, module);
                    ConstructionAuthorizationResult authorized =
                        world.TryAuthorizeConstruction(ConstructionKind.FoundingSite);
                    Assert.True(authorized.IsSuccess, authorized.Outcome.ToString());
                    project = world.Projects.Values.Single();
                }
                else
                {
                    DepositCost(world, module);
                    ConstructionAuthorizationResult authorized =
                        world.TryAuthorizeFoundingSiteModule(project.Id, module);
                    Assert.True(authorized.IsSuccess, authorized.Outcome.ToString());
                }
                project.Progress = project.RequiredWork;
                world.AdvanceWorldTick();
            }
            else
            {
                Assert.True(world.TryCloseFirstNightDialogue(), $"Stalled at {night.Stage}.");
            }
        }
        Assert.True(world.TryCloseFirstNightDialogue());
    }

    private static void CompleteCache(CityWorld world)
    {
        ConstructionProject project = world.Projects.Values.Single();
        DepositCost(world, FoundingSiteModule.Cache);
        ConstructionAuthorizationResult authorized =
            world.TryAuthorizeFoundingSiteModule(project.Id, FoundingSiteModule.Cache);
        Assert.True(authorized.IsSuccess, authorized.Outcome.ToString());
        project.Progress = project.RequiredWork;
        world.AdvanceWorldTick();
    }

    private static void DepositCost(CityWorld world, FoundingSiteModule module)
    {
        foreach (RecipeInput input in FoundingSiteRules.InputsFor(module))
        {
            world.Resources.DepositToCityInventory(input.Resource, input.Amount);
        }
    }
}
