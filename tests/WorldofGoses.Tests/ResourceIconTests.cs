using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Tests for the resource presentation helpers added in the iconography /
/// top-ticker scalability pass. Pure logic — the rendered pixels are
/// covered by the visual matrix, the strings and the ordering rules are
/// not.
/// </summary>
public sealed class ResourceIconTests
{
    // ── Compact number formatter ────────────────────────────────────────

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(42, "42")]
    [InlineData(999, "999")]
    public void CompactNumber_NaturalRange_IsUnchanged(int value, string expected)
    {
        Assert.Equal(expected, CompactNumber.Format(value));
    }

    [Theory]
    [InlineData(1_000, "1K")]
    [InlineData(1_200, "1.2K")]
    [InlineData(18_400, "18.4K")]
    [InlineData(999_500, "999.5K")]
    [InlineData(999_999, "1M")]
    public void CompactNumber_Thousands_CollapsesToOneDecimal(int value, string expected)
    {
        Assert.Equal(expected, CompactNumber.Format(value));
    }

    [Theory]
    [InlineData(1_000_000, "1M")]
    [InlineData(1_100_000, "1.1M")]
    [InlineData(12_345_678, "12.3M")]
    public void CompactNumber_Millions_CollapsesToOneDecimal(int value, string expected)
    {
        Assert.Equal(expected, CompactNumber.Format(value));
    }

    [Fact]
    public void CompactNumber_Negative_IsHandled()
    {
        Assert.Equal("-42", CompactNumber.Format(-42));
        Assert.Equal("-1.2K", CompactNumber.Format(-1_200));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(999, "999")]
    [InlineData(1_200, "1,200")]
    [InlineData(18_400, "18,400")]
    [InlineData(1_100_000, "1,100,000")]
    public void CompactNumber_Exact_PreservesThousandsSeparator(int value, string expected)
    {
        // Tooltips must keep the exact amount, even when the chip shows
        // the compact form. The thousands separator is required so the
        // player can read the precise total without a second pass.
        Assert.Equal(expected, CompactNumber.FormatExact(value));
    }

    [Fact]
    public void CompactNumber_CompactAndExact_DisagreeOnLargeValues()
    {
        // The whole point of the helper: compact is for the chip,
        // exact is for the tooltip. The two must never agree on a
        // value that crosses the thousand threshold.
        string compact = CompactNumber.Format(18_400);
        string exact = CompactNumber.FormatExact(18_400);
        Assert.NotEqual(compact, exact);
    }

    // ── Resource priority ───────────────────────────────────────────────

    [Fact]
    public void ResourcePriority_Sequence_PlacesSurvivalBeforeConstruction()
    {
        var list = ResourcePriority.Sequence.ToList();
        int foodIndex = list.IndexOf(ResourceType.Food);
        int woodIndex = list.IndexOf(ResourceType.Wood);
        int ironIndex = list.IndexOf(ResourceType.Iron);

        Assert.True(foodIndex >= 0);
        Assert.True(woodIndex > foodIndex, "Wood must come after Food in the priority sequence.");
        Assert.True(ironIndex > woodIndex, "Iron must come after the construction inputs.");
    }

    [Fact]
    public void ResourcePriority_Sequence_CoversEveryResourceType()
    {
        // The sequence must cover the entire current catalog, otherwise
        // a future addition slips through unranked.
        foreach (ResourceType resource in Enum.GetValues<ResourceType>())
        {
            Assert.Contains(resource, ResourcePriority.Sequence);
        }
    }

    [Fact]
    public void ResourcePriority_Survival_HoldsFoodAndWildFood()
    {
        Assert.Contains(ResourceType.Food, ResourcePriority.Survival);
        Assert.Contains(ResourceType.WildFood, ResourcePriority.Survival);
        Assert.DoesNotContain(ResourceType.Wood, ResourcePriority.Survival);
    }

    [Fact]
    public void ResourcePriority_ConstructionInputs_HoldsEveryFoundingSiteInput()
    {
        // The founding-site modules (Campfire, Bedroll, Cache, Canopy) and
        // the Basic Shelter consume this set. Anything new consumed by
        // construction must land here, not in the trailing "remaining"
        // tier.
        Assert.Contains(ResourceType.Wood, ResourcePriority.ConstructionInputs);
        Assert.Contains(ResourceType.Stone, ResourcePriority.ConstructionInputs);
        Assert.Contains(ResourceType.Branches, ResourcePriority.ConstructionInputs);
        Assert.Contains(ResourceType.PlantFiber, ResourcePriority.ConstructionInputs);
        Assert.Contains(ResourceType.SmallStone, ResourcePriority.ConstructionInputs);
        Assert.DoesNotContain(ResourceType.Iron, ResourcePriority.ConstructionInputs);
        Assert.DoesNotContain(ResourceType.Potions, ResourcePriority.ConstructionInputs);
    }

    [Fact]
    public void ResourcePriority_Prioritize_ReordersByCanonicalSequence()
    {
        var snapshot = new List<ResourceInventoryItem>
        {
            new(ResourceType.Iron, 5, 5),
            new(ResourceType.Branches, 1, 1),
            new(ResourceType.Food, 100, 90),
            new(ResourceType.Wood, 40, 40),
        };

        IReadOnlyList<ResourceInventoryItem> ordered = ResourcePriority.Prioritize(snapshot);

        // Survival first, then construction inputs in the canonical order,
        // then remaining. Iron is "remaining" and must come last.
        Assert.Equal(
            new[] { ResourceType.Food, ResourceType.Wood, ResourceType.Branches, ResourceType.Iron },
            ordered.Select(item => item.Resource).ToArray());
    }

    [Fact]
    public void ResourcePriority_Prioritize_SkipsMissingResources()
    {
        var snapshot = new List<ResourceInventoryItem>
        {
            new(ResourceType.Food, 50, 50),
            new(ResourceType.Iron, 2, 2),
        };

        IReadOnlyList<ResourceInventoryItem> ordered = ResourcePriority.Prioritize(snapshot);

        Assert.Equal(2, ordered.Count);
        Assert.Equal(ResourceType.Food, ordered[0].Resource);
        Assert.Equal(ResourceType.Iron, ordered[1].Resource);
    }

    [Fact]
    public void ResourcePriority_Prioritize_HandlesEmptyInput()
    {
        IReadOnlyList<ResourceInventoryItem> ordered =
            ResourcePriority.Prioritize(Array.Empty<ResourceInventoryItem>());
        Assert.Empty(ordered);
    }
}
