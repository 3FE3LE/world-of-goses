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
    public void GetScenePath_RejectsUnknownLineage()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            CharacterVisualRegistry.GetScenePath(
                new LineageId("unknown"), CharacterBodyVariant.Male));
    }

    [Theory]
    [InlineData(0, CharacterBodyVariant.Male)]
    [InlineData(2, CharacterBodyVariant.Male)]
    [InlineData(1, CharacterBodyVariant.Female)]
    [InlineData(-1, CharacterBodyVariant.Female)]
    public void ResolveBodyVariant_IsStableFromAppearanceSeed(
        int appearanceSeed,
        CharacterBodyVariant expected)
    {
        Assert.Equal(expected, CharacterVisualRegistry.ResolveBodyVariant(appearanceSeed));
    }
}
