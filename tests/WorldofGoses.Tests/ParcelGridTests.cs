using System;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class ParcelGridTests
{
    [Fact]
    public void Parcel_HasNineStandardLotsAtHalfTileResolution()
    {
        Assert.Equal(3, ParcelGrid.LotsPerAxis);
        Assert.Equal(6, ParcelGrid.HalfTilesPerStandardLot);
        Assert.Equal(18, ParcelGrid.HalfTilesPerParcel);
        Assert.Equal(new HalfTileRect(12, 12, 6, 6), ParcelGrid.StandardLot(2, 2));
    }

    [Fact]
    public void LegacyNaturalResourceUnitMapping_RemainsAvailableForMigration()
    {
        Assert.Equal((0, 0), ParcelGrid.NaturalResourceLot(0));
        Assert.Equal((1, 0), ParcelGrid.NaturalResourceLot(1));
        Assert.Equal((1, 2), ParcelGrid.NaturalResourceLot(7));
    }

    [Fact]
    public void StandardTemplates_PreserveFrontalAccess()
    {
        Assert.Equal(2, BuildingFootprintCatalog.StandardWithSideSetbacks.FrontClearance);
        Assert.Equal(2, BuildingFootprintCatalog.StandardFullWidth.FrontClearance);
    }

    [Theory]
    [InlineData(false, false, 0, 2, PassageClass.NarrowPassage)]
    [InlineData(false, true, 0, 1, PassageClass.Blocked)]
    [InlineData(true, true, 0, 0, PassageClass.Blocked)]
    [InlineData(false, false, 2, 4, PassageClass.Path)]
    [InlineData(true, true, 4, 4, PassageClass.Path)]
    public void AdjacentObstacles_ProduceExpectedClearance(
        bool leftFullWidth,
        bool rightFullWidth,
        int deliberatelyEmptyHalfTiles,
        int expectedClearance,
        PassageClass expectedClass)
    {
        ObstacleFootprintTemplate left = leftFullWidth
            ? BuildingFootprintCatalog.StandardFullWidth
            : BuildingFootprintCatalog.StandardWithSideSetbacks;
        ObstacleFootprintTemplate right = rightFullWidth
            ? BuildingFootprintCatalog.StandardFullWidth
            : BuildingFootprintCatalog.StandardWithSideSetbacks;

        int clearance = ParcelGrid.HorizontalClearance(
            left,
            deliberatelyEmptyHalfTiles,
            right);

        Assert.Equal(expectedClearance, clearance);
        Assert.Equal(expectedClass, ParcelGrid.ClassifyPassage(clearance));
    }

    [Fact]
    public void ObstacleTemplate_RejectsSolidAreaOutsideReservedLot()
    {
        Assert.Throws<ArgumentException>(() => new ObstacleFootprintTemplate(
            "invalid",
            new HalfTileRect(0, 0, 6, 6),
            new HalfTileRect(1, 0, 6, 6)));
    }

    [Fact]
    public void NaturalResourceProfile_LeavesAuthoredTraversalClearance()
    {
        ObstacleFootprintTemplate resource =
            NaturalResourceFootprintCatalog.StandardGroundResource;

        Assert.Equal(0, resource.LeftClearance);
        Assert.Equal(1, resource.RightClearance);
        Assert.Equal(1, resource.SolidArea.Width);
        Assert.Equal(2, resource.FrontClearance);
    }
}
