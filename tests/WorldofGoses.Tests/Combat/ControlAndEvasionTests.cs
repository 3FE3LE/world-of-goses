using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using Xunit;

namespace WorldofGoses.Tests.Combat;

/// <summary>
/// The three combat statistics that were computed and read by nobody: both
/// evasion channels, and the control pair that decides whether an expression
/// sticks. Each test here fails if its mechanic is removed — a stat the engine
/// never consults is worse than a missing one, because it looks finished.
/// </summary>
public sealed class ControlAndEvasionTests
{
    // ---- Evasion ----------------------------------------------------------

    [Fact]
    public void CertainEvasion_TakesTheWholeBlow()
    {
        CombatantState attacker = CombatTestFactory.AttackerWith(
            "attacker", CombatSide.Party, CombatTestFactory.Technique("test.strike"));
        CombatantState ghost = CombatTestFactory.Combatant(
            "ghost", CombatSide.Enemy, maxHealth: 500, currentHealth: 500,
            physicalEvasion: 1.0, elementalEvasion: 1.0);

        CombatTestFactory.Encounter(new[] { attacker }, new[] { ghost }).Advance(4);

        Assert.Equal(500, ghost.CurrentHealth);
    }

    /// <summary>
    /// Negative verification: the same fixture with evasion at zero must land.
    /// Without this the test above would also pass if attacks stopped resolving.
    /// </summary>
    [Fact]
    public void NoEvasion_StillTakesTheBlow()
    {
        CombatantState attacker = CombatTestFactory.AttackerWith(
            "attacker", CombatSide.Party, CombatTestFactory.Technique("test.strike"));
        CombatantState solid = CombatTestFactory.Combatant(
            "solid", CombatSide.Enemy, maxHealth: 500, currentHealth: 500);

        CombatTestFactory.Encounter(new[] { attacker }, new[] { solid }).Advance(4);

        Assert.True(solid.CurrentHealth < 500);
    }

    /// <summary>
    /// A target's two evasions answer the two channels separately, blended by
    /// the technique's own physical share.
    /// </summary>
    /// <remarks>
    /// Measured on the resolver rather than through an encounter: a combatant
    /// also throws its automatic Basic Attack every step, so the target's health
    /// after a few steps says nothing about which channel the tested technique
    /// used. The first version of this test read health and passed for the wrong
    /// reason on one half and failed on the other.
    /// </remarks>
    [Fact]
    public void EvasionIsBlendedByThePhysicalShareOfTheTechnique()
    {
        // Evasion of exactly 1 always evades and exactly 0 never does, whatever
        // the seeded draw is, so this needs no stubbed random source.
        CombatantState wardsElementOnly = CombatTestFactory.Combatant(
            "wardsElement", CombatSide.Enemy, maxHealth: 500, currentHealth: 500,
            physicalEvasion: 0.0, elementalEvasion: 1.0);

        Assert.False(
            Resolve(CombatTestFactory.Technique("thrust", physical: 1, elemental: 0),
                wardsElementOnly).Evaded,
            "Elemental evasion must not answer a fully physical blow.");
        Assert.True(
            Resolve(CombatTestFactory.Technique("blast", physical: 0, elemental: 1),
                wardsElementOnly).Evaded,
            "Elemental evasion must answer a fully elemental blow.");

        // And the mirror, so neither channel is quietly reading the other.
        CombatantState wardsBodyOnly = CombatTestFactory.Combatant(
            "wardsBody", CombatSide.Enemy, maxHealth: 500, currentHealth: 500,
            physicalEvasion: 1.0, elementalEvasion: 0.0);

        Assert.True(
            Resolve(CombatTestFactory.Technique("thrust", physical: 1, elemental: 0),
                wardsBodyOnly).Evaded);
        Assert.False(
            Resolve(CombatTestFactory.Technique("blast", physical: 0, elemental: 1),
                wardsBodyOnly).Evaded);
    }

    private static TechniqueResolution Resolve(
        TechniqueDefinition technique,
        CombatantState target)
    {
        CombatantState source = CombatTestFactory.Combatant("source", CombatSide.Party);
        return CombatTestFactory.Resolver().Resolve(
            step: 1, technique, source, target, new DeterministicRandom(11));
    }

    [Fact]
    public void AnEvadedBlow_AppliesNoExpression()
    {
        CombatantState attacker = CombatTestFactory.AttackerWith(
            "attacker",
            CombatSide.Party,
            CombatTestFactory.Technique("test.stun", appliesStatus: StatusEffectId.Stunning));
        CombatantState ghost = CombatTestFactory.Combatant(
            "ghost", CombatSide.Enemy, maxHealth: 500, currentHealth: 500,
            physicalEvasion: 1.0, elementalEvasion: 1.0);

        CombatEncounter encounter =
            CombatTestFactory.Encounter(new[] { attacker }, new[] { ghost });
        encounter.Advance(4);

        Assert.Empty(ghost.Statuses);
        Assert.Contains(encounter.Log, entry => entry.Kind == CombatLogKind.Evaded);
        Assert.DoesNotContain(encounter.Log, entry => entry.Kind == CombatLogKind.StatusApplied);
    }

