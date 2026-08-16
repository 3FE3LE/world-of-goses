using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using Xunit;

namespace WorldofGoses.Tests.Combat;

/// <summary>
/// Roadmap Fase 3 and 6: statuses, automatic priority, cooldowns and determinism.
/// Every test here runs the engine with no scene, no node and no frame.
/// </summary>
public sealed class CombatEncounterTests
{
    [Fact]
    public void Stunning_IsAppliedThenExpires()
    {
        var resolver = new StatusResolver();
        StatusEffect stun = resolver.Create(StatusEffectId.Stunning, "a", "b", step: 0);
        IReadOnlyList<StatusEffect> statuses = resolver.Apply(new List<StatusEffect>(), stun);

        Assert.True(resolver.IsActive(statuses, StatusEffectId.Stunning));
        Assert.True(resolver.PreventsAction(statuses));

        for (int step = 0; step < CombatBalanceConfig.Default.StunningDurationSteps; step++)
        {
            statuses = resolver.Tick(statuses);
        }

        Assert.False(resolver.IsActive(statuses, StatusEffectId.Stunning));
        Assert.False(resolver.PreventsAction(statuses));
    }

    [Fact]
    public void Knockdown_IsAppliedExposesTheTargetThenExpires()
    {
        var resolver = new StatusResolver();
        StatusEffect knockdown = resolver.Create(StatusEffectId.Knockdown, "a", "b", step: 0);
        IReadOnlyList<StatusEffect> statuses = resolver.Apply(new List<StatusEffect>(), knockdown);

        Assert.True(resolver.PreventsAction(statuses));
        // Knockdown exposes both windows at once — prone guards nothing — and,
        // unlike the other two control effects, also takes the ground. The old
        // comment here said the model "has nowhere to move"; it has since gained
        // an authoritative PositionX, so it does.
        Assert.True(resolver.PreventsMovement(statuses));
        Assert.Equal(
            CombatBalanceConfig.Default.KnockdownMitigationScale,
            resolver.Modifiers(statuses).PhysicalMitigationScale);
        Assert.Equal(
            CombatBalanceConfig.Default.KnockdownMitigationScale,
            resolver.Modifiers(statuses).ElementalMitigationScale);

        for (int step = 0; step < CombatBalanceConfig.Default.KnockdownDurationSteps; step++)
        {
            statuses = resolver.Tick(statuses);
        }

        Assert.False(resolver.PreventsAction(statuses));
        Assert.Equal(StatusModifiers.None, resolver.Modifiers(statuses));
    }

    [Fact]
    public void Statuses_StackAndRefreshInsteadOfDuplicating()
    {
        var resolver = new StatusResolver();
        IReadOnlyList<StatusEffect> statuses = resolver.Apply(
            new List<StatusEffect>(),
            resolver.Create(StatusEffectId.Stunning, "a", "b", 0));

        statuses = resolver.Apply(statuses, resolver.Create(StatusEffectId.Stunning, "a", "b", 1));

        Assert.Single(statuses);
        Assert.Equal(2, statuses[0].Stacks);
    }

    [Fact]
    public void StunningInCombat_CostsTheTargetItsAction()
    {
        TechniqueDefinition stunning = CombatTestFactory.Technique(
            "test.stun", appliesStatus: StatusEffectId.Stunning, cooldown: 10);
        // The stunner is faster, so it lands the stun before the victim can act.
        CombatantState stunner = CombatTestFactory.AttackerWith(
            "stunner", CombatSide.Party, stunning, attackSpeed: 2.0);
        CombatantState victim = CombatTestFactory.AttackerWith(
            "victim", CombatSide.Enemy, CombatTestFactory.Technique("test.hit"),
            attackSpeed: 1.0, maxHealth: 5000, currentHealth: 5000);

        CombatEncounter encounter = CombatTestFactory.Encounter(
            new[] { stunner }, new[] { victim });
        encounter.Resolve();

        Assert.Contains(encounter.Log, entry =>
            entry.Kind == CombatLogKind.StatusApplied
            && entry.Detail == nameof(StatusEffectId.Stunning));
        Assert.Contains(encounter.Log, entry =>
            entry.Kind == CombatLogKind.ActionPrevented && entry.ActorId == "victim");
    }

