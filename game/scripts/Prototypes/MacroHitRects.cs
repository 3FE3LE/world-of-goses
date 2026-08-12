#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Shared hit-rect bag for the macro street view (A4). The renderer fills
/// the lists in <see cref="MacroStreetLiveView._Draw"/>; the interaction
/// controller reads them in the next <see cref="MacroStreetLiveView._Process"/>
/// to resolve hover/click. This is the seam between "what the renderer drew"
/// and "what the pointer can hit" — a record-bag, not an interface, so the
/// two collaborators share the same instance without introducing a new
/// abstract type.
///
/// The lists are mutable so the renderer can write to them without
/// copying each frame. The view owns the single instance and passes it to
/// both the renderer and the interaction controller.
/// </summary>
internal sealed class MacroHitRects
{
    public List<(Rect2 Rect, int BuildingId)> BuildingClickableRects { get; } = new();
    public List<(Rect2 Rect, MacroStreetRenderer.TreeBox Tree)> TreeClickableRects { get; } = new();
    public List<(Rect2 Rect, CitizenId CitizenId)> CitizenClickableRects { get; } = new();
    public List<(Rect2 Rect, MacroStreetRenderer.PlotBox Plot)> StorageBadgeRects { get; } = new();
    public List<(Rect2 Rect, PlacementPresenter.PlacementLotBox Lot)> PlacementRects { get; } = new();

    /// <summary>Clears every list at the top of the renderer's draw pass.
    /// Same order the view used before extraction: clear, then fill in
    /// the renderer, then read in the next interaction update.</summary>
    public void Clear()
    {
        BuildingClickableRects.Clear();
        TreeClickableRects.Clear();
        CitizenClickableRects.Clear();
        StorageBadgeRects.Clear();
        PlacementRects.Clear();
    }
}