    [Fact]
    public void AnEvadedBlow_IsMarkedOnTheResolution()
    {
        CombatantState attacker = CombatTestFactory.AttackerWith(
            "attacker", CombatSide.Party, CombatTestFactory.Technique("test.strike"));
        CombatantState ghost = CombatTestFactory.Combatant(
            "ghost", CombatSide.Enemy, maxHealth: 500, currentHealth: 500,
            physicalEvasion: 1.0, elementalEvasion: 1.0);

        CombatEncounter encounter =
            CombatTestFactory.Encounter(new[] { attacker }, new[] { ghost });
        encounter.Advance(2);

        CombatLogEntry evaded = encounter.Log.First(entry => entry.Kind == CombatLogKind.Evaded);
        Assert.True(evaded.Resolution?.Evaded);
        Assert.Equal(0, evaded.Resolution!.FinalResult);
    }

    // ---- Control ----------------------------------------------------------

    [Fact]
    public void AnUnshakeableTarget_ResistsTheExpression()
    {
        // Land chance floors at MinimumControlLandChance, so over enough
        // attempts some land; what a resistant target buys is the misses.
        CombatEncounter encounter = ControlEncounter(
            controlPower: CombatBalanceConfig.Default.MinimumControlLandChance,
            controlResistance: 1000);
        encounter.Advance(30);

        Assert.Contains(encounter.Log, entry => entry.Kind == CombatLogKind.StatusResisted);
    }

    /// <summary>
    /// Negative verification for the roll: with resistance left at zero the
    /// expression must never be refused, so the test above is measuring
    /// resistance and not a technique that stopped firing.
    /// </summary>
    [Fact]
    public void ATargetWithoutControlStatistics_NeverResists()
    {
        CombatEncounter encounter = ControlEncounter(controlPower: 0, controlResistance: 0);
        encounter.Advance(30);

        Assert.DoesNotContain(encounter.Log, entry => entry.Kind == CombatLogKind.StatusResisted);
        Assert.Contains(encounter.Log, entry => entry.Kind == CombatLogKind.StatusApplied);
    }

    [Fact]
    public void MoreControlPower_LandsMoreExpressions()
    {
        int weak = LandedExpressions(controlPower: 0.80, controlResistance: 1.40);
        int strong = LandedExpressions(controlPower: 1.40, controlResistance: 0.80);

        Assert.True(
            strong > weak,
            $"Control Power must matter: weak landed {weak}, strong landed {strong}.");
    }

    [Fact]
    public void ControlIsNeverCertainAndNeverImpossible()
    {
        // Both ends are clamped, so even an absurd mismatch leaves the other
        // side something. A wall the player can never cross is a locked door.
        Assert.True(LandedExpressions(controlPower: 1000, controlResistance: 0.80, steps: 40) > 0);
        Assert.True(LandedExpressions(controlPower: 0.80, controlResistance: 1000, steps: 40) > 0);

        int overwhelming = LandedExpressions(controlPower: 1000, controlResistance: 0.80, steps: 40);
        int refused = ResistedExpressions(controlPower: 1000, controlResistance: 0.80, steps: 40);
        Assert.True(
            refused > 0,
            $"The ceiling must leave room to fail; {overwhelming} landed and none were refused.");
    }

    [Fact]
    public void AResistedKnockdown_DoesNotMoveTheTarget()
    {
        // The whole reason statuses are resolved before the knockback. Reading
        // the technique's intent instead of the landed set would shove here.
        CombatantState shover = CombatTestFactory.Combatant(
            "shover",
            CombatSide.Enemy,
            techniques: new[]
            {
                CombatTestFactory.Technique(
                    "thrust", physical: 1, elemental: 0,
                    appliesStatus: StatusEffectId.Knockdown),
            },
            controlPower: 0.0001,
            spatial: new CombatSpatialState(positionX: 520, facing: CombatFacing.Left));
        CombatantState planted = CombatTestFactory.Combatant(
            "planted", CombatSide.Party, maxHealth: 100000, currentHealth: 100000,
            controlResistance: 1000,
            spatial: new CombatSpatialState(positionX: 500, facing: CombatFacing.Right));

        CombatEncounter encounter =
            CombatTestFactory.Encounter(new[] { planted }, new[] { shover });

        double before = planted.Spatial.PositionX;
        for (int step = 0; step < 12; step++)
        {
            encounter.Advance();
            bool resisted = encounter.Log.Any(entry =>
                entry.Kind == CombatLogKind.StatusResisted && entry.Step == encounter.Step);
            if (!resisted) continue;

            Assert.Equal(before, planted.Spatial.PositionX);
            return;
        }

        Assert.Fail("The resistant target never refused a Knockdown in twelve steps.");
    }

