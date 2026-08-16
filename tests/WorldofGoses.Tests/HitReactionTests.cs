using System;
using System.Collections.Generic;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Tests.Combat;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The flinch. Only Knockdown moves anyone in the domain, which is correct and
/// which also left a spear thrust looking like it landed on a statue. This is
/// the other half: transient, presentational, and it always decays back to the
/// authoritative position.
/// </summary>
public sealed class HitReactionTests
{
    [Fact]
    public void APurelyElementalBlastShovesNobody()
    {
        // Momentum comes from the bodily part of a blow — the same rule the
        // domain applies to a real knockback. A view that shoved on any hit
        // would contradict the model it is drawing.
        Assert.Equal(0, HitReaction.ShovePixels(
            physicalShare: 0, attackerImpulse: 100, targetStability: 10));
    }

    [Fact]
    public void ShoveGrowsWithTheBodilyShareOfTheBlow()
    {
        double half = HitReaction.ShovePixels(0.5, attackerImpulse: 60, targetStability: 60);
        double whole = HitReaction.ShovePixels(1.0, attackerImpulse: 60, targetStability: 60);

        Assert.True(whole > half);
        Assert.True(half > 0);
    }

    [Fact]
    public void StabilityResistsAndImpulsePushes()
    {
        double planted = HitReaction.ShovePixels(1, attackerImpulse: 50, targetStability: 200);
        double even = HitReaction.ShovePixels(1, attackerImpulse: 50, targetStability: 50);
        double frail = HitReaction.ShovePixels(1, attackerImpulse: 50, targetStability: 5);

        Assert.True(planted < even);
        Assert.True(even < frail);
    }

    /// <summary>
    /// A flinch must stay visibly smaller than the displacement Knockdown
    /// produces, or the expression loses what makes it worth building around.
    /// </summary>
    [Fact]
    public void TheFlinchIsNeverAsLargeAsARealKnockback()
    {
        double biggest = HitReaction.ShovePixels(1, attackerImpulse: 10000, targetStability: 0);

        Assert.InRange(biggest, 0, HitReaction.MaximumShovePixels);
        Assert.True(HitReaction.MaximumShovePixels < CombatBalanceConfig.Default.KnockbackBaseDistance);
    }

    [Fact]
    public void UnmeasuredCombatantsAreNotShoved()
    {
        // A combatant assembled from bare parts has no Impulse and no Stability.
        // No opinion about the exchange means no opinion about how it looked.
        Assert.Equal(0, HitReaction.ShovePixels(1, attackerImpulse: 0, targetStability: 0));
    }

    [Fact]
    public void TheShoveIsAwayFromWhoeverThrewIt()
    {
        double fromLeft = HitReaction.SignedShovePixels(
            1, 60, 60, attackerScreenX: 100, targetScreenX: 200);
        double fromRight = HitReaction.SignedShovePixels(
            1, 60, 60, attackerScreenX: 300, targetScreenX: 200);

        Assert.True(fromLeft > 0);
        Assert.True(fromRight < 0);
        Assert.Equal(Math.Abs(fromLeft), Math.Abs(fromRight), precision: 9);
    }

    /// <summary>
    /// The whole point: the figure ends exactly where the domain says it is.
    /// </summary>
    [Fact]
    public void TheReactionDecaysToExactlyZero()
    {
        Assert.Equal(0, HitReaction.Remaining(7, HitReaction.DecaySeconds));
        Assert.Equal(0, HitReaction.Remaining(7, HitReaction.DecaySeconds * 3));
    }

    [Fact]
    public void TheReactionSettlesRatherThanSlidingBack()
    {
        double start = HitReaction.Remaining(8, 0);
        double quarter = HitReaction.Remaining(8, HitReaction.DecaySeconds * 0.25);
        double half = HitReaction.Remaining(8, HitReaction.DecaySeconds * 0.5);

        Assert.Equal(8, start);
        Assert.True(quarter < start);
        Assert.True(half < quarter);
        // Ease-out: more of the distance is covered early than late.
        Assert.True(start - half > half - HitReaction.Remaining(8, HitReaction.DecaySeconds));
    }

