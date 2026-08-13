#nullable enable
using WorldofGoses.Domain;
using WorldofGoses.Prototypes;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Pure projection tests for <see cref="MacroGroundProjection"/> (GitHub #30).
///
/// <para>Before the helper existed, three sites — the resource renderer,
/// the building renderer and the placement underlay — each derived the
/// lateral coordinate by hand. The math agreed in spirit but each site
/// took a different shape, and the resource renderer even took its
/// parameter as <c>totalLotColumns</c> instead of <c>totalParcelColumns</c>.
/// That mismatch hid a one-tile drift between where a resource was
/// drawn and where the placement grid marked its cell as blocked.</para>
///
/// <para>These tests pin every public method of the helper against the
/// inline formulas the codebase used to carry. A future site that
/// reads through the helper and finds a discrepancy with the existing
/// rendering is failing for a real reason, not a refactor regression.</para>
/// </summary>
public sealed class MacroGroundProjectionTests
{
    [Fact]
    public void TotalFrontageColumns_ScalesWithParcelCount()
    {
        Assert.Equal(9, MacroGroundProjection.TotalFrontageColumns(1));
        Assert.Equal(27, MacroGroundProjection.TotalFrontageColumns(3));
        Assert.Equal(45, MacroGroundProjection.TotalFrontageColumns(5));
    }

    [Fact]
    public void LateralOffsetForCell_MatchesTheLegacyInlineFormula()
    {
        const int worldParcelColumns = 5;
        for (int column = 0; column < MacroGroundProjection.TotalFrontageColumns(worldParcelColumns); column++)
        {
            float total = MacroGroundProjection.TotalFrontageColumns(worldParcelColumns);
            float legacyCenter = column + 0.5f;
            float legacy = (legacyCenter - total * 0.5f) * MacroViewConstants.TileUnitPx;
            Assert.Equal(
                legacy,
                MacroGroundProjection.LateralOffsetForCell(column, worldParcelColumns),
                precision: 5);
        }
    }

    [Fact]
    public void LateralOffsetForWindow_MatchesTheLegacyInlineFormula()
    {
        const int worldParcelColumns = 5;
        // The Basic Shelter's minimum 3-column window is the smallest
        // the helper should ever see in production; sweep the slot
        // boundaries the player can actually click.
        int[] startColumns = { 0, 1, 2, 3, 6, 9, 12, 15 };
        int[] widths = { 3, 4, 5, 6 };
        foreach (int start in startColumns)
        {
            foreach (int width in widths)
            {
                if (start + width > MacroGroundProjection.TotalFrontageColumns(worldParcelColumns)) continue;
                float total = MacroGroundProjection.TotalFrontageColumns(worldParcelColumns);
                float legacyCenter = start + width * 0.5f;
                float legacy = (legacyCenter - total * 0.5f) * MacroViewConstants.TileUnitPx;
                Assert.Equal(
                    legacy,
                    MacroGroundProjection.LateralOffsetForWindow(start, width, worldParcelColumns),
                    precision: 5);
            }
        }
    }

    [Fact]
    public void ResourceAnchor_EqualsTheFrontageCellItBlocks()
    {
        // The single critical invariant of GitHub #30: the lateral
        // coordinate of a natural-resource unit must equal the
        // lateral coordinate of the placement cell that the domain
        // reports as `NaturalResource` for the same `(row, column)`.
        // Any drift between the two is exactly the "ghost cell"
        // bug the issue describes.
        const int worldParcelColumns = 5;
        for (int column = 0; column < MacroGroundProjection.TotalFrontageColumns(worldParcelColumns); column++)
        {
            float cellOffset = MacroGroundProjection.LateralOffsetForCell(column, worldParcelColumns);
            float resourceOffset = MacroGroundProjection.ResourceAnchor(column, worldParcelColumns);
            Assert.Equal(cellOffset, resourceOffset);
        }
    }

    [Fact]
    public void CellWidth_EqualsOneFrontageTile()
    {
        // The placement underlay and the resource asset are the same
        // scalar at the same scale. If either grows, the other must
        // move in lockstep or the visual alignment drifts.
        Assert.Equal(MacroViewConstants.TileUnitPx, MacroGroundProjection.CellWidthPx);
    }

    [Fact]
    public void ConstructionRowHeight_EqualsThreeFrontageTiles()
    {
        // The strip covers a full construction row (the 1×3 block
        // BuildingReservation.RequiredDepthRows encodes), not three
        // independent 1×1 sub-cells. Pinning the height here makes
        // the rule that the underlay renders one strip, not three.
        Assert.Equal(
            BuildingReservation.RequiredDepthRows * MacroViewConstants.TileUnitPx,
            MacroGroundProjection.ConstructionRowHeightPx);
    }

    [Fact]
    public void LateralOffsetForCell_ZeroAtTheCityCenter()
    {
        // The "city center" is the midpoint of the visible
        // frontage column span. A cell at that exact column should
        // have a zero lateral offset, which is the property the
        // legacy inline formula relied on when computing camera-
        // relative coordinates.
        int center = MacroGroundProjection.TotalFrontageColumns(3) / 2;
        Assert.Equal(0f, MacroGroundProjection.LateralOffsetForCell(center, 3));
    }
}
