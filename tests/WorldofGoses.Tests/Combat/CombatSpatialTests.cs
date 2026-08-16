using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests.Combat;

public sealed class CombatSpatialTests
{
    [Fact]
    public void MeleeApproachesAndStopsInsideAttackRange()
    {
        CombatantState melee = Actor("melee", CombatSide.Party, 100, speed: 1, range: 30);
        CombatantState target = Actor("target", CombatSide.Enemy, 300, speed: 0, range: 30);
        var balance = CombatBalanceConfig.Default with { KnockbackBaseDistance = 0 };
        CombatEncounter encounter = CombatTestFactory.Encounter(
            [melee],
            [target],
            balance: balance);

        encounter.Advance(10);

        Assert.True(melee.Spatial.PositionX > 100);
        Assert.True(melee.Spatial.IsWithinAttackRange(target.Spatial));
        Assert.Contains(encounter.Log, entry =>
            entry.Kind == CombatLogKind.CombatantMoved && entry.ActorId == melee.Id);
    }

    [Fact]
    public void RangedApproachesThenPlantsWithoutKitingWhenOpponentCloses()
    {
        CombatantState ranged = Actor("ranged", CombatSide.Party, 700, speed: 0.8, range: 220);
        CombatantState melee = Actor("melee", CombatSide.Enemy, 300, speed: 1.2, range: 30);
        CombatEncounter encounter = CombatTestFactory.Encounter([ranged], [melee]);

        encounter.Advance();
        double plantedAt = ranged.Spatial.PositionX;
        Assert.True(plantedAt < 700);

        encounter.Advance(8);

        Assert.True(melee.Spatial.PositionX > 300);
        Assert.DoesNotContain(encounter.Log, entry =>
            entry.Kind == CombatLogKind.CombatantMoved
            && entry.ActorId == ranged.Id
            && double.Parse(entry.Detail, System.Globalization.CultureInfo.InvariantCulture) > 0);
    }

    [Fact]
    public void FasterMovementCoversMoreDistanceWithTheSameRules()
    {
        CombatantState slow = Actor("slow", CombatSide.Party, 100, speed: 0.5, range: 30);
        CombatantState fast = Actor("fast", CombatSide.Party, 100, speed: 1.5, range: 30);
        CombatantState slowTarget = Actor("target", CombatSide.Enemy, 900, speed: 0, range: 30);
        CombatantState fastTarget = Actor("target", CombatSide.Enemy, 900, speed: 0, range: 30);

        CombatTestFactory.Encounter([slow], [slowTarget]).Advance();
        CombatTestFactory.Encounter([fast], [fastTarget]).Advance();

        Assert.True(fast.Spatial.PositionX - 100 > slow.Spatial.PositionX - 100);
    }

    [Fact]
    public void ActorAlreadyInsideRangeStopsWithoutAReverseAdjustment()
    {
        CombatantState ranged = Actor("ranged", CombatSide.Party, 500, speed: 1, range: 220);
        CombatantState target = Actor("target", CombatSide.Enemy, 340, speed: 0, range: 20);
        var balance = CombatBalanceConfig.Default with { KnockbackBaseDistance = 0 };
        CombatEncounter encounter = CombatTestFactory.Encounter(
            [ranged],
            [target],
            balance: balance);

        encounter.Advance();

        Assert.Equal(500, ranged.Spatial.PositionX);
        Assert.DoesNotContain(encounter.Log, entry =>
            entry.Kind == CombatLogKind.CombatantMoved && entry.ActorId == ranged.Id);
    }

    [Fact]
    public void KnockbackUsesImpulseAndStabilityMonotonically()
    {
        double lowImpulse = Knockback(impulse: 20, stability: 50);
        double highImpulse = Knockback(impulse: 80, stability: 50);
        double highStability = Knockback(impulse: 80, stability: 100);

        Assert.True(highImpulse > lowImpulse);
        Assert.True(highStability < highImpulse);
    }

    [Fact]
    public void ActorReapproachesAfterKnockbackBreaksRange()
    {
        var balance = CombatBalanceConfig.Default with { KnockbackBaseDistance = 100 };
        CombatantState attacker = Actor(
            "attacker", CombatSide.Party, 100, speed: 1, range: 20,
            impulse: 100, knocksDown: true);
        CombatantState target = Actor("target", CombatSide.Enemy, 140, speed: 0, range: 20, stability: 0);
        CombatEncounter encounter = CombatTestFactory.Encounter([attacker], [target], balance: balance);

        encounter.Advance();
        Assert.False(attacker.Spatial.IsWithinAttackRange(target.Spatial));
        encounter.Advance();

        Assert.Contains(encounter.Log, entry =>
            entry.Step == 2
            && entry.Kind == CombatLogKind.CombatantMoved
            && entry.ActorId == attacker.Id);
    }