    /// <summary>
    /// A blow that did not connect does not shove. This is what stops the
    /// evasion and control work from being contradicted on screen.
    /// </summary>
    [Fact]
    public void EvadedAndAbsorbedBlowsShoveNothing()
    {
        var strikers = new Dictionary<string, Striker>
        {
            ["attacker"] = new Striker(Impulse: 80, ScreenX: 100),
        };

        Assert.Equal(0, HitReaction.ForEvents(
            "target", 40, 200, new[] { Entry("attacker", "target", final: 0, evaded: true) }, strikers));
        Assert.Equal(0, HitReaction.ForEvents(
            "target", 40, 200, new[] { Entry("attacker", "target", final: 0) }, strikers));

        // And the control: a blow that did land shoves.
        Assert.True(HitReaction.ForEvents(
            "target", 40, 200, new[] { Entry("attacker", "target", final: 25) }, strikers) > 0);
    }

    [Fact]
    public void BlowsFromOppositeSidesCancel()
    {
        var strikers = new Dictionary<string, Striker>
        {
            ["left"] = new Striker(Impulse: 60, ScreenX: 100),
            ["right"] = new Striker(Impulse: 60, ScreenX: 300),
        };
        var events = new[]
        {
            Entry("left", "target", final: 20),
            Entry("right", "target", final: 20),
        };

        Assert.Equal(0, HitReaction.ForEvents("target", 60, 200, events, strikers), precision: 9);
    }

    [Fact]
    public void OnlyBlowsAgainstThisCombatantCount()
    {
        var strikers = new Dictionary<string, Striker>
        {
            ["attacker"] = new Striker(Impulse: 80, ScreenX: 100),
        };
        var events = new[] { Entry("attacker", "someone.else", final: 40) };

        Assert.Equal(0, HitReaction.ForEvents("target", 40, 200, events, strikers));
    }

    /// <summary>
    /// A striker nobody placed contributes nothing rather than throwing. An
    /// off-screen or already-removed attacker is an ordinary state.
    /// </summary>
    [Fact]
    public void AnUnplacedStrikerIsIgnored()
    {
        Assert.Equal(0, HitReaction.ForEvents(
            "target",
            40,
            200,
            new[] { Entry("ghost", "target", final: 40) },
            new Dictionary<string, Striker>()));
    }

    /// <summary>
    /// The snapshot has to carry the two spatial facts a reaction is sized by.
    /// </summary>
    /// <remarks>
    /// Every other test here feeds <see cref="HitReaction"/> its numbers
    /// directly, so all of them would stay green if the session stopped
    /// projecting Impulse and Stability onto the participant snapshot — and the
    /// shove would silently become zero in the actual game, because zero is what
    /// an unmeasured combatant gets. This is the wire between the two halves.
    /// </remarks>
    [Fact]
    public void TheParticipantSnapshotCarriesImpulseAndStability()
    {
        CombatantState brute = EnemyCatalog.Create(EnemyArchetype.MeleeEnemy, "brute");
        var session = new CombatSession(CombatTestFactory.Encounter(
            new[] { CombatTestFactory.Combatant("hero", CombatSide.Party) },
            new[] { brute }));

        CombatParticipantState projected = Assert.Single(session.Snapshot().Enemies);

        Assert.Equal(brute.Spatial.Impulse, projected.Impulse);
        Assert.Equal(brute.Spatial.Stability, projected.Stability);
        Assert.True(projected.Impulse > 0, "A catalogued enemy must carry a real Impulse.");
        Assert.True(projected.Stability > 0, "A catalogued enemy must carry a real Stability.");
    }

    private static CombatLogEntry Entry(
        string actorId,
        string targetId,
        double final,
        bool evaded = false) =>
        new(
            1,
            CombatLogKind.BasicAttackResolved,
            actorId,
            targetId,
            "test.blow",
            new TechniqueResolution(
                1,
                "test.blow",
                actorId,
                targetId,
                PhysicalChannelPower: 50,
                ElementalChannelPower: 0,
                PhysicalCoefficient: 1,
                ElementalCoefficient: 0,
                PhysicalContribution: 50,
                ElementalContribution: 0,
                RawTechniqueResult: 50,
                PhysicalMitigation: 0,
                ElementalMitigation: 0,
                GeneralDamageReduction: 0,
                CriticalResult: false,
                FinalResult: final,
                ElementalNature: ElementalAffinity.Earth,
                AppliedStatuses: Array.Empty<StatusEffectId>(),
                Evaded: evaded));
}
