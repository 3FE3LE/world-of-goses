#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Owns the pointer / hover / selection state of the macro street view (A4).
/// The controller's reads and writes move the visual selection bubbles,
/// the world-status overlays, and the cursor without touching the domain
/// or the rest of the city. The view remains the composition root: it
/// builds the controller in <c>_Ready</c>, wires its public callbacks,
/// and forwards pointer events to it.
/// </summary>
internal sealed class MacroInteractionController
{
    /// <summary>What the pointer landed on in the world. The order of the
    /// members is the resolution order: a tree in front of a building wins
    /// the click, because the tree is what the player sees there.</summary>
    public enum MacroHitKind
    {
        None,
        Tree,
        Citizen,
        Building,
    }

    /// <summary>One resolved pointer hit. Only the field matching
    /// <see cref="Kind"/> carries meaning; the rest are default. A record
    /// struct rather than three out-parameters so the call sites can
    /// pattern-match on <see cref="Kind"/> and read the payload in one
    /// step.</summary>
    public readonly record struct MacroHit(
        MacroHitKind Kind,
        Rect2 Rect,
        MacroStreetRenderer.TreeBox Tree,
        CitizenId Citizen,
        int BuildingId)
    {
        /// <summary>The pointer hit nothing: empty ground.</summary>
        public static MacroHit None => new(MacroHitKind.None, default, default, default, 0);
    }

    /// <summary>Resolves what the pointer at <paramref name="position"/>
    /// landed on, testing trees, then citizens, then buildings.
    ///
    /// <para>Left click and right click resolve the same target — they only
    /// differ in what they then do with it (select vs act). Keeping one
    /// hit-test is what guarantees that: before A4 the priority order was
    /// written out twice, so the two buttons could silently drift apart and
    /// a right click could act on something other than what a left click in
    /// the same pixel would have selected.</para></summary>
    public MacroHit HitTest(Vector2 position, MacroHitRects hitRects)
    {
        foreach ((Rect2 rect, MacroStreetRenderer.TreeBox tree) in hitRects.TreeClickableRects)
        {
            if (rect.HasPoint(position))
                return new MacroHit(MacroHitKind.Tree, rect, tree, default, 0);
        }
        foreach ((Rect2 rect, CitizenId citizenId) in hitRects.CitizenClickableRects)
        {
            if (rect.HasPoint(position))
                return new MacroHit(MacroHitKind.Citizen, rect, default, citizenId, 0);
        }
        foreach ((Rect2 rect, int buildingId) in hitRects.BuildingClickableRects)
        {
            if (rect.HasPoint(position))
                return new MacroHit(MacroHitKind.Building, rect, default, default, buildingId);
        }
        return MacroHit.None;
    }

    private MacroStreetRenderer.TreeBox? _selectedTree;
    private int? _selectedBuildingId;
    private CitizenId? _selectedCitizenId;
    private int? _hoveredCitizenId;
    private int? _hoveredStorageBuildingId;
    private CitizenId? _visualStatusCitizenId;
    private bool _selectionIsMacro = true;

    private WorldStatusBubble _worldStatusBubble = null!;
    private CursorController? _cursorController;

    /// <summary>Bound to the view's own host so the controller can add or
    /// reparent child nodes (the world-status bubble).</summary>
    public void Attach(WorldStatusBubble bubble, CursorController? cursor)
    {
        _worldStatusBubble = bubble;
        _cursorController = cursor;
    }

    /// <summary>Bookkeeping read for the view's HUD: which entity currently
    /// owns the contextual selection. The hierarchy body renders the
    /// selection, not the controller.</summary>
    public bool SelectionIsMacro
    {
        get => _selectionIsMacro;
        set => _selectionIsMacro = value;
    }

    /// <summary>Selected building id, or <c>null</c> when no building is
    /// selected. The view's <c>SelectBuilding</c> call on the controller
    /// triggers a building-detail view through the controller facade.</summary>
    public int? SelectedBuildingId
    {
        get => _selectedBuildingId;
        set => _selectedBuildingId = value;
    }

    /// <summary>Selected citizen id, or <c>null</c> when no citizen is
    /// selected. The controller's id is the only source for the citizen
    /// hover bubble.</summary>
    public CitizenId? SelectedCitizenId
    {
        get => _selectedCitizenId;
        set => _selectedCitizenId = value;
    }

    /// <summary>Selected tree, or <c>null</c> when no tree is selected.
    /// The gather menu reads through this property.</summary>
    public MacroStreetRenderer.TreeBox? SelectedTree
    {
        get => _selectedTree;
        set => _selectedTree = value;
    }

    /// <summary>Citizen id whose status bubble is currently pinned (a
    /// fixture for the visual regression matrix).</summary>
    public CitizenId? VisualStatusCitizenId
    {
        get => _visualStatusCitizenId;
        set => _visualStatusCitizenId = value;
    }