    [Fact]
    public void MultipleMeleeMayOverlapAndEngageTheSameTarget()
    {
        CombatantState first = Actor("first", CombatSide.Party, 100, speed: 1, range: 30);
        CombatantState second = Actor("second", CombatSide.Party, 100, speed: 1, range: 30);
        CombatantState target = Actor("target", CombatSide.Enemy, 140, speed: 0, range: 30);
        var balance = CombatBalanceConfig.Default with { KnockbackBaseDistance = 0 };
        CombatEncounter encounter = CombatTestFactory.Encounter(
            [first, second],
            [target],
            balance: balance);

        encounter.Advance();

        Assert.Equal(first.Spatial.PositionX, second.Spatial.PositionX);
        Assert.Contains(encounter.Log, entry =>
            entry.Kind == CombatLogKind.BasicAttackResolved && entry.ActorId == first.Id);
        Assert.Contains(encounter.Log, entry =>
            entry.Kind == CombatLogKind.BasicAttackResolved && entry.ActorId == second.Id);
    }

    [Fact]
    public void AreaSkillAndAutoCastConditionIgnoreEnemiesOutsideAttackRange()
    {
        TechniqueDefinition sweep = CombatTestFactory.Technique(
            "area.sweep",
            cooldown: 20,
            target: TechniqueTargetRule.AllEnemies,
            condition: TechniqueUseCondition.UseAgainstTwoOrMoreEnemies);
        CombatantState attacker = CombatTestFactory.Combatant(
            "attacker",
            CombatSide.Party,
            maxHealth: 100000,
            currentHealth: 100000,
            techniques: [sweep],
            spatial: Spatial(100, speed: 0, range: 80, CombatSide.Party));
        CombatantState near = Actor("near", CombatSide.Enemy, 170, speed: 0, range: 20);
        CombatantState far = Actor("far", CombatSide.Enemy, 400, speed: 0, range: 20);
        double farHealth = far.CurrentHealth;
        double farPosition = far.Spatial.PositionX;
        CombatEncounter encounter = CombatTestFactory.Encounter([attacker], [near, far]);

        encounter.Advance();

        Assert.DoesNotContain(encounter.Log, entry =>
            entry.Kind == CombatLogKind.TechniqueResolved && entry.ActorId == attacker.Id);
        Assert.DoesNotContain(encounter.Log, entry =>
            entry.TargetId == far.Id
            && entry.Kind is CombatLogKind.BasicAttackResolved
                or CombatLogKind.TechniqueResolved
                or CombatLogKind.KnockbackApplied);
        Assert.Equal(farHealth, far.CurrentHealth);
        Assert.Equal(farPosition, far.Spatial.PositionX);
    }

    [Fact]
    public void AreaSkillResolvesOnlyAgainstEnemiesInsideAttackRange()
    {
        TechniqueDefinition sweep = CombatTestFactory.Technique(
            "area.sweep",
            cooldown: 20,
            target: TechniqueTargetRule.AllEnemies,
            condition: TechniqueUseCondition.UseWhenReady);
        CombatantState attacker = CombatTestFactory.Combatant(
            "attacker",
            CombatSide.Party,
            maxHealth: 100000,
            currentHealth: 100000,
            techniques: [sweep],
            spatial: Spatial(100, speed: 0, range: 80, CombatSide.Party));
        CombatantState near = Actor("near", CombatSide.Enemy, 170, speed: 0, range: 20);
        CombatantState far = Actor("far", CombatSide.Enemy, 400, speed: 0, range: 20);
        double nearHealth = near.CurrentHealth;
        double farHealth = far.CurrentHealth;
        double farPosition = far.Spatial.PositionX;
        CombatEncounter encounter = CombatTestFactory.Encounter([attacker], [near, far]);

        encounter.Advance();

        Assert.Contains(encounter.Log, entry =>
            entry.Kind == CombatLogKind.TechniqueResolved
            && entry.ActorId == attacker.Id
            && entry.TargetId == near.Id);
        Assert.True(near.CurrentHealth < nearHealth);
        Assert.DoesNotContain(encounter.Log, entry =>
            entry.TargetId == far.Id
            && entry.Kind is CombatLogKind.BasicAttackResolved
                or CombatLogKind.TechniqueResolved
                or CombatLogKind.KnockbackApplied);
        Assert.Equal(farHealth, far.CurrentHealth);
        Assert.Equal(farPosition, far.Spatial.PositionX);
    }

