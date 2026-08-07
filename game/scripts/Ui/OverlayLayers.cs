using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Semantic catalog of overlay layers. Every overlay, banner, modal, and
/// toast in the scene tree reads its <see cref="CanvasItem.ZIndex"/> from
/// one of these constants via <see cref="Apply"/>. The numeric gaps make
/// room for in-between layers without re-shuffling every consumer.
///
/// Why a static class instead of per-scene <c>z_index = N</c>:
///   - the catalog documents what may occlude what;
///   - new overlays get a named slot instead of guessing a number;
///   - the visual regression matrix can assert the layer order from a
///     single source of truth.
///
/// The constants are intentionally not an enum: the runtime needs an
/// <c>int</c> for <c>ZIndex</c>, and an enum would force a cast at every
/// call site.
/// </summary>
public static class OverlayLayers
{
    /// <summary>World (terrain, trees, macro plots). Default.
    /// This layer is diegetic: everything on it is part of the city the
    /// player is looking at, so <see cref="AmbientTint"/> is allowed to
    /// colour it. HUD chrome must NOT sit here — see <see cref="Hud"/>.</summary>
    public const int World = 0;

    /// <summary>
    /// Base z for the macro view itself. The view sorts its own contents by
    /// depth — one child layer per street band, plus the citizen carriers —
    /// and needs a whole range of positive child indices to do it. Children
    /// are relative to their parent, so parking the view here keeps that
    /// entire range below <see cref="World"/> and therefore below the ambient
    /// tint, the HUD and every overlay above them.
    /// </summary>
    public const int WorldDepthBase = -256;

    /// <summary>Ambient day/night tint over the world. Sits above
    /// <see cref="World"/> and below <see cref="Hud"/>: it is an
    /// immersion effect for the map, never a wash over the interface.
    /// A control that gets tinted when it should not is almost always a
    /// control that forgot to claim <see cref="Hud"/>.</summary>
    public const int AmbientTint = 5;

    /// <summary>Persistent HUD chrome: status strip, macro action bar,
    /// and the full-screen views that replace the map (building detail,
    /// hero profile). Above <see cref="AmbientTint"/> so the interface
    /// keeps its authored colours at every hour of the in-game day.</summary>
    public const int Hud = 6;

    /// <summary>Contextual menu anchored to an in-world resource.</summary>
    public const int ContextMenu = 8;

    /// <summary>Bottom-left anchored selection details (SelectionInfoPanel).</summary>
    public const int SelectionInfo = 9;

    /// <summary>Bottom-right anchored event log (OfflineReportPanel).</summary>
    public const int Chronicle = 10;

    /// <summary>Full-viewport scrim owned by ModalHost.</summary>
    public const int ModalScrim = 20;

    /// <summary>Modal body sitting on top of <see cref="ModalScrim"/>
    /// (ConstructionPanel, ExpeditionPanel, MigrantPanel).</summary>
    public const int Modal = 21;

    /// <summary>Construction placement overlay (selected-blueprint mode).
    /// Sits above the modal layer so the lot overlay remains visible
    /// while the construction modal is still open underneath.</summary>
    public const int PlacementOverlay = 40;

    /// <summary>Authored guidance above macro + modals, below pause.
    /// Reserved and currently unclaimed: the three-step modal
    /// <c>TutorialOverlay</c> that used to own it was deleted because its
    /// hand-written copy had drifted out of step with the real recipes and
    /// status strip. The slot is kept for the first-night dialogue surface,
    /// which must occlude the construction and expedition modals without
    /// hiding the pause menu or Notifier toasts.</summary>
    public const int Tutorial = 50;

    /// <summary>Founding-hero astral onboarding (12-step narrative).</summary>
    public const int Onboarding = 80;

    /// <summary>Founder arrival sequence (post-onboarding fall + title card).</summary>
    public const int FounderArrival = 90;

    /// <summary>Pause menu (top-level scrim) and the Notifier CanvasLayer.
    /// Both end up on top because the Notifier sits on its own CanvasLayer
    /// (Godot's separate axis from <c>ZIndex</c>).</summary>
    public const int PauseAndNotifier = 100;

    /// <summary>Assigns the layer constant to a Control's <see cref="CanvasItem.ZIndex"/>.
    /// Idempotent; safe to call from <c>_Ready</c> repeatedly.</summary>
    public static void Apply(Control node, int layer)
    {
        if (node is null) return;
        node.ZIndex = layer;
    }
}
