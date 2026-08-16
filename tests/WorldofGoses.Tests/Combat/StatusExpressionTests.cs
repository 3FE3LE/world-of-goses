using System;
using System.Collections.Generic;
using WorldofGoses.Domain.Combat;
using Xunit;

namespace WorldofGoses.Tests.Combat;

/// <summary>
/// One test per physical expression, each asserting the thing that expression
/// costs and no other.
/// </summary>
/// <remarks>
/// <para>
/// These exist because the six were labels. <c>StatusResolver.Create</c> fell
/// through to <c>(1, 1)</c> for four of them and the only place in the whole
/// encounter that read a status id was the Stunning check — a technique could
/// apply Bleeding and nothing anywhere would notice. The suite passed either
/// way, which is exactly the failure mode a test file is supposed to close.
/// </para>
/// <para>
/// The distinctions asserted here are the design, so read them as the contract:
/// Stunning costs the action, Knockdown costs the action <em>and</em> the
/// ground, Paralysis costs the ground <em>and not</em> the action, Bleeding is
/// attrition armour answers, Poisoning is attrition it does not, and Fracture
/// costs nothing during the fight and everything after it.
/// </para>
/// </remarks>
public sealed class StatusExpressionTests
{
    private static readonly CombatBalanceConfig Balance = CombatBalanceConfig.Default;

    private static IReadOnlyList<StatusEffect> With(
        StatusResolver resolver, params StatusEffectId[] ids)
    {
        IReadOnlyList<StatusEffect> statuses = new List<StatusEffect>();
        foreach (StatusEffectId id in ids)
        {
            statuses = resolver.Apply(statuses, resolver.Create(id, "source", "target", step: 0));
        }
        return statuses;
    }

    /// <summary>Stunning takes the turn and leaves the combatant standing.</summary>
    [Fact]
    public void Stunning_CostsTheActionAndNotTheGround()
    {
        var resolver = new StatusResolver();
        IReadOnlyList<StatusEffect> statuses = With(resolver, StatusEffectId.Stunning);

        Assert.True(resolver.PreventsAction(statuses));
        Assert.False(resolver.PreventsMovement(statuses));
    }

    /// <summary>Knockdown takes both, because the combatant is on the floor.</summary>
    [Fact]
    public void Knockdown_CostsTheActionAndTheGround()
    {
        var resolver = new StatusResolver();
        IReadOnlyList<StatusEffect> statuses = With(resolver, StatusEffectId.Knockdown);

        Assert.True(resolver.PreventsAction(statuses));
        Assert.True(resolver.PreventsMovement(statuses));
    }

    /// <summary>
    /// Paralysis slows severely and seizes sometimes — never roots outright.
    /// </summary>
    /// <remarks>
    /// It rooted in the first pass, which made it a third certainty next to
    /// Stunning and Knockdown and left it worthless against an enemy that never
    /// wanted to move. The pair below is the differentiation: a slow that always
    /// applies, and an interruption that only sometimes does.
    /// </remarks>
    [Fact]
    public void Paralysis_SlowsSeverelyAndNeverRootsOutright()
    {
        var resolver = new StatusResolver();
        IReadOnlyList<StatusEffect> statuses = With(resolver, StatusEffectId.Paralysis);

        Assert.False(resolver.PreventsAction(statuses));
        Assert.False(resolver.PreventsMovement(statuses));
        Assert.Equal(
            Balance.ParalysisMovementSpeedScale,
            resolver.Modifiers(statuses).MovementSpeedScale);
        Assert.True(Balance.ParalysisMovementSpeedScale < 1);
    }

