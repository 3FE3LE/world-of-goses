using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The twelve weapon families used to ship identical <c>1.0 / 1.0</c> channels,
/// which made choosing between them a choice about technique lists and nothing
/// else. These pin that the axis is real and that it stays a trade.
/// </summary>
public sealed class WeaponFamilyChannelTests
{
    /// <summary>
    /// The six reference profiles in <c>statistics-and-combat.md</c> §9.1. If a
    /// value here ever disagrees with the document, the document is canon.
    /// </summary>
    public static TheoryData<WeaponFamily, double, double> CanonicalProfiles => new()
    {
        { WeaponFamily.Hammer, 1.20, 0.75 },   // Aren
        { WeaponFamily.Bow, 0.85, 1.15 },      // Seyra
        { WeaponFamily.Whip, 0.95, 1.00 },     // Mira
        { WeaponFamily.Orb, 0.75, 1.20 },      // Tovan
        { WeaponFamily.Daggers, 1.05, 0.95 },  // Neris
        { WeaponFamily.Spear, 1.10, 1.00 },    // Vael
    };

    [Theory]
    [MemberData(nameof(CanonicalProfiles))]
    public void CanonicalFamilies_MatchTheReferenceProfiles(
        WeaponFamily family,
        double physical,
        double elemental)
    {
        (double actualPhysical, double actualElemental) = WeaponFamilyChannels.For(family);

        Assert.Equal(physical, actualPhysical, precision: 6);
        Assert.Equal(elemental, actualElemental, precision: 6);
    }

    [Fact]
    public void EveryFamilyIsCovered()
    {
        foreach (WeaponFamily family in Enum.GetValues<WeaponFamily>())
        {
            // Throws for an unmapped family, so adding a thirteenth family
            // without giving it channels fails here rather than at runtime.
            (double physical, double elemental) = WeaponFamilyChannels.For(family);
            Assert.True(physical > 0 && elemental > 0, $"{family} has an empty channel.");
        }
    }

    [Fact]
    public void EveryFamilyStaysInsideTheSanctionedBand()
    {
        var balance = StatisticsBalanceConfig.Default;

        foreach (WeaponFamily family in Enum.GetValues<WeaponFamily>())
        {
            (double physical, double elemental) = WeaponFamilyChannels.For(family);

            Assert.InRange(physical, balance.MinimumWeaponChannel, balance.MaximumWeaponChannel);
            Assert.InRange(elemental, balance.MinimumWeaponChannel, balance.MaximumWeaponChannel);
            // Constructing the profile re-runs the same validation the domain
            // applies to any real weapon, so the table cannot drift out of band.
            Assert.Equal(family, WeaponFamilyChannels.ProfileFor(family).Family);
        }
    }

    /// <summary>
    /// No family is simply better than another: transferring the body well
    /// costs resonance. Without this a "best weapon" could be added by raising
    /// both halves, and every other family would become a mistake to pick.
    /// </summary>
    [Fact]
    public void ChannelsAreATradeRatherThanARanking()
    {
        var totals = new List<double>();
        foreach (WeaponFamily family in Enum.GetValues<WeaponFamily>())
        {
            (double physical, double elemental) = WeaponFamilyChannels.For(family);
            totals.Add(physical + elemental);
        }

        // The canonical six themselves span 1.95 to 2.10, so this is a band and
        // not a fixed budget; what it forbids is a family well outside it.
        Assert.InRange(totals.Min(), 1.90, 2.10);
        Assert.InRange(totals.Max(), 1.90, 2.10);
    }

    /// <summary>
    /// Negative verification for the whole point of the change: the twelve are
    /// no longer interchangeable. This fails the moment anyone flattens the
    /// table back to a neutral pair.
    /// </summary>
    [Fact]
    public void TheTwelveFamiliesAreNotInterchangeable()
    {
        var distinct = new HashSet<(double, double)>();
        foreach (WeaponFamily family in Enum.GetValues<WeaponFamily>())
        {
            distinct.Add(WeaponFamilyChannels.For(family));
        }

        Assert.True(distinct.Count >= 10, $"Only {distinct.Count} distinct channel pairs.");
        Assert.DoesNotContain((1.0, 1.0), distinct);
    }

    [Fact]
    public void TheStarterWeaponTakesItsFamilysChannels()
    {
        var service = new CitizenEquipmentService();
        Citizen citizen = Fixture();

        WeaponItemInstance weapon =
            service.MaterializeStarterWeapon(citizen, WeaponFamily.Hammer);

        (double physical, double elemental) = WeaponFamilyChannels.For(WeaponFamily.Hammer);
        Assert.Equal(physical, weapon.Channels.PhysicalTransfer, precision: 6);
        Assert.Equal(elemental, weapon.Channels.ElementalResonance, precision: 6);
        // And it is the equipped one, not just an owned item.
        Assert.Equal(physical, citizen.EquipmentLoadout.Weapon!.PhysicalTransfer, precision: 6);
    }

    /// <summary>
    /// A founder who picks the Hammer really does hit harder with the body than
    /// one who picks the Orb, all else equal. Before this the two founders were
    /// numerically identical.
    /// </summary>
    [Fact]
    public void TheStarterFamilyChangesThePhysicalChannel()
    {
        double hammer = PhysicalChannelPowerWith(WeaponFamily.Hammer);
        double orb = PhysicalChannelPowerWith(WeaponFamily.Orb);

        Assert.True(hammer > orb, $"Hammer {hammer:0.###} should out-transfer Orb {orb:0.###}.");
    }

    private static double PhysicalChannelPowerWith(WeaponFamily family)
    {
        var service = new CitizenEquipmentService();
        Citizen citizen = Fixture();
        service.MaterializeStarterWeapon(citizen, family);

        var balance = StatisticsBalanceConfig.Default;
        DerivedStatistics derived = new StatisticsCalculator(balance).Calculate(
            citizen.CubeProfile,
            citizen.EquipmentLoadout,
            new StatCalculationContext(
                citizen.WeaponSkillLevel(family),
                conditionFactor: 1.0,
                citySupportFactor: 1.0,
                balance));
        return derived.Offense.PhysicalChannelPower.Value;
    }

    /// <summary>
    /// A founder built the way the game builds one (DEC-0013): a lineage, a
    /// body, an elemental affinity and a Cube, and nothing else.
    /// </summary>
    /// <remarks>
    /// Not <c>CitizenProfile.TryCreate</c>. That overload still demands three
    /// professional affinities, a combat style, weapon preferences, three
    /// personality traits, a political orientation and a spiritual posture —
    /// all of which a citizen earns during their life and none of which
    /// onboarding produces. A fixture that hand-picks them describes a founder
    /// the game cannot create. <c>CityPrototype.NewFounderProfile</c> makes the
    /// same point for the visual-regression fixtures.
    /// </remarks>
    private static Citizen Fixture()
    {
        CitizenProfile profile = CitizenProfile.CreateFounder(
            new FounderOnboardingResult(
                LineageId.Ardhen,
                ElementalAffinity.Earth,
                CubeScoring.ComputeCubeVertex(LineageId.Ardhen),
                FounderNarrativeMemory.Empty),
            GenderId.Masculine);
        return new Citizen(new CitizenId(99), "Fixture", appearanceSeed: 99, profile: profile);
    }
}
