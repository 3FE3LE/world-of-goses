using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class CharacterVisualRegistryTests
{
    [Theory]
    [InlineData("ardhen")]
    [InlineData("eirune")]
    [InlineData("kovari")]
    [InlineData("myrven")]
    [InlineData("vaelun")]
    [InlineData("orveth")]
    [InlineData("caelith")]
    [InlineData("theryn")]
    public void GetScenePath_ResolvesBothBodyVariants(string lineageValue)
    {
        var lineage = new LineageId(lineageValue);

        string male = CharacterVisualRegistry.GetScenePath(lineage, CharacterBodyVariant.Male);
        string female = CharacterVisualRegistry.GetScenePath(lineage, CharacterBodyVariant.Female);

        Assert.Equal(
            $"res://assets/characters/lineages/{lineageValue}/male/{lineageValue}_male.tscn",
            male);
        Assert.Equal(
            $"res://assets/characters/lineages/{lineageValue}/female/{lineageValue}_female.tscn",
            female);
    }

    [Fact]
    public void GetScenePath_AppearanceVariant_FallsBackToStandardWhenMissing()
    {
        var lineage = LineageId.Myrven;
        var fallback = CharacterVisualRegistry.GetScenePath(lineage, AppearanceVariantId.Standard, CharacterBodyVariant.Male);
        Assert.Equal(
            "res://assets/characters/lineages/myrven/male/myrven_male.tscn",
            fallback);
    }

    [Fact]
    public void GetScenePath_AppearanceVariant_ResolvesKnownVariant()
    {
        var lineage = LineageId.Ardhen;
        string promotion = CharacterVisualRegistry.GetScenePath(lineage, AppearanceVariantId.Construction, CharacterBodyVariant.Male);
        Assert.Equal(
            "res://assets/characters/lineages/ardhen/variants/construction/male/ardhen_construction_male.tscn",
            promotion);
    }

    [Fact]
    public void GetScenePath_RejectsUnknownLineage()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            CharacterVisualRegistry.GetScenePath(
                new LineageId("unknown"), CharacterBodyVariant.Male));
    }

    [Theory]
    [InlineData(GenderId.Masculine, CharacterBodyVariant.Male)]
    [InlineData(GenderId.Feminine, CharacterBodyVariant.Female)]
    public void ResolveBodyVariant_MapsGenderToBodyVariant(
        GenderId gender,
        CharacterBodyVariant expected)
    {
        Assert.Equal(expected, CharacterVisualRegistry.ResolveBodyVariant(gender));
    }
}
