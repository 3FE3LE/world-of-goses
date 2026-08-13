#nullable enable
using WorldofGoses.Domain;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Single source for the lateral / depth projection the macro view applies to
/// ground resources, placement cells and placement windows (GitHub #30).
///
/// <para>Before this helper existed, three independent sites re-derived the
/// same formula:
/// <c>(frontageCenter - totalFrontageColumns / 2) * TileUnitPx</c>.
/// Each site had its own copy of the constant, its own off-by-one in
/// how it counts parcel columns, and its own notion of where a
/// resource's anchor sits within the construction row. A natural
/// resource and the placement cell that was supposed to be blocked
/// by it could disagree on the lateral coordinate by exactly one
/// tile, and the construction-grid underlay could divide a single
/// frontage cell into three apparent depth sub-cells. The fix is one
/// helper that every consumer reads from.</para>
///
/// <para>The helper is a pure presentation primitive. It does not own
/// gameplay authority (the domain's <see cref="BuildingReservation"/>
/// and <see cref="NaturalResourceUnitPosition"/> are still the single
/// source of truth), and it does not invent depth sub-cells: a
/// frontage cell is one strip, and a resource anchor falls inside
/// that strip.</para>
/// </summary>
internal static class MacroGroundProjection
{
    /// <summary>
    /// Total lateral frontage columns the visible city exposes, given
    /// the parcel grid in use. A fresh city has three unlocked
    /// parcels (9 frontage columns each) plus the locked neighbours;
    /// this constant lets callers normalise by the visible width.
    /// </summary>
    public static int TotalFrontageColumns(int worldParcelColumns) =>
        checked(worldParcelColumns * ParcelGrid.FrontageColumnsPerParcel);

    /// <summary>
    /// Lateral center of a one-tile-wide frontage cell measured from
    /// the leftmost frontage column of the visible city. The
    /// resource asset, the placement underlay, and the hit rect
    /// should all read this same value for the same logical cell.
    /// </summary>
    public static float FrontageCellCenter(int frontageColumn) =>
        frontageColumn + 0.5f;

    /// <summary>
    /// Lateral center of a multi-column window measured from the
    /// leftmost frontage column of the visible city. Used by the
    /// Basic Shelter preview and any other multi-column construction.
    /// </summary>
    public static float FrontageWindowCenter(int startColumn, int frontageColumns) =>
        startColumn + frontageColumns * 0.5f;

    /// <summary>
    /// Pixel offset of a one-tile-wide frontage cell, relative to the
    /// city center, before the camera lateral is subtracted. The
    /// caller is expected to subtract <c>CameraLateral</c> itself so
    /// this helper stays a pure geometry function.
    /// </summary>
    public static float LateralOffsetForCell(
        int frontageColumn,
        int worldParcelColumns) =>
        (FrontageCellCenter(frontageColumn)
            - TotalFrontageColumns(worldParcelColumns) * 0.5f)
            * MacroViewConstants.TileUnitPx;

    /// <summary>
    /// Pixel offset of a multi-column window, relative to the city
    /// center, before the camera lateral is subtracted.
    /// </summary>
    public static float LateralOffsetForWindow(
        int startColumn,
        int frontageColumns,
        int worldParcelColumns) =>
        (FrontageWindowCenter(startColumn, frontageColumns)
            - TotalFrontageColumns(worldParcelColumns) * 0.5f)
            * MacroViewConstants.TileUnitPx;

    /// <summary>
    /// The ground anchor of a natural-resource unit, expressed as a
    /// pixel offset from the city center. The asset still draws as a
    /// <c>1×1</c> tile in the first third of the construction row
    /// (see <see cref="MacroViewConstants.ResourceUnitBaseSizePx"/>);
    /// this helper returns the lateral coordinate of the cell the
    /// domain reports as blocked by that unit. The renderer's
    /// <c>DrawStreetObstacles</c> and the placement underlay both
    /// read this same value for the same logical unit.
    /// </summary>
    public static float ResourceAnchor(
        int globalFrontageColumn,
        int worldParcelColumns) =>
        LateralOffsetForCell(globalFrontageColumn, worldParcelColumns);

    /// <summary>
    /// Pixel width of one frontage cell. The placement underlay
    /// renders one of these per construction cell; the resource
    /// asset and the construction footprint preview share the
    /// same scalar so the strip and the asset line up.
    /// </summary>
    public static float CellWidthPx => MacroViewConstants.TileUnitPx;

    /// <summary>
    /// Pixel height of one construction row. The placement underlay
    /// uses this as its vertical span so the strip covers the whole
    /// 1×3 block, not three 1×1 sub-cells.
    /// </summary>
    public static float ConstructionRowHeightPx =>
        BuildingReservation.RequiredDepthRows * MacroViewConstants.TileUnitPx;
}
