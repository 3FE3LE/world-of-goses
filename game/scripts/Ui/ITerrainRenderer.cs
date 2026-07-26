using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

#nullable enable

namespace WorldofGoses.Ui;

/// <summary>
/// Rendering contract for the macro city terrain. Today a single
/// implementation (<see cref="OrthogonalParcelTerrain"/>) draws the
/// parcel grid and tree sprites manually. The seam exists so that
/// when the terrain catalog grows past one tile type (suelo base +
/// suelo quarry + suelo farm + …) or past 16 parcels, a
/// <c>TileMapTerrainRenderer</c> implementation can be swapped in
/// without touching <c>CityMacroView</c> or the resource tree
/// pipeline.
///
/// <para>
/// The interface is intentionally minimal: it covers what the macro
/// view consumes today (resource plots, parcel grid, highlight)
/// and nothing more. Tile-specific authoring (autotile bitmask,
/// per-parcel palette) lives in the future implementation, not in
/// the contract.
/// </para>
/// </summary>
public interface ITerrainRenderer
{
    /// <summary>Number of parcels currently drawn.</summary>
    int VisibleParcelCount { get; }

    /// <summary>Number of resource trees currently drawn.</summary>
    int VisibleTreeCount { get; }

    /// <summary>
    /// Replaces the rendered resource plots with the given list. The
    /// terrain surface itself (parcel grid) is rendered once on
    /// <c>_Ready</c> and not part of the rebuild surface.
    /// </summary>
    void RenderResources(
        IReadOnlyList<CityMacroSnapshot.PlotItem> buildings,
        IReadOnlyList<Rect2>? occupiedGlobalRects = null,
        bool canGather = true,
        string gatherUnavailableReason = "");

    /// <summary>Highlights a single parcel (e.g. on placement mode). Null clears.</summary>
    void SetParcelHighlight(ParcelId? parcelId);
}
