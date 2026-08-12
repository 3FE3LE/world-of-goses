using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class MigrateV33ToV34Tests
{
    [Fact]
    public void ActiveSpiritTrail_ReleasesReservationAndRemovesSyntheticWeapon()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            global::WorldofGoses.Tests.Combat.ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        WorldSave v33 = WorldPersistence.Capture(world);
        v33.Version = 33;
        ExpeditionSave expedition = Assert.Single(v33.Expeditions);
        expedition.EndTick = expedition.StartTick + 180;
        expedition.SupplyResource = ResourceType.Food.ToString();
        expedition.SupplyAmount = 1;
        expedition.ReservationId = 77;
        expedition.RewardKind = ExpeditionRewardKind.Supplies.ToString();
        expedition.RewardResource = ResourceType.Wood.ToString();
        expedition.RewardAmount = 8;
        expedition.SetbackReturn = 4;
        expedition.PartialReturn = 6;
        expedition.CarryCapacity = 8;
        v33.ResourceReservations.Add(new ResourceReservationSave
        {
            Id = 77,
            Resource = ResourceType.Food.ToString(),
            Amount = 1,
            OwnerKind = ResourceReservationOwnerKind.Expedition.ToString(),
            OwnerEntityId = expedition.Id,
        });
        WeaponChannelProfile baseline = ExpeditionCombatSessionFactory.OpeningBaselineFor(
            expeditionId,
            expedition.StartTick);
        v33.Citizens.Single(citizen => citizen.Id == world.Hero!.Id.Value)
            .EquipmentLoadout!.Weapon = new WeaponChannelProfileSave
            {
                Family = baseline.Family.ToString(),
                PhysicalTransfer = baseline.PhysicalTransfer,
                ElementalResonance = baseline.ElementalResonance,
            };

        WorldSave migrated = WorldPersistence.MigrateV33ToV34(v33);

        Assert.Equal(34, migrated.Version);
        Assert.Null(expedition.SupplyResource);
        Assert.Equal(0, expedition.SupplyAmount);
        Assert.Null(expedition.ReservationId);
        Assert.Equal(ExpeditionRewardKind.Discovery.ToString(), expedition.RewardKind);
        Assert.Null(expedition.RewardResource);
        Assert.Equal(0, expedition.RewardAmount);
        Assert.Equal(
            ExpeditionTiming.SpiritTrailDurationTicks,
            expedition.EndTick - expedition.StartTick);
        Assert.DoesNotContain(migrated.ResourceReservations, reservation => reservation.Id == 77);
        Assert.Null(migrated.Citizens.Single(citizen => citizen.Id == world.Hero!.Id.Value)
            .EquipmentLoadout!.Weapon);
        WorldPersistence.Validate(migrated);
    }

    [Fact]
    public void CurrentValidationRejectsFakeReservationForNoSupply()
    {
        (CityWorld world, _) =
            global::WorldofGoses.Tests.Combat.ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        WorldSave save = WorldPersistence.Capture(world);
        ExpeditionSave expedition = Assert.Single(save.Expeditions);
        expedition.ReservationId = 9;

        Assert.Throws<System.InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void ActiveEncounterMigration_ReplaysCommandsAndCooldownWithoutPersistentWeapon()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            global::WorldofGoses.Tests.Combat.ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        WorldSave legacySeed = WorldPersistence.Capture(world);
        Assert.Single(legacySeed.Expeditions).CombatRulesVersion =
            ExpeditionCombatSessionFactory.LegacyRulesVersion;
        world = WorldPersistence.FromSave(legacySeed);
        while (world.GetCombatSessionSnapshot(expeditionId) is null) world.AdvanceWorldTick();
        Assert.True(world.SetCombatAutoSkillsEnabled(expeditionId, false));
        Assert.True(world.TryActivateMemberSkill(expeditionId, 0));
        while (!world.GetCombatSessionSnapshot(expeditionId)!.MemberSkills.Any(skill =>
                   skill.Remaining > 0))
        {
            world.AdvanceWorldTick();
        }
        CombatSessionSnapshot expected = world.GetCombatSessionSnapshot(expeditionId)!;
        WorldSave v33 = WorldPersistence.Capture(world);
        v33.Version = 33;
        ExpeditionSave expedition = Assert.Single(v33.Expeditions);
        WeaponChannelProfile baseline = ExpeditionCombatSessionFactory.OpeningBaselineFor(
            expeditionId,
            expedition.StartTick);
        v33.Citizens.Single(citizen => citizen.Id == world.Hero!.Id.Value)
            .EquipmentLoadout!.Weapon = new WeaponChannelProfileSave
            {
                Family = baseline.Family.ToString(),
                PhysicalTransfer = baseline.PhysicalTransfer,
                ElementalResonance = baseline.ElementalResonance,
            };

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.MigrateToCurrent(v33));
        CombatSessionSnapshot actual = Assert.IsType<CombatSessionSnapshot>(
            restored.GetCombatSessionSnapshot(expeditionId));

        Assert.Null(restored.Hero!.EquipmentLoadout.Weapon);
        Assert.Equal(expected.Step, actual.Step);
        Assert.Equal(expected.AutoSkillsEnabled, actual.AutoSkillsEnabled);
        Assert.Equal(expected.Outcome, actual.Outcome);
        Assert.Equal(expected.Party, actual.Party);
        Assert.Equal(expected.Enemies, actual.Enemies);
        Assert.Equal(expected.MemberSkills, actual.MemberSkills);
        Assert.Equal(
            expected.Log.Select(entry => $"{entry.Step}:{entry.Kind}:{entry.ActorId}:{entry.TargetId}"),
            actual.Log.Select(entry => $"{entry.Step}:{entry.Kind}:{entry.ActorId}:{entry.TargetId}"));
    }
}
