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
    /// <summary>World (terrain, trees, macro plots, HUD chips). Default.</summary>
    public const int World = 0;

    /// <summary>Contextual menu anchored to an in-world resource.</summary>
    public const int ContextMenu = 8;

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

    /// <summary>Tutorial overlay above macro + modals, below pause.</summary>
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
