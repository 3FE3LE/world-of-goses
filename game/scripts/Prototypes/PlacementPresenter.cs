#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Owns the placement-mode presentation state (A4): whether placement is
/// active, which construction kind the player chose, the projected lot and
/// cell boxes, and the hovered / selected lot. The view drives the
/// lifecycle through <see cref="Begin"/> and <see cref="End"/>; authorization
/// still flows through the controller via the view, and the dock's
/// instruction text is still composed by the view, which owns the
/// localisation keys.
///
/// <para>The presenter deliberately does <em>not</em> own the clickable
/// rects: those are published per-frame by the renderer into
/// <see cref="MacroHitRects.PlacementRects"/>, the single hit-rect bag the
/// whole macro view shares. <see cref="TryFindNearestLot"/> therefore takes
/// the rect list as an argument, the same shape
/// <see cref="MacroInteractionController.TryFindHoveredTree"/> uses.</para>
/// </summary>
internal sealed class PlacementPresenter
{
    /// <summary>One candidate three-column frontage window projected to the
    /// macro perspective, plus the source <see cref="ConstructionPlacementSnapshot.WindowItem"/>
    /// the rendering and the selection logic consume.</summary>
    public readonly record struct PlacementLotBox(
        ConstructionPlacementSnapshot.WindowItem Window,
        int Street,
        float LateralOffset,
        float Width,
        float Height);

    /// <summary>One individual frontage cell projected to the macro
    /// perspective. The cells compose the lot boxes; the renderer draws
    /// them as the underlay beneath the highlighted three-column window.</summary>
    public readonly record struct PlacementCellBox(
        ConstructionPlacementSnapshot.CellItem Cell,
        int Street,
        float LateralOffset,
        float Width,
        float Height);

    private readonly List<PlacementLotBox> _placementLots = new();
    private readonly List<PlacementCellBox> _placementCells = new();
    private bool _placementActive;
    private ConstructionKind _placementKind;
    private ConstructionLot? _selectedPlacementLot;
    private PlacementLotBox? _hoveredPlacementLot;
    private string _placementBaseInstruction = string.Empty;

    /// <summary>Read-only view of the projected lots. The renderer
    /// iterates this list when drawing the placement footprint.</summary>
    public IReadOnlyList<PlacementLotBox> PlacementLots => _placementLots;

    /// <summary>Read-only view of the projected cells. The renderer
    /// iterates this list when drawing the per-cell overlay.</summary>
    public IReadOnlyList<PlacementCellBox> PlacementCells => _placementCells;

    /// <summary>True while placement is active. Placement mode is
    /// exclusive: the view gates world clicks, hover, gathering and the
    /// primary navigation dock on this flag.</summary>
    public bool PlacementActive => _placementActive;

    /// <summary>Construction kind the player chose. Only meaningful while
    /// <see cref="PlacementActive"/> is true.</summary>
    public ConstructionKind PlacementKind => _placementKind;

    /// <summary>Selected lot — the player has clicked one. <c>null</c>
    /// when they have hovered but not clicked.
    ///
    /// <para>Declared <c>internal</c>, not <c>public</c>, for the same
    /// reason <c>CityWorldController.AvailableConstructionLots</c> is:
    /// <see cref="ConstructionLot"/> is a domain type, and
    /// <c>ArchitectureBoundaryTests.Presentation_DoesNotExposeMutableDomainEntities</c>
    /// forbids presentation from publishing domain entities on a public
    /// surface. The type is an immutable record struct, so the risk is
    /// nominal here — but the rule is enforced by shape, not by
    /// case-by-case judgement, and the enclosing class is
    /// <c>internal</c> anyway, so nothing is actually narrowed.</para></summary>
    internal ConstructionLot? SelectedPlacementLot => _selectedPlacementLot;

    /// <summary>Hovered lot — the pointer is over a placement rect.</summary>
    public PlacementLotBox? HoveredPlacementLot => _hoveredPlacementLot;

    /// <summary>Base instruction string the dock shows while placement is
    /// active and nothing is hovered or selected. Composed and localised
    /// by the view, stored here because it outlives a single frame.</summary>
    public string PlacementBaseInstruction => _placementBaseInstruction;

    /// <summary>Enters placement mode for <paramref name="kind"/>. Clears
    /// any lot projected by a previous run; the view then re-projects the
    /// snapshot through <see cref="AddLot"/> / <see cref="AddCell"/>.</summary>
    public void Begin(ConstructionKind kind, string baseInstruction)
    {
        _placementActive = true;
        _placementKind = kind;
        _selectedPlacementLot = null;
        _hoveredPlacementLot = null;
        _placementBaseInstruction = baseInstruction;
        _placementLots.Clear();
        _placementCells.Clear();
    }

    /// <summary>Leaves placement mode and drops every projected box. The
    /// view separately clears <see cref="MacroHitRects.PlacementRects"/>,
    /// which it owns.</summary>
    public void End()
    {
        _placementActive = false;
        _selectedPlacementLot = null;
        _hoveredPlacementLot = null;
        _placementLots.Clear();
        _placementCells.Clear();
    }

    /// <summary>Adds one projected frontage window.</summary>
    public void AddLot(PlacementLotBox lot) => _placementLots.Add(lot);

    /// <summary>Adds one projected frontage cell.</summary>
    public void AddCell(PlacementCellBox cell) => _placementCells.Add(cell);

    /// <summary>Marks the selected lot, or clears the selection when the
    /// window the player clicked is not buildable.</summary>
    public void SelectLot(PlacementLotBox lot) =>
        _selectedPlacementLot = lot.Window.IsValid ? lot.Window.Lot : null;

    /// <summary>Records the hovered lot and reports whether it changed, so
    /// the view only recomposes the dock text and redraws on a
    /// transition rather than every mouse-motion event.</summary>
    public bool SetHoveredLot(PlacementLotBox? lot)
    {
        if (_hoveredPlacementLot == lot) return false;
        _hoveredPlacementLot = lot;
        return true;
    }

    /// <summary>Returns the lot whose rect contains
    /// <paramref name="position"/> and whose centre is nearest to it, or
    /// <c>null</c> when the pointer is over no lot. Nearest-centre rather
    /// than first-hit because projected lot rects overlap in perspective:
    /// picking the first would make the choice depend on draw order.</summary>
    public static PlacementLotBox? TryFindNearestLot(
        Vector2 position,
        IReadOnlyList<(Rect2 Rect, PlacementLotBox Lot)> placementRects)
    {
        PlacementLotBox? nearest = null;
        float nearestDistanceSquared = float.MaxValue;
        foreach ((Rect2 rect, PlacementLotBox lot) in placementRects)
        {
            if (!rect.HasPoint(position)) continue;
            float distanceSquared = position.DistanceSquaredTo(rect.GetCenter());
            if (distanceSquared >= nearestDistanceSquared) continue;
            nearest = lot;
            nearestDistanceSquared = distanceSquared;
        }
        return nearest;
    }
}
