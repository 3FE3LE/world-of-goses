using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using Xunit;

namespace WorldofGoses.Tests.Combat;

public sealed class CombatSessionTests
{
    [Fact]
    public void ResolveToEnd_AndIncrementalAdvance_AreEquivalent()
    {
        CombatSession direct = NewSession(seed: 41);
        CombatSession incremental = NewSession(seed: 41);

        Assert.Equal(CombatOutcome.PartyVictory, direct.ResolveToEnd());
        while (incremental.IsActive) incremental.Advance();

        Assert.Equal(direct.Outcome, incremental.Outcome);
        Assert.Equal(Signature(direct.Log), Signature(incremental.Log));
    }

    [Fact]
    public void BasicAttack_HappensWithoutInput_AndWhileAutoIsOff()
    {
        CombatSession session = NewSession();
        session.SetAutoSkillsEnabled(false);

        session.Advance();

        Assert.Contains(session.Log, entry =>
            entry.Kind == CombatLogKind.BasicAttackResolved && entry.ActorId == "founder");
        Assert.DoesNotContain(session.Log, entry =>
            entry.Kind == CombatLogKind.TechniqueResolved && entry.ActorId == "founder");
    }

    [Fact]
    public void AutoOn_SpendsTheSameActiveSkillPipeline()
    {
        CombatSession session = NewSession();

        session.Advance();

        CombatLogEntry active = Assert.Single(session.Log, entry =>
            entry.Kind == CombatLogKind.TechniqueResolved && entry.ActorId == "founder");
        Assert.NotNull(active.Resolution);
        Assert.Equal("test.active", active.Resolution!.TechniqueId);
    }

    [Fact]
    public void ManualSkill_UsesTheResolverAndStartsAuthoritativeCooldown()
    {
        CombatSession session = NewSession();
        session.SetAutoSkillsEnabled(false);

        Assert.True(session.TryActivateMemberSkill(0));
        session.Advance();

        CombatLogEntry active = Assert.Single(session.Log, entry =>
            entry.Kind == CombatLogKind.TechniqueResolved && entry.ActorId == "founder");
        Assert.NotNull(active.Resolution);
        CombatSkillState skill = Assert.Single(session.Snapshot().MemberSkills);
        Assert.False(skill.Ready);
        Assert.Equal(2, skill.Remaining);
        Assert.Equal(3, skill.Duration);
        Assert.False(session.TryActivateMemberSkill(0));
    }

    [Fact]
    public void ManualAndAuto_ProduceTheSameResolutionForTheSameSeed()
    {
        CombatSession automatic = NewSession(seed: 77);
        CombatSession manual = NewSession(seed: 77);
        manual.SetAutoSkillsEnabled(false);
        Assert.True(manual.TryActivateMemberSkill(0));

        automatic.Advance();
        manual.Advance();

        TechniqueResolution fromAuto = automatic.Log.Single(entry =>
            entry.Kind == CombatLogKind.TechniqueResolved && entry.ActorId == "founder").Resolution!;
        TechniqueResolution fromManual = manual.Log.Single(entry =>
            entry.Kind == CombatLogKind.TechniqueResolved && entry.ActorId == "founder").Resolution!;
        Assert.Equal(fromAuto with { AppliedStatuses = System.Array.Empty<StatusEffectId>() },
            fromManual with { AppliedStatuses = System.Array.Empty<StatusEffectId>() });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void LockedMemberSlots_AreLegalNoOps(int slotIndex)
    {
        CombatSession session = NewSession();

        Assert.False(session.TryActivateMemberSkill(slotIndex));
        Assert.Empty(session.Commands);
    }

    [Fact]
    public void Replay_RestoresAutoManualCooldownHealthAndLog()
    {
        CombatSession live = NewSession(seed: 123);
        live.SetAutoSkillsEnabled(false);
        Assert.True(live.TryActivateMemberSkill(0));
        live.Advance(2);

        CombatSession restored = CombatSession.Restore(
            NewSession(seed: 123),
            live.Step,
            live.Commands);

        CombatSessionSnapshot expected = live.Snapshot();
        CombatSessionSnapshot actual = restored.Snapshot();
        Assert.Equal(expected.Active, actual.Active);
        Assert.Equal(expected.AutoSkillsEnabled, actual.AutoSkillsEnabled);
        Assert.Equal(expected.Step, actual.Step);
        Assert.Equal(expected.Outcome, actual.Outcome);
        Assert.Equal(expected.EnemyCount, actual.EnemyCount);
        Assert.Equal(expected.Party, actual.Party);
        Assert.Equal(expected.Enemies, actual.Enemies);
        Assert.Equal(expected.MemberSkills, actual.MemberSkills);
        Assert.Equal(Signature(live.Log), Signature(restored.Log));
    }

    [Fact]
    public void AdvancingEquivalentWorldWork_NeverDuplicatesActions()
    {
        CombatSession normal = NewSession(seed: 9);
        CombatSession fastest = NewSession(seed: 9);

        normal.Advance(4);
        for (int tick = 0; tick < 4; tick++) fastest.Advance();

        Assert.Equal(normal.Step, fastest.Step);
        Assert.Equal(Signature(normal.Log), Signature(fastest.Log));
    }

    private static CombatSession NewSession(ulong seed = 7)
    {
        TechniqueDefinition active = CombatTestFactory.Technique(
            "test.active",
            physical: 0.7,
            elemental: 0.3,
            cooldown: 3);
        CombatantState founder = CombatTestFactory.Combatant(
            "founder",
            CombatSide.Party,
            maxHealth: 500,
            currentHealth: 500,
            physicalPower: 40,
            elementalPower: 20,
            attackSpeed: 2,
            techniques: new[] { active },
            citizenId: new CitizenId(1));
        CombatantState enemy = CombatTestFactory.Combatant(
            "enemy",
            CombatSide.Enemy,
            maxHealth: 260,
            currentHealth: 260,
            physicalPower: 10,
            elementalPower: 10,
            attackSpeed: 1,
            techniques: new[] { CombatTestFactory.Technique("enemy.active", cooldown: 4) });
        CombatEncounter encounter = CombatTestFactory.Encounter(
            new[] { founder },
            new[] { enemy },
            seed);
        return new CombatSession(encounter);
    }

    private static List<string> Signature(IReadOnlyList<CombatLogEntry> log) => log
        .Select(entry =>
            $"{entry.Step}:{entry.Kind}:{entry.ActorId}:{entry.TargetId}:"
            + $"{entry.Detail}:{entry.Resolution?.FinalResult:0.####}")
        .ToList();
}
