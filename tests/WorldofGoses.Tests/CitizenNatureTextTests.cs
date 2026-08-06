using WorldofGoses.Domain;
using WorldofGoses.Presentation;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// DEC-0013 leaves the founder with no professional affinities, which used to
/// crash the citizens panel because it indexed the list positionally. The panel
/// now renders the cube-derived nature instead, so the formatter must survive a
/// founder profile. Only the raw <see cref="CitizenNatureText.Format"/> is
/// exercised here: FormatLocalized reaches Godot's TranslationServer, which does
/// not exist in these tests.
/// </summary>
public sealed class CitizenNatureTextTests
{
    [Fact]
    public void Format_DescribesAFounderWithNoProfessionalAffinities()
    {
        var onboarding = new FounderOnboardingResult(
            LineageId.Kovari,
            ElementalAffinity.Fire,
            CubeScoring.ComputeCubeVertex(LineageId.Kovari),
            FounderNarrativeMemory.Empty);
        CitizenProfile profile = CitizenProfile.CreateFounder(onboarding, GenderId.Feminine);
#pragma warning disable CS0618 // Asserting the DEC-0013 shape this formatter exists for.
        Assert.Empty(profile.ProfessionalAffinities);
#pragma warning restore CS0618

        string text = CitizenNatureText.Format(
            profile.CubeProfile,
            profile.Lineage,
            profile.CombatNature);

        Assert.Contains("Embodiment profile", text);
        Assert.Contains("Signature Reconfiguración", text);
        Assert.Contains("Affinity: Fire", text);
        // Fire is the Impulse face, whose physical expression is Stunning, whose
        // natural weapon families are Mace and Orb.
        Assert.Contains("Physical expression: Stunning", text);
        Assert.Contains("Natural weapons: Mace, Orb", text);
    }

    [Theory]
    [InlineData(ElementalAffinity.Earth, "Fracture", "Hammer, Axe")]
    [InlineData(ElementalAffinity.Water, "Paralysis", "Whip, Gauntlets")]
    [InlineData(ElementalAffinity.Air, "Knockdown", "Spear, Staff")]
    [InlineData(ElementalAffinity.Aether, "Poisoning", "Bow, Darts")]
    [InlineData(ElementalAffinity.Silence, "Bleeding", "Sword, Daggers")]
    public void Format_DerivesPhysicalExpressionAndNaturalWeaponsFromTheAffinity(
        ElementalAffinity affinity,
        string expectedExpression,
        string expectedWeapons)
    {
        string text = CitizenNatureText.Format(
            CubeScoring.ComputeCubeVertex(LineageId.Ardhen),
            LineageId.Ardhen,
            new CombatNature(affinity));

        Assert.Contains($"Physical expression: {expectedExpression}", text);
        Assert.Contains($"Natural weapons: {expectedWeapons}", text);
    }

    [Fact]
    public void Snapshot_CarriesThePhysicalExpressionAndItsNaturalFamilies()
    {
        HeroProfileSnapshot snapshot =
            Assert.IsType<HeroProfileSnapshot>(HeroProfileSnapshot.From(NewFireFounderWorld()));

        Assert.Equal("Stunning", snapshot.PhysicalExpression);
        Assert.Equal(new[] { "Mace", "Orb" }, snapshot.NaturalWeaponFamilies);
        // The affinity was already surfaced; the expression derived from it was not.
        Assert.Equal("Fire", snapshot.ElementalAffinity);
    }

    private static CityWorld NewFireFounderWorld()
    {
        var world = new CityWorld();
        var onboarding = new FounderOnboardingResult(
            LineageId.Kovari,
            ElementalAffinity.Fire,
            CubeScoring.ComputeCubeVertex(LineageId.Kovari),
            FounderNarrativeMemory.Empty);
        CitizenProfile profile = CitizenProfile.CreateFounder(onboarding, GenderId.Feminine);
        HeroCreationResult result = world.TryCreateHero(
            new HeroCreationRequest("Ilan", profile, GenderId.Feminine, onboarding));
        Assert.True(result.IsSuccess, result.Outcome.ToString());
        return world;
    }

    [Fact]
    public void Format_RendersTheThreeCubePairsAndSumsEachToOneHundred()
    {
        FounderCubeProfile cube = CubeScoring.ComputeCubeVertex(LineageId.Kovari);

        string text = CitizenNatureText.Format(
            cube,
            LineageId.Kovari,
            new CombatNature(ElementalAffinity.Earth));

        Assert.Contains($"Body {cube.Body} / {cube.Bond} Bond", text);
        Assert.Contains($"Stability {cube.Stability} / {cube.Impulse} Impulse", text);
        Assert.Contains($"Mastery {cube.Mastery} / {cube.Reach} Reach", text);
        Assert.Equal(100, cube.Body + cube.Bond);
        Assert.Equal(100, cube.Stability + cube.Impulse);
        Assert.Equal(100, cube.Mastery + cube.Reach);
    }
}
