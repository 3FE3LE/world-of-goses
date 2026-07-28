using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class TerrainWearGridTests
{
    [Fact]
    public void FreshTile_IsNotWorn()
    {
        var grid = new TerrainWearGrid();
        Assert.False(grid.IsWorn(street: 0, tileIndex: 0));
    }

    [Fact]
    public void RepeatedTrample_CrossesDirtThreshold()
    {
        var grid = new TerrainWearGrid();
        int steps = (int)System.Math.Ceiling(TerrainWearGrid.DirtThreshold / TerrainWearGrid.WearPerTrample);
        for (int i = 0; i < steps - 1; i++) grid.Trample(street: 2, tileIndex: 5);
        Assert.False(grid.IsWorn(street: 2, tileIndex: 5));

        grid.Trample(street: 2, tileIndex: 5);
        Assert.True(grid.IsWorn(street: 2, tileIndex: 5));
    }

    [Fact]
    public void Wear_IsIsolatedPerStreetAndTileIndex()
    {
        var grid = new TerrainWearGrid();
        for (int i = 0; i < 20; i++) grid.Trample(street: 1, tileIndex: 3);

        Assert.True(grid.IsWorn(street: 1, tileIndex: 3));
        Assert.False(grid.IsWorn(street: 1, tileIndex: 4));
        Assert.False(grid.IsWorn(street: 0, tileIndex: 3));
    }

    [Fact]
    public void Wear_NeverExceedsFullSaturation()
    {
        var grid = new TerrainWearGrid();
        for (int i = 0; i < 1000; i++) grid.Trample(street: 0, tileIndex: 0);
        Assert.True(grid.IsWorn(street: 0, tileIndex: 0));
    }
}