    /// <summary>Marked selection cleared. The view calls this when the
    /// pointer has nothing to hit and the contextual inspector must hide.</summary>
    public void ClearSelection()
    {
        _selectedTree = null;
        _selectedBuildingId = null;
        _selectedCitizenId = null;
    }

    /// <summary>Mark a tree as the contextual selection. The view's
    /// overlay renders through the inspector's <c>ShowSelection</c>.</summary>
    public void SelectTree(MacroStreetRenderer.TreeBox tree) => _selectedTree = tree;

    /// <summary>Mark a building as the contextual selection.</summary>
    public void SelectBuilding(BuildingId id) => _selectedBuildingId = id.Value;

    /// <summary>Mark a citizen as the contextual selection.</summary>
    public void SelectCitizen(CitizenId id) => _selectedCitizenId = id;

    /// <summary>Capture the current citizen id for the visual regression
    /// fixture. The view's <c>ShowCitizenStatusForVisualRegression</c>
    /// halo just reads through this.</summary>
    public void SetVisualStatusCitizen(CitizenId id) => _visualStatusCitizenId = id;

    /// <summary>Reset hover state. Called by the view's
    /// <c>OnWorldChanged</c> pass so a vanished entity doesn't leave a
    /// stale hover bubble.</summary>
    public void OnWorldChanged()
    {
        _hoveredCitizenId = null;
        _hoveredStorageBuildingId = null;
    }

    /// <summary>Current citizen hover id. The view's
    /// <c>TryFindHoveredCitizen</c> writes through this property.</summary>
    public int? HoveredCitizenId
    {
        get => _hoveredCitizenId;
        set => _hoveredCitizenId = value;
    }

    /// <summary>Current storage badge hover id. The view's
    /// <c>IsCursorOverStorageBadge</c> reads and writes through this.</summary>
    public int? HoveredStorageBuildingId
    {
        get => _hoveredStorageBuildingId;
        set => _hoveredStorageBuildingId = value;
    }

    /// <summary>Cursor controller (singleton). The view's gather / tree
    /// hover code reads through this property.</summary>
    public CursorController? CursorController => _cursorController;

    /// <summary>World-status bubble. The view's hover code reads and
    /// writes through this property.</summary>
    public WorldStatusBubble WorldStatusBubble => _worldStatusBubble;

    // ---------- Hover state ----------

    private bool _treeHovered;
    private ResourceType _hoveredResource = ResourceType.Wood;

    /// <summary>Whether the pointer is currently over a tree hit-rect.
    /// The view reads this from <c>UpdateTreeHover</c> and writes
    /// through the helper.</summary>
    public bool TreeHovered
    {
        get => _treeHovered;
        set => _treeHovered = value;
    }

    /// <summary>Current resource under the cursor. The view's
    /// <c>UpdateTreeHover</c> uses this to swap the gather tool.</summary>
    public ResourceType HoveredResource
    {
        get => _hoveredResource;
        set => _hoveredResource = value;
    }

    /// <summary>Clears the world status bubble. The view's
    /// <c>UpdateWorldHover</c> calls this when no hit is registered
    /// (or the pointer leaves the world).</summary>
    public void ClearWorldStatusHover()
    {
        _hoveredCitizenId = null;
        _hoveredStorageBuildingId = null;
        _worldStatusBubble.Hide();
    }

    /// <summary>Returns true if the local mouse is over a storage-full
    /// badge. The view's <c>TryRightClick</c> uses this to decide
    /// between gather and open-building right-click semantics.</summary>
    public bool IsCursorOverStorageBadge(Vector2 localMouse, IReadOnlyList<(Rect2 Rect, MacroStreetRenderer.PlotBox Plot)> storageBadgeRects)
    {
        foreach ((Rect2 rect, MacroStreetRenderer.PlotBox _) in storageBadgeRects)
        {
            if (rect.HasPoint(localMouse)) return true;
        }
        return false;
    }

    /// <summary>Returns true if the local mouse is over a tree hit-rect
    /// and writes the resource under the cursor. The view's
    /// <c>UpdateTreeHover</c> uses this to detect the resource transition
    /// between wood, branches, fibre, and stone.</summary>
    public bool TryFindHoveredTree(
        Vector2 mousePosition,
        IReadOnlyList<(Rect2 Rect, MacroStreetRenderer.TreeBox Tree)> treeClickableRects,
        out ResourceType resource)
    {
        foreach ((Rect2 rect, MacroStreetRenderer.TreeBox unit) in treeClickableRects)
        {
            if (!rect.HasPoint(mousePosition)) continue;
            resource = unit.ResourceType;
            return true;
        }
        resource = ResourceType.Wood;
        return false;
    }
}