    [Fact]
    public void ActorReevaluatesTargetAndApproachesTheNextEnemyAfterDefeat()
    {
        CombatantState attacker = Actor("attacker", CombatSide.Party, 100, speed: 1, range: 30);
        CombatantState near = CombatTestFactory.Combatant(
            "near",
            CombatSide.Enemy,
            maxHealth: 1,
            currentHealth: 1,
            techniques: Array.Empty<TechniqueDefinition>(),
            spatial: Spatial(140, speed: 0, range: 20, CombatSide.Enemy));
        CombatantState far = CombatTestFactory.Combatant(
            "far",
            CombatSide.Enemy,
            maxHealth: 100000,
            currentHealth: 100000,
            techniques: Array.Empty<TechniqueDefinition>(),
            spatial: Spatial(400, speed: 0, range: 20, CombatSide.Enemy));
        var balance = CombatBalanceConfig.Default with { KnockbackBaseDistance = 0 };
        CombatEncounter encounter = CombatTestFactory.Encounter(
            [attacker],
            [near, far],
            balance: balance);

        encounter.Advance();
        Assert.True(near.IsDefeated);

        encounter.Advance();

        Assert.Contains(encounter.Log, entry =>
            entry.Step == 2
            && entry.Kind == CombatLogKind.CombatantMoved
            && entry.ActorId == attacker.Id
            && entry.TargetId == far.Id);
    }

    [Fact]
    public void EquivalentSeedsProduceTheSameSpatialOutcome()
    {
        CombatSession left = Session(seed: 89);
        CombatSession right = Session(seed: 89);

        left.Advance(8);
        right.Advance(8);

        Assert.Equal(left.Snapshot().Party, right.Snapshot().Party);
        Assert.Equal(left.Snapshot().Enemies, right.Snapshot().Enemies);
        Assert.Equal(
            left.Log.Select(entry => $"{entry.Step}:{entry.Kind}:{entry.Detail}"),
            right.Log.Select(entry => $"{entry.Step}:{entry.Kind}:{entry.Detail}"));
    }

    [Fact]
    public void PresentationInterpolationDoesNotMutateAuthoritativeSpatialState()
    {
        CombatantState actor = Actor("actor", CombatSide.Party, 123, speed: 1, range: 40);
        double authoritativePosition = actor.Spatial.PositionX;

        Vector2I visual = CombatantView.InterpolatedPixelPosition(
            new Vector2(10, 20),
            new Vector2(31, 44),
            0.5f);

        Assert.Equal(new Vector2I(20, 32), visual);
        Assert.Equal(authoritativePosition, actor.Spatial.PositionX);
    }

    [Fact]
    public void FirstEnemyArchetypesExposeControlledDistinctSpatialProfiles()
    {
        CombatantState melee = EnemyCatalog.Create(EnemyArchetype.MeleeEnemy, "melee", 800);
        CombatantState ranged = EnemyCatalog.Create(EnemyArchetype.RangedEnemy, "ranged", 820);

        Assert.True(melee.Spatial.MovementSpeed > ranged.Spatial.MovementSpeed);
        Assert.True(melee.Spatial.AttackRange < ranged.Spatial.AttackRange);
        Assert.Equal(CombatStature.Tall, melee.Stature);
        Assert.Equal(CombatStature.Standard, ranged.Stature);
        Assert.InRange(melee.Spatial.BodyRadius, 1, 20);
        Assert.InRange(ranged.Spatial.BodyRadius, 1, 20);
    }

    private static double Knockback(double impulse, double stability)
    {
        CombatantState attacker = Actor(
            "attacker", CombatSide.Party, 100, speed: 0, range: 60,
            impulse: impulse, knocksDown: true);
        CombatantState target = Actor("target", CombatSide.Enemy, 140, speed: 0, range: 60, stability: stability);
        CombatEncounter encounter = CombatTestFactory.Encounter([attacker], [target]);
        double before = target.Spatial.PositionX;
        encounter.Advance();
        return target.Spatial.PositionX - before;
    }

    private static CombatSession Session(ulong seed)
    {
        CombatantState party = Actor("party", CombatSide.Party, 100, speed: 1, range: 40);
        CombatantState enemy = Actor("enemy", CombatSide.Enemy, 600, speed: 0.8, range: 180);
        return new CombatSession(CombatTestFactory.Encounter([party], [enemy], seed));
    }

    /// <param name="knocksDown">
    /// Whether this actor's technique carries the Knockdown expression. Only a
    /// blow that lands Knockdown displaces anyone, so the knockback tests have
    /// to ask for it explicitly; every other test here wants a combatant that
    /// stays where the domain put it.
    /// </param>
    private static CombatantState Actor(
        string id,
        CombatSide side,
        double position,
        double speed,
        double range,
        double stability = 50,
        double impulse = 50,
        bool knocksDown = false) => CombatTestFactory.Combatant(
            id,
            side,
            maxHealth: 100000,
            currentHealth: 100000,
            techniques:
            [
                CombatTestFactory.Technique(
                    $"{id}.active",
                    cooldown: 20,
                    appliesStatus: knocksDown ? StatusEffectId.Knockdown : null),
            ],
            spatial: Spatial(position, speed, range, side, stability, impulse));

    private static CombatSpatialState Spatial(
        double position,
        double speed,
        double range,
        CombatSide side,
        double stability = 50,
        double impulse = 50) => new(
            position,
            speed,
            range,
            bodyRadius: 10,
            stability,
            impulse,
            side == CombatSide.Party ? CombatFacing.Right : CombatFacing.Left);
}