    // ---- Typed impact payload ---------------------------------------------

    [Fact]
    public void KnockbackCarriesATypedImpactRatherThanAFormattedString()
    {
        CombatantState shover = CombatTestFactory.Combatant(
            "shover",
            CombatSide.Enemy,
            techniques: new[]
            {
                CombatTestFactory.Technique(
                    "thrust", physical: 1, elemental: 0,
                    appliesStatus: StatusEffectId.Knockdown),
            },
            spatial: new CombatSpatialState(positionX: 520, facing: CombatFacing.Left));
        CombatantState shoved = CombatTestFactory.Combatant(
            "shoved", CombatSide.Party, maxHealth: 100000, currentHealth: 100000,
            spatial: new CombatSpatialState(positionX: 500, facing: CombatFacing.Right));

        double before = shoved.Spatial.PositionX;
        CombatEncounter encounter =
            CombatTestFactory.Encounter(new[] { shoved }, new[] { shover });
        encounter.Advance(1);

        CombatLogEntry knockback = encounter.Log
            .First(entry => entry.Kind == CombatLogKind.KnockbackApplied);

        Assert.NotNull(knockback.Impact);
        Assert.Equal(
            shoved.Spatial.PositionX - before,
            knockback.Impact!.Value.Displacement,
            precision: 6);
        // A fully physical thrust, so presentation may draw it at full weight.
        Assert.Equal(1.0, knockback.Impact.Value.PhysicalShare, precision: 6);
    }

    [Fact]
    public void DamageOverTimeCarriesASignedHealthDelta()
    {
        CombatantState bleeder = CombatTestFactory.AttackerWith(
            "bleeder",
            CombatSide.Party,
            CombatTestFactory.Technique("test.cut", appliesStatus: StatusEffectId.Bleeding));
        CombatantState victim = CombatTestFactory.Combatant(
            "victim", CombatSide.Enemy, maxHealth: 5000, currentHealth: 5000);

        CombatEncounter encounter =
            CombatTestFactory.Encounter(new[] { bleeder }, new[] { victim });
        encounter.Advance(6);

        CombatLogEntry tick = encounter.Log
            .First(entry => entry.Kind == CombatLogKind.StatusDamage);

        Assert.NotNull(tick.Impact);
        Assert.True(
            tick.Impact!.Value.HealthDelta < 0,
            "Damage must be a negative delta so presentation never guesses the sign.");
    }

    [Fact]
    public void MovementCarriesItsDistanceInTheImpact()
    {
        CombatantState walker = CombatTestFactory.Combatant(
            "walker",
            CombatSide.Party,
            techniques: new[] { CombatTestFactory.Technique("test.poke") },
            spatial: new CombatSpatialState(
                positionX: 100, movementSpeed: 1, attackRange: 20,
                bodyRadius: 10, facing: CombatFacing.Right));
        CombatantState far = CombatTestFactory.Combatant(
            "far", CombatSide.Enemy, maxHealth: 100000, currentHealth: 100000,
            spatial: new CombatSpatialState(
                positionX: 600, movementSpeed: 0, attackRange: 20,
                bodyRadius: 10, facing: CombatFacing.Left));

        CombatEncounter encounter = CombatTestFactory.Encounter(new[] { walker }, new[] { far });
        encounter.Advance(1);

        CombatLogEntry moved = encounter.Log
            .First(entry => entry.Kind == CombatLogKind.CombatantMoved);

        Assert.NotNull(moved.Impact);
        Assert.True(moved.Impact!.Value.Displacement > 0);
    }

    // ---- Helpers ----------------------------------------------------------

    private static CombatEncounter ControlEncounter(
        double controlPower,
        double controlResistance)
    {
        CombatantState controller = CombatTestFactory.Combatant(
            "controller",
            CombatSide.Party,
            techniques: new[]
            {
                CombatTestFactory.Technique(
                    "test.bind", cooldown: 0, appliesStatus: StatusEffectId.Bleeding),
            },
            controlPower: controlPower);
        CombatantState subject = CombatTestFactory.Combatant(
            "subject", CombatSide.Enemy, maxHealth: 1000000, currentHealth: 1000000,
            controlResistance: controlResistance);

        return CombatTestFactory.Encounter(new[] { controller }, new[] { subject });
    }

    private static int LandedExpressions(
        double controlPower,
        double controlResistance,
        int steps = 60) =>
        Count(controlPower, controlResistance, steps, CombatLogKind.StatusApplied);

    private static int ResistedExpressions(
        double controlPower,
        double controlResistance,
        int steps = 60) =>
        Count(controlPower, controlResistance, steps, CombatLogKind.StatusResisted);

    private static int Count(
        double controlPower,
        double controlResistance,
        int steps,
        CombatLogKind kind)
    {
        CombatEncounter encounter = ControlEncounter(controlPower, controlResistance);
        encounter.Advance(steps);
        return encounter.Log.Count(entry => entry.Kind == kind);
    }
}
