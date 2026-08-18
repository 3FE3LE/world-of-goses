using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class MigrateV32ToV33Tests
{
    [Fact]
    public void MigrateV32ToV33_InitializesNoSyntheticCombatSession()
    {
        var save = new WorldSave
        {
            Version = 32,
            Expeditions = new List<ExpeditionSave>
            {
                new()
                {
                    Id = 7,
                    HasCombatSession = true,
                    CombatStepsAdvanced = 9,
                    CombatCommands = new List<CombatSessionCommandSave>
                    {
                        new() { BeforeStep = 1, Kind = "SetAutoSkills", Value = 0 },
                    },
                },
            },
        };

        WorldSave migrated = WorldPersistence.MigrateV32ToV33(save);

        Assert.Equal(33, migrated.Version);
        ExpeditionSave expedition = Assert.Single(migrated.Expeditions);
        Assert.False(expedition.HasCombatSession);
        Assert.Equal(0, expedition.CombatStepsAdvanced);
        Assert.Empty(expedition.CombatCommands);
    }

    [Fact]
    public void MigrateToCurrent_IncludesV32ToV33()
    {
        var save = new WorldSave { Version = 32 };

        Assert.Equal(WorldSave.CurrentVersion, WorldPersistence.MigrateToCurrent(save).Version);
    }

    [Fact]
    public void OutboundFounderSpiritTrail_ReachesCurrentUnarmedContractAndCompletes()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            global::WorldofGoses.Tests.Combat.ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        WorldSave v32 = WorldPersistence.Capture(world);
        v32.Version = 32;
        CitizenSave founder = v32.Citizens.Single(citizen => citizen.Id == world.Hero!.Id.Value);
        founder.EquipmentLoadout!.Weapon = null;

        WorldSave migrated = WorldPersistence.MigrateToCurrent(v32);
        CityWorld restored = WorldPersistence.FromSave(migrated);
        Assert.Null(restored.Hero!.EquipmentLoadout.Weapon);

        TestHelpers.AdvanceUntilExpeditionSettles(restored, expeditionId);

        Assert.Equal(ExpeditionStatus.Returned, restored.Expeditions[expeditionId].Status);
    }

    [Fact]
    public void LegacyTwoMemberSpiritTrail_KeepsAggregateResolverAfterMigration()
    {
        (CityWorld world, ResourceOpportunity opportunity) =
            global::WorldofGoses.Tests.Combat.ExpeditionCombatSessionIntegrationTests.PrepareSpiritTrailWorld();
        Citizen companion = TestHelpers.NewCitizen(2);
        world.RegisterCitizen(companion);
        Assert.True(world.TryIncorporateHero(companion.Id).IsSuccess);
        ExpeditionStartResult started = world.StartResourceExpedition(
            opportunity.Id,
            new[] { world.Hero!.Id },
            ExpeditionRetreatPosture.ContinueAfterSetback);
        Assert.True(started.IsSuccess);

        WorldSave v32 = WorldPersistence.Capture(world);
        v32.Version = 32;
        ExpeditionSave legacy = Assert.Single(v32.Expeditions);
        legacy.MemberCitizenIds.Add(companion.Id.Value);
        CitizenSave companionSave = v32.Citizens.Single(citizen => citizen.Id == companion.Id.Value);
        companionSave.CommitmentKind = CitizenCommitmentKind.Expedition.ToString();
        companionSave.CommitmentEntityId = legacy.Id;

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.MigrateToCurrent(v32));
        int encounterTicks = (legacy.EndTick - legacy.StartTick) / 4;
        WorldTimeAdvance.Advance(restored, encounterTicks);

        Expedition expedition = restored.Expeditions[started.ExpeditionId!.Value];
        Assert.Null(restored.GetCombatSessionSnapshot(expedition.Id));
        Assert.NotNull(expedition.EncounterOutcome);
    }

    [Fact]
    public void UnresolvedV32Encounter_StartsAtStepZeroInsteadOfBecomingStuck()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            global::WorldofGoses.Tests.Combat.ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        WorldSave v32 = WorldPersistence.Capture(world);
        v32.Version = 32;
        ExpeditionSave expedition = Assert.Single(v32.Expeditions);
        expedition.Phase = ExpeditionPhase.Encounter.ToString();
        expedition.EncounterOutcome = null;
        v32.Citizens.Single(citizen => citizen.Id == world.Hero!.Id.Value)
            .EquipmentLoadout!.Weapon = null;

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.MigrateToCurrent(v32));

        Assert.Equal(0, restored.GetCombatSessionSnapshot(expeditionId)!.Step);
        restored.AdvanceWorldTick();
        Assert.Equal(1, restored.GetCombatSessionSnapshot(expeditionId)!.Step);
    }

    [Fact]
    public void PostEncounterV32SpiritTrail_PreservesEmptyLoadoutAndAggregateReturn()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            global::WorldofGoses.Tests.Combat.ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        WorldSave v32 = WorldPersistence.Capture(world);
        v32.Version = 32;
        ExpeditionSave expedition = Assert.Single(v32.Expeditions);
        expedition.Phase = ExpeditionPhase.Returning.ToString();
        expedition.EncounterOutcome = ExpeditionEncounterOutcome.FullSuccess.ToString();
        CitizenSave founder = v32.Citizens.Single(citizen => citizen.Id == world.Hero!.Id.Value);
        founder.EquipmentLoadout!.Weapon = null;

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.MigrateToCurrent(v32));
        Assert.Null(restored.Hero!.EquipmentLoadout.Weapon);
        Assert.Null(restored.GetCombatSessionSnapshot(expeditionId));

        WorldTimeAdvance.Advance(
            restored,
            restored.Expeditions[expeditionId].EndTick - restored.CurrentTick);

        Assert.Equal(ExpeditionStatus.Returned, restored.Expeditions[expeditionId].Status);
        Assert.Null(restored.Hero.EquipmentLoadout.Weapon);
    }
}
