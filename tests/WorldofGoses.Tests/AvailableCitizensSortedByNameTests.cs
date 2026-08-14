using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Pins the order returned by
/// <see cref="CityWorld.AvailableCitizensSortedByName"/> so the rename
/// (issue #53) cannot silently drift back to "by priority" while
/// keeping the alpha sort. The honesty check: changing
/// <see cref="Building.ConfigureProductionPolicy"/> values must NOT
/// change the order, because the field set a player used to drive
/// priority is gone.
/// </summary>
public class AvailableCitizensSortedByNameTests
{
    [Fact]
    public void AvailableCitizensSortedByName_OrdersByNameAscending()
    {
        var world = TestHelpers.NewProductionWorld();
        var freeCitizens = world.AvailableCitizens();
        Assert.True(freeCitizens.Count >= 2, "Test requires at least two available citizens.");

        // The seed assigns predictable names (Citizen-{id}, e.g. Citizen-3);
        // record the natural alpha-sorted order so the test stays
        // hermetic.
        var naturalAlpha = new System.Collections.Generic.List<string>(freeCitizens.Count);
        foreach (var c in freeCitizens) naturalAlpha.Add(c.Name);
        naturalAlpha.Sort(System.StringComparer.Ordinal);

        var actual = world.AvailableCitizensSortedByName();
        for (int i = 0; i < naturalAlpha.Count; i++)
        {
            Assert.Equal(naturalAlpha[i], actual[i].Name);
        }
    }

    [Fact]
    public void ChangingConfiguredMinMaxStock_DoesNotChangeOrder()
    {
        // Regression to the previous "by priority" lie: a future edit
        // that re-adds any non-name sort key must break this assertion.
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        quarry.ConfigureProductionPolicy(enabled: true, minStock: 1, maxStock: 4);

        var namesBefore = NamesOf(world.AvailableCitizensSortedByName());
        quarry.ConfigureProductionPolicy(enabled: true, minStock: 3, maxStock: 7);
        var namesAfter = NamesOf(world.AvailableCitizensSortedByName());

        Assert.Equal(namesBefore, namesAfter);
    }

    private static System.Collections.Generic.List<string> NamesOf(
        System.Collections.Generic.IReadOnlyList<Citizen> list)
    {
        var names = new System.Collections.Generic.List<string>(list.Count);
        foreach (var c in list) names.Add(c.Name);
        return names;
    }
}