    /// <summary>
    /// The seizure is a roll, so it lands sometimes and not always — which is
    /// what makes it a different promise from Stunning's certainty.
    /// </summary>
    [Fact]
    public void Paralysis_SeizesTheActionSometimesAndNotAlways()
    {
        var resolver = new StatusResolver();
        IReadOnlyList<StatusEffect> statuses = With(resolver, StatusEffectId.Paralysis);
        var random = new DeterministicRandom(seed: 12345);

        int seizures = 0;
        const int rolls = 400;
        for (int i = 0; i < rolls; i++)
        {
            if (resolver.ParalysisSeizesAction(statuses, random)) seizures++;
        }

        Assert.True(seizures > 0, "Paralysis that never seizes is a pure slow.");
        Assert.True(seizures < rolls, "Paralysis that always seizes is a second Stunning.");
    }

    /// <summary>An unparalysed combatant is never seized, whatever the roll.</summary>
    [Fact]
    public void Paralysis_SeizesNobodyWhoDoesNotHaveIt()
    {
        var resolver = new StatusResolver();
        var random = new DeterministicRandom(seed: 99);
        IReadOnlyList<StatusEffect> none = new List<StatusEffect>();

        for (int i = 0; i < 100; i++)
        {
            Assert.False(resolver.ParalysisSeizesAction(none, random));
        }
    }

    /// <summary>
    /// Fracture opens the physical window and leaves the elemental one shut.
    /// </summary>
    /// <remarks>
    /// This is the pair that gives Fracture and Stunning their offensive
    /// identities. Before it they shared one exposure factor, so from the
    /// attacker's side choosing between them changed nothing.
    /// </remarks>
    [Fact]
    public void Fracture_OpensThePhysicalWindowOnly()
    {
        var resolver = new StatusResolver();
        StatusModifiers modifiers = resolver.Modifiers(
            With(resolver, StatusEffectId.Fracture, StatusEffectId.Fracture));

        Assert.Equal(Balance.FracturePhysicalMitigationScale, modifiers.PhysicalMitigationScale);
        Assert.Equal(1.0, modifiers.ElementalMitigationScale);
        Assert.True(modifiers.PhysicalMitigationScale < 1);
    }

    /// <summary>And Stunning opens the elemental one and leaves the physical shut.</summary>
    [Fact]
    public void Stunning_OpensTheElementalWindowOnly()
    {
        var resolver = new StatusResolver();
        StatusModifiers modifiers = resolver.Modifiers(With(resolver, StatusEffectId.Stunning));

        Assert.Equal(Balance.StunningElementalMitigationScale, modifiers.ElementalMitigationScale);
        Assert.Equal(1.0, modifiers.PhysicalMitigationScale);
        Assert.True(modifiers.ElementalMitigationScale < 1);
    }

    /// <summary>
    /// Fracture does exactly two things, and slowing is not one of them.
    /// </summary>
    /// <remarks>
    /// It briefly also cut attack speed and charged a flat cost every step the
    /// target acted, on top of the physical window — three penalties on one
    /// expression. Slowing a clock belongs to Paralysis; Fracture's second
    /// effect is the price of a physical blow, asserted separately below.
    /// </remarks>
    [Fact]
    public void Fracture_DoesNotAlsoSlowTheTarget()
    {
        var resolver = new StatusResolver();
        StatusModifiers fractured = resolver.Modifiers(
            With(resolver, StatusEffectId.Fracture, StatusEffectId.Fracture));
        StatusModifiers paralysed = resolver.Modifiers(With(resolver, StatusEffectId.Paralysis));

        Assert.Equal(1.0, fractured.MovementSpeedScale);
        Assert.True(paralysed.MovementSpeedScale < 1);
        Assert.Equal(1.0, paralysed.PhysicalMitigationScale);
    }

    /// <summary>
    /// Poisoning cannot deepen, so it scales the other way: everything else
    /// lands harder while it is up.
    /// </summary>
    [Fact]
    public void Poisoning_AmplifiesDamageTakenInsteadOfDeepening()
    {
        var resolver = new StatusResolver();
        StatusModifiers once = resolver.Modifiers(With(resolver, StatusEffectId.Poisoning));
        StatusModifiers twice = resolver.Modifiers(
            With(resolver, StatusEffectId.Poisoning, StatusEffectId.Poisoning));

        Assert.Equal(Balance.PoisoningDamageTakenScale, once.DamageTakenScale);
        Assert.True(once.DamageTakenScale > 1);
        Assert.Equal(once.DamageTakenScale, twice.DamageTakenScale);
    }