    [Fact]
    public void Cooldown_AndActivationTime_GateReuse()
    {
        TechniqueDefinition slow = CombatTestFactory.Technique(
            "test.slow", cooldown: 3, activationTime: 1);
        CombatantState attacker = CombatTestFactory.AttackerWith("a", CombatSide.Party, slow);
        CombatantState dummy = CombatTestFactory.Dummy("d", CombatSide.Enemy, maxHealth: 100000);

        CombatEncounter encounter = CombatTestFactory.Encounter(new[] { attacker }, new[] { dummy });
        encounter.Resolve();

        List<CombatLogEntry> uses = encounter.Log
            .Where(entry => entry.Kind == CombatLogKind.TechniqueResolved)
            .ToList();

        Assert.True(uses.Count > 1);
        // Cooldown 3 + activation 1 means one use every four steps.
        for (int index = 1; index < uses.Count; index++)
        {
            Assert.Equal(4, uses[index].Step - uses[index - 1].Step);
        }
    }

    [Fact]
    public void AutomaticPriority_PrefersThePlayersOrderedList()
    {
        TechniqueDefinition first = CombatTestFactory.Technique("test.first", cooldown: 50);
        TechniqueDefinition second = CombatTestFactory.Technique("test.second", cooldown: 50);
        CombatantState attacker = CombatTestFactory.Combatant(
            "a", CombatSide.Party, techniques: new[] { first, second });
        CombatantState dummy = CombatTestFactory.Dummy("d", CombatSide.Enemy);

        var plans = new Dictionary<string, CombatantPlan>
        {
            // The player asks for the second technique first.
            ["a"] = new CombatantPlan(0, new[] { "test.second", "test.first" }, null, false),
        };
        CombatEncounter encounter = CombatTestFactory.Encounter(
            new[] { attacker }, new[] { dummy }, plans: plans);
        encounter.Resolve();

        CombatLogEntry opening = encounter.Log
            .First(entry => entry.Kind == CombatLogKind.TechniqueResolved);
        Assert.Equal("test.second", opening.Detail);
    }

    [Fact]
    public void UseCondition_HoldsATechniqueUntilTwoEnemiesArePresent()
    {
        TechniqueDefinition crowd = CombatTestFactory.Technique(
            "test.crowd",
            cooldown: 50,
            condition: TechniqueUseCondition.UseAgainstTwoOrMoreEnemies);
        CombatantState attacker = CombatTestFactory.AttackerWith("a", CombatSide.Party, crowd);
        CombatantState lone = CombatTestFactory.Dummy("d", CombatSide.Enemy, maxHealth: 50);

        CombatEncounter single = CombatTestFactory.Encounter(new[] { attacker }, new[] { lone });
        single.Resolve();

        Assert.DoesNotContain(single.Log, entry => entry.Kind == CombatLogKind.TechniqueResolved);
    }

    [Fact]
    public void PartyVictory_IsReachedAgainstProvisionalEnemies()
    {
        CombatantState hero = CombatTestFactory.AttackerWith(
            "hero",
            CombatSide.Party,
            CombatTestFactory.Technique("test.strike", 0.9, 0.1),
            physicalPower: 140,
            attackSpeed: 2.0,
            maxHealth: 400,
            currentHealth: 400);

        CombatEncounter encounter = CombatTestFactory.Encounter(
            new[] { hero },
            new[] { EnemyCatalog.Create(EnemyArchetype.MeleeEnemy, "e0") });

        Assert.Equal(CombatOutcome.PartyVictory, encounter.Resolve());
    }

