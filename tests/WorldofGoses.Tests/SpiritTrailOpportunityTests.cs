using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The <see cref="ResourceOpportunityKind.SpiritTrailSearch"/>
/// opportunity is the post-dawn motivation that
/// <c>docs/19_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c> §12 promises: the
/// trail the spirit left leads to fire-blackened wood. These tests
/// assert the definition fits the existing
/// <see cref="ResourceExpeditionDefinition"/> shape without
/// introducing a new field, and that the kind round-trips through
/// the string-serialised opportunity log.
/// </summary>
public sealed class SpiritTrailOpportunityTests
{
    [Fact]
    public void Definition_ProducesWoodReward()
    {
        ResourceExpeditionDefinition definition =
            ResourceExpeditionRules.Definition(ResourceOpportunityKind.SpiritTrailSearch);

        Assert.Equal(ResourceType.Wood, definition.RewardResource);
        Assert.Equal(ResourceType.Food, definition.SupplyResource);
        Assert.Equal(1, definition.SupplyAmount);
    }

    [Fact]
    public void Definition_MatchesFallenWoodReturnCurve()
    {
        // The trail mirrors FallenWoodSearch's return curve: the only
        // thing that differs between the two opportunities is the
        // narrative framing, so a player who learnt one learns the other.
        ResourceExpeditionDefinition definition =
            ResourceExpeditionRules.Definition(ResourceOpportunityKind.SpiritTrailSearch);

        Assert.Equal(180, definition.DurationTicks);
        Assert.Equal(4, definition.SetbackReturn);
        Assert.Equal(6, definition.PartialReturn);
        Assert.Equal(8, definition.FullReturn);
    }

    [Fact]
    public void Definition_ExposesADisplayName()
    {
        ResourceExpeditionDefinition definition =
            ResourceExpeditionRules.Definition(ResourceOpportunityKind.SpiritTrailSearch);

        Assert.False(string.IsNullOrWhiteSpace(definition.DisplayName));
    }

    [Fact]
    public void TheKindEnumSerializesAsAStringAndParsesBack()
    {
        // Resource opportunities persist as their enum name string
        // (see WorldPersistence). Enum.TryParse must round-trip the
        // new value so saves already on disk do not need a schema bump.
        string serialized = ResourceOpportunityKind.SpiritTrailSearch.ToString();
        Assert.True(
            System.Enum.TryParse(serialized, true, out ResourceOpportunityKind parsed));
        Assert.Equal(ResourceOpportunityKind.SpiritTrailSearch, parsed);
    }
}
