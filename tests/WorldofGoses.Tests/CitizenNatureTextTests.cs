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
        // The affinity says nothing about the expression. This founder sits on
        // the bare Kovari vertex — Body, Impulse and Domain all at 60 — and Body
        // wins the tie, so they are Fracture whatever element they resonate with.
        Assert.Contains("Physical expression: Fracture", text);
        Assert.Contains("Natural weapons: Hammer, Axe", text);
    }

    [Theory]
    [InlineData(64, 36, 56, 44, 56, 44, "Fracture", "Hammer, Axe")]
    [InlineData(36, 64, 44, 56, 44, 56, "Poisoning", "Bow, Darts")]
    [InlineData(56, 44, 64, 36, 56, 44, "Paralysis", "Whip, Gauntlets")]
    [InlineData(44, 56, 36, 64, 44, 56, "Stunning", "Mace, Orb")]
    [InlineData(56, 44, 56, 44, 64, 36, "Bleeding", "Sword, Daggers")]
    [InlineData(44, 56, 44, 56, 36, 64, "Knockdown", "Spear, Staff")]
    public void Format_DerivesPhysicalExpressionAndNaturalWeaponsFromTheHighestCubeFace(
        int body, int bond, int stability, int impulse, int domain, int reach,
        string expectedExpression,
        string expectedWeapons)
    {
        var cube = new FounderCubeProfile(body, bond, stability, impulse, domain, reach);

        // The affinity is held constant across all six rows on purpose: only the
        // cube moves, and only the cube changes the answer.
        string text = CitizenNatureText.Format(
            cube,
            LineageId.Ardhen,
            CombatNature.FromCube(ElementalAffinity.Fire, cube));

        Assert.Contains($"Physical expression: {expectedExpression}", text);
        Assert.Contains($"Natural weapons: {expectedWeapons}", text);
    }

    [Fact]
    public void Snapshot_CarriesThePhysicalExpressionAndItsNaturalFamilies()
    {
        HeroProfileSnapshot snapshot =
            Assert.IsType<HeroProfileSnapshot>(HeroProfileSnapshot.From(NewFireFounderWorld()));

        // Kovari vertex, Body wins the three-way tie: Fracture, not the Stunning
        // the old affinity shortcut produced for a Fire founder.
        Assert.Equal("Fracture", snapshot.PhysicalExpression);
        Assert.Equal(new[] { "Hammer", "Axe" }, snapshot.NaturalWeaponFamilies);
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
            CombatNature.FromCube(ElementalAffinity.Earth, cube));

        Assert.Contains($"Body {cube.Body} / {cube.Bond} Bond", text);
        Assert.Contains($"Stability {cube.Stability} / {cube.Impulse} Impulse", text);
        Assert.Contains($"Domain {cube.Domain} / {cube.Reach} Reach", text);
        Assert.Equal(100, cube.Body + cube.Bond);
        Assert.Equal(100, cube.Stability + cube.Impulse);
        Assert.Equal(100, cube.Domain + cube.Reach);
    }
}