    [Fact]
    public void RetreatRule_EndsTheEncounterWithoutDefeat()
    {
        // Faster than the enemy so it reaches its own turn, where the retreat rule
        // is evaluated, before taking another hit.
        CombatantState fragile = CombatTestFactory.AttackerWith(
            "fragile",
            CombatSide.Party,
            CombatTestFactory.Technique("test.poke"),
            attackSpeed: 3.0,
            maxHealth: 100,
            currentHealth: 10);
        var plans = new Dictionary<string, CombatantPlan>
        {
            ["fragile"] = new CombatantPlan(0, System.Array.Empty<string>(), null, true),
        };

        CombatEncounter encounter = CombatTestFactory.Encounter(
            new[] { fragile },
            new[] { EnemyCatalog.Create(EnemyArchetype.MeleeEnemy, "e0") },
            plans: plans);

        Assert.Equal(CombatOutcome.PartyRetreated, encounter.Resolve());
    }

    [Fact]
    public void IncapacitatedPartyMember_StillExistsInTheDomain()
    {
        CombatantState doomed = CombatTestFactory.AttackerWith(
            "doomed",
            CombatSide.Party,
            CombatTestFactory.Technique("test.weak", 0.5, 0.5),
            physicalPower: 1,
            elementalPower: 1,
            maxHealth: 12,
            currentHealth: 12);

        CombatEncounter encounter = CombatTestFactory.Encounter(
            new[] { doomed },
            new[] { EnemyCatalog.Create(EnemyArchetype.MeleeEnemy, "e0") });

        Assert.Equal(CombatOutcome.PartyDefeated, encounter.Resolve());
        Assert.Single(encounter.Party);
        Assert.True(encounter.Party[0].IsDefeated);
    }

    [Fact]
    public void SameSeed_ProducesAnIdenticalLog()
    {
        Assert.Equal(RunSignature(seed: 99), RunSignature(seed: 99));
        Assert.Equal(RunSignature(seed: 1234), RunSignature(seed: 1234));
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentRandomSequences()
    {
        // Determinism is the requirement; this pins that the seed is actually
        // reaching the sequence, so identical logs across seeds would be suspicious
        // rather than reassuring.
        var first = new DeterministicRandom(99);
        var second = new DeterministicRandom(100);
        var repeat = new DeterministicRandom(99);

        var firstDraws = new List<double>();
        var secondDraws = new List<double>();
        var repeatDraws = new List<double>();
        for (int index = 0; index < 8; index++)
        {
            firstDraws.Add(first.NextDouble());
            secondDraws.Add(second.NextDouble());
            repeatDraws.Add(repeat.NextDouble());
        }

        Assert.Equal(firstDraws, repeatDraws);
        Assert.NotEqual(firstDraws, secondDraws);
        Assert.All(firstDraws, draw => Assert.InRange(draw, 0.0, 1.0));
    }

    private static List<string> RunSignature(ulong seed)
    {
        CombatantState hero = CombatTestFactory.Combatant(
            "hero", CombatSide.Party,
            maxHealth: 260, currentHealth: 260,
            physicalPower: 55, elementalPower: 30,
            criticalChance: 0.5, attackSpeed: 1.3,
            techniques: new[] { CombatTestFactory.Technique("test.strike", 0.6, 0.4, cooldown: 1) });

        CombatEncounter encounter = CombatTestFactory.Encounter(
            new[] { hero },
            new[]
            {
                EnemyCatalog.Create(EnemyArchetype.MeleeEnemy, "e0"),
                EnemyCatalog.Create(EnemyArchetype.RangedEnemy, "e1"),
            },
            seed: seed);
        encounter.Resolve();

        return encounter.Log
            .Select(entry =>
                $"{entry.Step}:{entry.Kind}:{entry.ActorId}:{entry.TargetId}:"
                + $"{entry.Resolution?.FinalResult:0.####}:{entry.Resolution?.CriticalResult}")
            .ToList();
    }
}