    /// <summary>The three incapacities are pairwise different, stated directly.</summary>
    [Fact]
    public void TheThreeIncapacities_AreDistinguishableFromEachOther()
    {
        var resolver = new StatusResolver();
        (bool Action, bool Ground) Costs(StatusEffectId id)
        {
            IReadOnlyList<StatusEffect> statuses = With(resolver, id);
            return (resolver.PreventsAction(statuses), resolver.PreventsMovement(statuses));
        }

        (bool, bool) stunning = Costs(StatusEffectId.Stunning);
        (bool, bool) knockdown = Costs(StatusEffectId.Knockdown);
        (bool, bool) paralysis = Costs(StatusEffectId.Paralysis);

        Assert.NotEqual(stunning, knockdown);
        Assert.NotEqual(knockdown, paralysis);
        Assert.NotEqual(stunning, paralysis);
    }

    /// <summary>Bleeding scales with how deep the cut went, and armour counts.</summary>
    [Fact]
    public void Bleeding_IsMitigableAndDeepensWithStacks()
    {
        var resolver = new StatusResolver();
        IReadOnlyList<StatusEffect> once = With(resolver, StatusEffectId.Bleeding);
        IReadOnlyList<StatusEffect> twice =
            With(resolver, StatusEffectId.Bleeding, StatusEffectId.Bleeding);

        (double mitigableOnce, double unmitigableOnce) = resolver.DamageOverTime(once);
        (double mitigableTwice, _) = resolver.DamageOverTime(twice);

        Assert.Equal(Balance.BleedingDamagePerStack, mitigableOnce);
        Assert.Equal(0, unmitigableOnce);
        Assert.Equal(mitigableOnce * 2, mitigableTwice);
    }

    /// <summary>
    /// Poisoning bypasses armour and refuses to deepen, so its pressure is
    /// duration rather than accumulation.
    /// </summary>
    [Fact]
    public void Poisoning_IsUnmitigableAndRefusesToStack()
    {
        var resolver = new StatusResolver();
        IReadOnlyList<StatusEffect> once = With(resolver, StatusEffectId.Poisoning);
        IReadOnlyList<StatusEffect> twice =
            With(resolver, StatusEffectId.Poisoning, StatusEffectId.Poisoning);

        (double mitigableOnce, double unmitigableOnce) = resolver.DamageOverTime(once);
        (_, double unmitigableTwice) = resolver.DamageOverTime(twice);

        Assert.Equal(0, mitigableOnce);
        Assert.Equal(Balance.PoisoningDamagePerStep, unmitigableOnce);
        Assert.Equal(unmitigableOnce, unmitigableTwice);
    }

    /// <summary>
    /// The two damage-over-time effects are told apart by which side of the
    /// mitigation line they land on, not by their numbers.
    /// </summary>
    [Fact]
    public void TheTwoAttritionEffects_LandOnOppositeSidesOfMitigation()
    {
        var resolver = new StatusResolver();

        (double bleedMitigable, double bleedUnmitigable) =
            resolver.DamageOverTime(With(resolver, StatusEffectId.Bleeding));
        (double poisonMitigable, double poisonUnmitigable) =
            resolver.DamageOverTime(With(resolver, StatusEffectId.Poisoning));

        Assert.True(bleedMitigable > 0 && bleedUnmitigable == 0);
        Assert.True(poisonUnmitigable > 0 && poisonMitigable == 0);
    }

