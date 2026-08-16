#nullable enable
using Godot;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Single source of numeric truth and shared color values for the macro
/// street view (A4). Every collaborator composing
/// <see cref="MacroStreetLiveView"/> reads from here so the projection,
/// the quantized zoom, the building-entry push, and the placement palette
/// never drift out of sync.
///
/// The view itself keeps <c>private const</c> / <c>private static readonly</c>
/// forwarders to these fields so the existing codebase keeps its in-class
/// naming and the existing test surface (<c>MacroStreetLiveView.PacedRouteSteps</c>
/// etc.) keeps compiling unchanged. Future collaborators reference this
/// class directly.
/// </summary>
internal static class MacroViewConstants
{
    // Camera projection (ScreenContent-local space).
    public const float CenterX = 640f;
    public const float BaseY = 580f;
    public const float CameraZoomPivotY = 680f;
    // A lot is three tiles across and the art grid is 32, so 96 rather than
    // the 90 this carried while the world was drawn from 16 px placeholders.
    // See visual-language.md, "La rejilla de 32": the geometry follows the art,
    // not the other way round.
    public const float LotUnitPx = 96f;
    public const int DefaultWorldParcelColumns = 5;
    public const int DefaultWorldParcelRows = 2;

    // Quantized zoom: discrete steps, never a continuous drag/slider — and now
    // integer ones. At zoom 1 a source pixel is a logical pixel; at 2 it is a
    // 2x2 block, at 3 a 3x3. The old 1.30/1.45/3.00 ladder in steps of 0.15
    // drew 32 px art at 46.4 px, which is neither 1:1 nor a whole multiple of
    // it, and contradicted the integer-scale rule the visual language already
    // stated. Two is the default because it is where 32 px art reads at its
    // full detail; one is the wide view.
    public const float ZoomStep = 1f;
    public const float MinZoom = 1f;
    public const float DefaultZoom = 2f;
    public const float MaxZoom = 3f;

    // Holding vertical pan repeats slowly at first, then gently accelerates.
    public const float VerticalPanInitialRepeatSeconds = 0.48f;
    public const float VerticalPanMinimumRepeatSeconds = 0.26f;
    public const float VerticalPanAccelerationSeconds = 3f;
    public const float VerticalPanMaximumTransitionMultiplier = 1.55f;

    // Same cadence discipline as the earlier prototypes (design bible §08,
    // "Pixel-motion grammar"): no continuous tweening.
    public const int TransitionSteps = 10;
    public const float DepthStepSize = 1f / TransitionSteps;

    // Building-entry camera push: a handful of DISCRETE zoom steps toward
    // the clicked building (same stepped cadence as citizen/camera motion —
    // never a continuous Tween).
    public const int BuildingEntryZoomSteps = 10;
    // The closest rung, not a value between rungs: entering a building is the
    // one moment the macro pushes past its default. It was 1.75 against a 1.45
    // default; on an integer ladder the equivalent push is 2 -> 3.
    public const float BuildingEntryZoomLevel = MaxZoom;

    // Floor tile grain. ParcelGrid.TilesPerStandardLot is 3, so a lot is
    // composed of 3×3 base tiles per row.
    public const float TileUnitPx = LotUnitPx / 3f;
    // One resource unit owns one frontage cell. Its visual canvas therefore
    // stays within that cell instead of visually claiming a whole 3×3 lot.
    public const float ResourceUnitBaseSizePx = TileUnitPx;
    // Lateral span a living tree blocks when crossing its band.
    public const float RouteClearancePx = 14f;
    // Granularity when scanning a band for a viable crossing point.
    public const float CrossingScanStepPx = 6f;
    // LPC frames center the body; feet sit ~28 frame px below center, which
    // is 7 px at the carrier's 0.25 macro scale (before depth scaling).
    public const float HeroFootOffsetMacroPx = 7f;
    // Chunky pixel-grid step for the floor's staircase edges.
    public const float PixelStepPx = 2f;

    // Vertical spacing between consecutive street bands in z. Leaves room
    // for a citizen to land between two bands rather than tying with one.
    public const int BandZStep = 4;

    // Z-index bounds used by MacroProjectionHelpers.DepthToZ. Symmetric and
    // deliberately generous so the band stack can never clip under Godot's
    // 16-bit signed z range.
    public const int ZIndexMin = -4000;
    public const int ZIndexMax = 4000;

    // Ground biome atlas coordinates in the shared Kenney roguelike sheet.
    public const int GrassAtlasColumn = 5;
    public const int DirtAtlasColumn = 6;
    public const int StoneAtlasColumn = 7;
    public const int GroundAtlasRowA = 0;
    public const int GroundAtlasRowB = 1;

    public const float StatusBadgeSize = 24f;
    public const float StatusBadgeBorder = 2f;

    // Camera mode (design bible §04 "Cámara-sigue"): free pan by default;
    // follow-the-founder requires the explicit toggle.
    public const bool DefaultCameraFollowsHero = false;

    // Scene paths for transient menus parented to ScreenContent.
    public const string ResourceActionMenuScenePath = "res://scenes/Components/ResourceActionMenu.tscn";
    public const string CultivationActionMenuScenePath = "res://scenes/Components/CultivationActionMenu.tscn";

    // ---------- Colors ----------

    public static readonly Color BuildingColor = new("#8a7a54");
    public static readonly Color UnderConstructionModulate = new(0.55f, 0.55f, 0.55f);
    public static readonly Color PlacementAvailableColor = new("#2f8f5b22");
    public static readonly Color PlacementHoveredValidColor = new("#45c87866");
    public static readonly Color PlacementHoveredInvalidColor = new("#c94f4f70");
    public static readonly Color PlacementBlockedCellColor = new("#8f3f3f2e");
    public static readonly Color PlacementGridColor = new("#d8cda566");
    // Territory tints: opaque for Locked so the player cannot mistake
    // it for buildable ground; progressively lighter for intermediate
    // states so the visual cost of an expedition reads at a glance.
    public static readonly Color LockedParcelColor = new(0.08f, 0.07f, 0.05f, 0.78f);
    public static readonly Color ReconnoitredParcelColor = new(0.86f, 0.72f, 0.28f, 0.32f);
    public static readonly Color RouteSecuredParcelColor = new(0.47f, 0.62f, 0.34f, 0.22f);
    public static readonly Color PlacementSelectedColor = new("#f2c94ccc");
}