    /// <summary>
    /// Using a broken body costs health, charged on exertion rather than on the
    /// clock — so standing still is a real answer.
    /// </summary>
    /// <remarks>
    /// Fracture used to cost nothing at all until the encounter ended, which
    /// made it the one expression with no reason to be chosen over the other
    /// five: if the party won, it never mattered.
    /// </remarks>
    private static CombatantState Fractured(string id, TechniqueDefinition technique)
    {
        CombatantState combatant = CombatTestFactory.Combatant(
            id,
            CombatSide.Enemy,
            maxHealth: 1000,
            currentHealth: 1000,
            techniques: new[] { technique });

        var resolver = new StatusResolver(Balance);
        IReadOnlyList<StatusEffect> statuses = combatant.Statuses;
        for (int i = 0; i < Balance.FractureThreshold; i++)
        {
            statuses = resolver.Apply(
                statuses, resolver.Create(StatusEffectId.Fracture, "s", combatant.Id, 0));
        }
        combatant.ReplaceStatuses(statuses);
        return combatant;
    }

    /// <summary>
    /// The price tracks how bodily the blow was.
    /// </summary>
    /// <remarks>
    /// Compared rather than measured in absolute terms because every combatant
    /// also throws a Basic Attack, and that one is physical — so a fractured
    /// caster still pays something for the swing it cannot help making. What the
    /// effect promises is that the more of your output is body, the more it
    /// costs you, and that is what the inequality states.
    /// </remarks>
    [Fact]
    public void Fracture_ChargesMoreForABodilyBlowThanForAChannelledOne()
    {
        CombatantState swinging = Fractured(
            "swinging", CombatTestFactory.Technique("swing", physical: 1, elemental: 0));
        CombatantState channelling = Fractured(
            "channelling", CombatTestFactory.Technique("channel", physical: 0, elemental: 1));
        CombatantState target = CombatTestFactory.Dummy("target", CombatSide.Party);

        CombatEncounter encounter = CombatTestFactory.Encounter(
            new[] { target }, new[] { swinging, channelling });
        encounter.Advance(1);

        double swingCost = swinging.MaxHealth - swinging.CurrentHealth;
        double channelCost = channelling.MaxHealth - channelling.CurrentHealth;

        Assert.True(swingCost > 0, "A fractured combatant throwing a physical blow must pay.");
        Assert.True(
            swingCost > channelCost,
            $"The bodily blow cost {swingCost:0.##} and the channelled one {channelCost:0.##}. "
            + "Fracture must price the body, not the action.");
    }

    /// <summary>
    /// Only a blow that lands Knockdown displaces its target.
    /// </summary>
    /// <remarks>
    /// Every damaging technique used to write PositionX, so ordinary attrition
    /// slid combatants around the field and range drifted without anyone
    /// choosing it. The shove a solid hit looks like it should produce is a hit
    /// reaction and belongs to presentation, which ends it where the domain says
    /// the target still stands.
    /// </remarks>
    [Fact]
    public void OnlyKnockdown_MovesTheTargetInTheDomain()
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
        CombatantState jabber = CombatTestFactory.Combatant(
            "jabber",
            CombatSide.Enemy,
            techniques: new[] { CombatTestFactory.Technique("jab", physical: 1, elemental: 0) },
            spatial: new CombatSpatialState(positionX: 480, facing: CombatFacing.Right));
        CombatantState shoved = CombatTestFactory.Combatant(
            "shoved", CombatSide.Party, maxHealth: 100000, currentHealth: 100000,
            spatial: new CombatSpatialState(positionX: 500, facing: CombatFacing.Right));

        double before = shoved.Spatial.PositionX;
        CombatTestFactory.Encounter(new[] { shoved }, new[] { shover, jabber }).Advance(1);

        Assert.NotEqual(before, shoved.Spatial.PositionX);

        // And the jab on its own moves nobody.
        CombatantState onlyJab = CombatTestFactory.Combatant(
            "onlyJab",
            CombatSide.Enemy,
            techniques: new[] { CombatTestFactory.Technique("jab", physical: 1, elemental: 0) },
            spatial: new CombatSpatialState(positionX: 520, facing: CombatFacing.Left));
        CombatantState unmoved = CombatTestFactory.Combatant(
            "unmoved", CombatSide.Party, maxHealth: 100000, currentHealth: 100000,
            spatial: new CombatSpatialState(positionX: 500, facing: CombatFacing.Right));

        double stood = unmoved.Spatial.PositionX;
        CombatTestFactory.Encounter(new[] { unmoved }, new[] { onlyJab }).Advance(1);

        Assert.Equal(stood, unmoved.Spatial.PositionX);
    }

    /// <summary>
    /// A Knockdown blow shoves in proportion to how much of it was body.
    /// </summary>
    /// <remarks>
    /// The formula read Impulse against Stability and nothing else, so a purely
    /// elemental blast displaced its target exactly as far as a spear thrust.
    /// Momentum comes from the bodily half of a blow.
    /// </remarks>
    [Fact]
    public void Knockback_ScalesWithThePhysicalShareOfTheBlow()
    {
        // Placed mid-field on purpose: both combatants default to PositionX 0,
        // which is BattlefieldMinimumX, so any shove away from the attacker
        // clamps against the edge and reads as no displacement at all.
        double Displacement(double physical, double elemental)
        {
            CombatantState attacker = CombatTestFactory.Combatant(
                "attacker",
                CombatSide.Enemy,
                techniques: new[]
                {
                    CombatTestFactory.Technique(
                        "blow", physical: physical, elemental: elemental,
                        appliesStatus: StatusEffectId.Knockdown),
                },
                spatial: new CombatSpatialState(positionX: 520, facing: CombatFacing.Left));
            CombatantState target = CombatTestFactory.Combatant(
                "target",
                CombatSide.Party,
                maxHealth: 100000,
                currentHealth: 100000,
                spatial: new CombatSpatialState(positionX: 500, facing: CombatFacing.Right));
            double before = target.Spatial.PositionX;

            CombatTestFactory.Encounter(new[] { target }, new[] { attacker }).Advance(1);
            return Math.Abs(target.Spatial.PositionX - before);
        }

        double bodily = Displacement(physical: 1, elemental: 0);
        double channelled = Displacement(physical: 0, elemental: 1);

        Assert.True(bodily > 0, "A physical blow must displace its target.");
        Assert.True(
            bodily > channelled,
            $"A bodily blow moved the target {bodily:0.##} and a channelled one {channelled:0.##}. "
            + "Momentum is carried by the body of a blow, not by its resonance.");
    }

    /// <summary>An unfractured combatant pays nothing for the same blow.</summary>
    [Fact]
    public void Fracture_ChargesNobodyWhoDoesNotHaveIt()
    {
        TechniqueDefinition swing =
            CombatTestFactory.Technique("swing", physical: 1, elemental: 0);
        CombatantState whole = CombatTestFactory.Combatant(
            "whole", CombatSide.Enemy, maxHealth: 1000, currentHealth: 1000,
            techniques: new[] { swing });
        CombatantState target = CombatTestFactory.Dummy("target", CombatSide.Party);

        CombatEncounter encounter = CombatTestFactory.Encounter(new[] { target }, new[] { whole });
        encounter.Advance(1);

        Assert.Equal(whole.MaxHealth, whole.CurrentHealth);
    }

    /// <summary>
    /// And it takes more than one application, so a graze cannot follow someone
    /// home for days.
    /// </summary>
    [Fact]
    public void Fracture_NeedsMoreThanOneApplicationBeforeItCounts()
    {
        var resolver = new StatusResolver();
        Assert.True(Balance.FractureThreshold > 1);

        IReadOnlyList<StatusEffect> once = With(resolver, StatusEffectId.Fracture);
        Assert.False(resolver.IsActive(once, StatusEffectId.Fracture));

        IReadOnlyList<StatusEffect> enough = once;
        for (int i = 1; i < Balance.FractureThreshold; i++)
        {
            enough = resolver.Apply(
                enough, resolver.Create(StatusEffectId.Fracture, "source", "target", step: 0));
        }
        Assert.True(resolver.IsActive(enough, StatusEffectId.Fracture));
    }

    /// <summary>
    /// Attrition actually reaches a combatant inside a running encounter.
    /// </summary>
    /// <remarks>
    /// The tests above only prove the resolver reports a number. This one proves
    /// the encounter spends it — which is the half that was missing, since
    /// nothing in <c>AdvanceOneStep</c> read a damage-over-time status at all.
    /// The target carries no techniques and faces no attacker, so every point it
    /// loses came from the status and nowhere else.
    /// </remarks>
    [Fact]
    public void Attrition_CostsHealthInsideARunningEncounter()
    {
        CombatantState bleeding = CombatTestFactory.Dummy("bleeding", CombatSide.Enemy);
        CombatantState untouched = CombatTestFactory.Dummy("untouched", CombatSide.Enemy);
        CombatantState idle = CombatTestFactory.Dummy("idle", CombatSide.Party);

        var resolver = new StatusResolver(Balance);
        bleeding.ReplaceStatuses(resolver.Apply(
            bleeding.Statuses,
            resolver.Create(StatusEffectId.Bleeding, "source", bleeding.Id, step: 0)));

        CombatEncounter encounter = CombatTestFactory.Encounter(
            new[] { idle }, new[] { bleeding, untouched });
        encounter.Advance(1);

        Assert.Equal(untouched.MaxHealth, untouched.CurrentHealth);
        Assert.True(
            bleeding.CurrentHealth < bleeding.MaxHealth,
            "A bleeding combatant must lose health on a step even with nobody attacking it.");
        Assert.Equal(
            Balance.BleedingDamagePerStack,
            bleeding.MaxHealth - bleeding.CurrentHealth);
    }

    /// <summary>Armour answers Bleeding and does nothing at all about Poisoning.</summary>
    [Fact]
    public void Mitigation_AnswersBleedingAndNotPoisoning()
    {
        const double mitigation = 0.5;
        CombatantState bled = CombatTestFactory.Dummy(
            "bled", CombatSide.Enemy, physicalMitigation: mitigation);
        CombatantState poisoned = CombatTestFactory.Dummy(
            "poisoned", CombatSide.Enemy, physicalMitigation: mitigation);
        CombatantState idle = CombatTestFactory.Dummy("idle", CombatSide.Party);

        var resolver = new StatusResolver(Balance);
        bled.ReplaceStatuses(resolver.Apply(
            bled.Statuses, resolver.Create(StatusEffectId.Bleeding, "s", bled.Id, 0)));
        poisoned.ReplaceStatuses(resolver.Apply(
            poisoned.Statuses, resolver.Create(StatusEffectId.Poisoning, "s", poisoned.Id, 0)));

        CombatEncounter encounter = CombatTestFactory.Encounter(
            new[] { idle }, new[] { bled, poisoned });
        encounter.Advance(1);

        Assert.Equal(
            Balance.BleedingDamagePerStack * (1 - mitigation),
            bled.MaxHealth - bled.CurrentHealth);
        Assert.Equal(
            Balance.PoisoningDamagePerStep,
            poisoned.MaxHealth - poisoned.CurrentHealth);
    }

    /// <summary>
    /// Every expression now carries a duration and a threshold of its own. Four
    /// of them shared a hard-coded <c>(1, 1)</c> fallback, which is what made
    /// them interchangeable.
    /// </summary>
    [Theory]
    [InlineData(StatusEffectId.Stunning)]
    [InlineData(StatusEffectId.Knockdown)]
    [InlineData(StatusEffectId.Paralysis)]
    [InlineData(StatusEffectId.Bleeding)]
    [InlineData(StatusEffectId.Poisoning)]
    [InlineData(StatusEffectId.Fracture)]
    public void EveryExpression_DeclaresItsOwnDuration(StatusEffectId id)
    {
        var resolver = new StatusResolver();
        StatusEffect status = resolver.Create(id, "source", "target", step: 0);

        Assert.True(
            status.Duration > 1,
            $"{id} lasts {status.Duration} step(s). A one-step default is the "
            + "fallback the six used to share; an expression declares its own.");
    }
}
