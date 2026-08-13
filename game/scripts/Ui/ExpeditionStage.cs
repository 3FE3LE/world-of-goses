#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Prototypes;

namespace WorldofGoses.Ui;

/// <summary>
/// Depth-band projection for the expedition. Maps authoritative
/// one-dimensional session positions onto the authored HUD bounds and
/// interpolates views without writing anything back into combat.
///
/// <para>
/// The stage (#21) lives on the same depth-band grammar as the macro
/// street view: rear/sky bands, foreground bands, the playable band.
/// It consumes <see cref="StreetDepthProjection"/> and
/// <see cref="SharedDepthBands"/> for the trapezoid rasterizer so
/// the expedition and the macro never diverge on geometry. The
/// authority remains 1D — <see cref="Travel.PositionX"/> in
/// <see cref="ConfigureTravel"/> and combat
/// <c>CombatParticipantState.PositionX</c> in <see cref="Configure"/>
/// drive the screen position read-only.
/// </para>
/// </summary>
public partial class ExpeditionStage : Control
{
    private const int HorizontalPadding = 44;
    private const int CombatantWidth = 64;
    private const int CombatantHeight = 96;
    private const int GroundRatioPercent = 68;
    private const string CombatantScenePath = "res://scenes/Components/CombatantView.tscn";
    private const string TerrainAtlasPath =
        "res://assets/terrain/kenney/roguelike-rpg/roguelike_sheet_transparent.png";
    private const int EarthFillTileId = 786; // seam-free fill from the shared atlas
    private const int PathTileId = 538; // worn-footprint tile reused from the same atlas

    private readonly Dictionary<string, CombatantView> _views = new();
    private PackedScene _combatantScene = null!;
    private int _lastPresentedStep = -1;
    private double _domainMinimumX;
    private double _domainMaximumX = 1000;
    private bool _objectiveVisible;
    private bool _objectiveReached;
    private double _objectivePositionX;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        ClipContents = true;
        _combatantScene = ResourceLoader.Load<PackedScene>(CombatantScenePath);
        EnsureTerrainAtlas();
        QueueRedraw();
    }

    public void Configure(
        IReadOnlyList<CombatParticipantState> party,
        IReadOnlyList<CombatParticipantState> enemies,
        IReadOnlyList<CombatLogEntry> log,
        int step,
        double domainMinimumX,
        double domainMaximumX)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(log);
        if (domainMaximumX <= domainMinimumX)
            throw new ArgumentOutOfRangeException(nameof(domainMaximumX));
        _domainMinimumX = domainMinimumX;
        _domainMaximumX = domainMaximumX;
        // Encounter: keep the same chunk pool (#24) so the path
        // does not reset to a fresh window when combat starts.
        // The world scroll pauses because Travel.PositionX is no
        // longer advancing while combat controls the timeline.
        _objectiveVisible = false;
        _objectiveReached = false;

        var activeIds = new HashSet<string>(StringComparer.Ordinal);
        ApplyParticipants(party, CombatSide.Party, activeIds, log, step);
        ApplyParticipants(enemies, CombatSide.Enemy, activeIds, log, step);
        RemoveMissing(activeIds);
        _lastPresentedStep = Math.Max(_lastPresentedStep, step);
        QueueRedraw();
    }

    public void ConfigureTravel(
        ExpeditionLiveSnapshot.Travel travel,
        string displayName,
        double? healthRatio,
        int worldTick)
    {
        // Travel.PositionX is the authoritative 1D world offset for
        // the expedition. The infinite-path recycler (#22) drives
        // off this value; outbound advances it forward, the
        // return leg pulls it back. The stage never invents a
        // parallel offset of its own.
        _worldOffsetUnits = (long)System.Math.Round(travel.PositionX);
        _chunkPool ??= new ExpeditionPathChunkPool(seed: worldTick);
        _chunkPool.SetWorldOffset(_worldOffsetUnits);
        double maximumHealth = 100;
        double currentHealth = maximumHealth * Math.Clamp(healthRatio ?? 1, 0, 1);
        var founder = new CombatParticipantState(
            "travel.founder",
            null,
            displayName,
            currentHealth,
            maximumHealth,
            false,
            travel.PositionX,
            0,
            12,
            travel.Facing,
            travel.Activity,
            0,
            CombatStature.Standard);
        Configure(
            [founder],
            Array.Empty<CombatParticipantState>(),
            Array.Empty<CombatLogEntry>(),
            worldTick,
            travel.BattlefieldMinimumX,
            travel.BattlefieldMaximumX);
        _objectiveVisible = travel.ObjectiveVisible;
        _objectiveReached = travel.ObjectiveReached;
        _objectivePositionX = travel.ObjectivePositionX;
        QueueRedraw();
    }

    internal void ShowEarlyFixture()
    {
        if (!WorldofGoses.Testing.VisualRegressionHarness.IsActive) return;
        var party = new[]
        {
            FixtureParticipant("fixture.founder", "Founder", 180, CombatFacing.Right),
        };
        var enemies = new[]
        {
            FixtureParticipant("fixture.enemy0", "Enemy", 760, CombatFacing.Left),
            FixtureParticipant("fixture.enemy1", "Enemy", 840, CombatFacing.Left),
        };
        Configure(party, enemies, Array.Empty<CombatLogEntry>(), 0, 0, 1000);
    }

    /// <summary>Test seam: returns the chunk pool driving the world
    /// scroll, or <c>null</c> when the stage has not yet received a
    /// Travel snapshot. Lives behind a read-only getter so the test
    /// assembly can verify the recycler state without leaking the
    /// pool across the public surface.</summary>
    internal ExpeditionPathChunkPool? ChunkPool => _chunkPool;

    /// <summary>The most recent world offset driven by the
    /// authoritative Travel.PositionX. Returns 0 before the first
    /// ConfigureTravel call.</summary>
    internal long WorldOffsetUnits => _worldOffsetUnits;

    public void ClearCombatants()
    {
        foreach (CombatantView view in _views.Values) view.QueueFree();
        _views.Clear();
        _lastPresentedStep = -1;
        _objectiveVisible = false;
        _objectiveReached = false;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2I logicalSize = new(Mathf.RoundToInt(Size.X), Mathf.RoundToInt(Size.Y));
        if (logicalSize.X <= 0 || logicalSize.Y <= 0) return;

        Color sky = GetThemeColor("fill_empty");
        Color distance = GetThemeColor("fill_cooldown");
        Color ground = GetThemeColor("border_disabled");
        Color outline = GetThemeColor("border_locked");
        // Sky: solid colour is still the right call (no perspective on
        // the void); everything from the rear band down uses the
        // shared depth-band rasterizer.
        DrawRect(new Rect2I(0, 0, logicalSize.X, logicalSize.Y), sky);

        // Bands grow as depth increases; row 0 sits in the rear and
        // the playable band is the last one (ExpeditionPathRenderer.RowCount-1).
        if (_terrainAtlas is not null)
        {
            for (int depth = 0; depth < ExpeditionPathRenderer.RowCount; depth++)
            {
                float depthNear = depth;
                float depthFar = depth + 1f;
                float yNear = ExpeditionPathRenderer.RowScreenY(depthNear);
                float yFar = ExpeditionPathRenderer.RowScreenY(depthFar);
                float scaleNear = StreetDepthProjection.HorizontalScale(depthNear);
                float scaleFar = StreetDepthProjection.HorizontalScale(depthFar);
                float halfWidth = ExpeditionPathRenderer.HalfWidthPx;
                float center = ExpeditionPathRenderer.CenterX;
                float leftNear = center - halfWidth * scaleNear;
                float rightNear = center + halfWidth * scaleNear;
                float leftFar = center - halfWidth * scaleFar;
                float rightFar = center + halfWidth * scaleFar;
                int tileId = depth == ExpeditionPathRenderer.RowCount - 1
                    ? PathTileId
                    : EarthFillTileId;
                SharedDepthBands.DrawStaircaseTrapezoid(
                    this,
                    yNear, yFar,
                    leftNear, rightNear,
                    leftFar, rightFar,
                    _terrainAtlas,
                    TerrainAtlas.RegionOfId(tileId),
                    ExpeditionPathRenderer.PixelStep);
            }
        }

        // Outline on the horizon line keeps the staged silhouette the
        // previous projection provided, drawn as a 2 px line at the
        // playable band's near edge so it never crosses the bands.
        float playableY = ExpeditionPathRenderer.RowScreenY(
            ExpeditionPathRenderer.PlayableDepth);
        DrawLine(
            new Vector2I(0, Mathf.RoundToInt(playableY)),
            new Vector2I(logicalSize.X, Mathf.RoundToInt(playableY)),
            outline,
            width: 2,
            antialiased: false);
        // Legacy silhouette is preserved where bands do not yet cover
        // the upper region (depth 0 still gives a 580-ish row, the
        // silhouette was painted above the ground line so it can stay
        // as a small overlap with the empty sky band).
        if (_terrainAtlas is null)
        {
            int horizon = logicalSize.Y * GroundRatioPercent / 100;
            DrawLandscapeSilhouette(logicalSize, horizon, outline);
        }
        if (_objectiveVisible) DrawSpiritTrailManifestation(Mathf.RoundToInt(playableY));
    }

    private Texture2D? _terrainAtlas;
    private ExpeditionPathChunkPool? _chunkPool;
    private long _worldOffsetUnits;

    /// <summary>Custom <c>_Ready</c> extension: load the shared terrain
    /// atlas once so the new depth-band rendering does not instantiate
    /// it per <c>_Draw</c>. Visual-regression guard keeps the existing
    /// legacy render path when no atlas is available so tests built
    /// before the migration can still cover the silhouette.</summary>
    private void EnsureTerrainAtlas() =>
        _terrainAtlas ??= ResourceLoader.Load<Texture2D>(TerrainAtlasPath);

    private void ApplyParticipants(
        IReadOnlyList<CombatParticipantState> participants,
        CombatSide side,
        HashSet<string> activeIds,
        IReadOnlyList<CombatLogEntry> log,
        int step)
    {
        for (int index = 0; index < participants.Count; index++)
        {
            CombatParticipantState participant = participants[index];
            activeIds.Add(participant.Id);
            if (!_views.TryGetValue(participant.Id, out CombatantView? view))
            {
                view = _combatantScene.Instantiate<CombatantView>();
                view.Name = SafeNodeName(participant.Id);
                AddChild(view);
                _views.Add(participant.Id, view);
            }

            int baselineOffset = ((index % 3) - 1) * 4;
            // Project the authoritative one-dimensional PositionX onto
            // the playable band of the depth-band stage. The Y is the
            // stage's playable row — never a domain Y, never a combat
            // position — and the X is the read-only projection of the
            // domain coordinate. The stage is still 1D; the rendering
            // is what gives the playable band depth.
            float stageX = ExpeditionPathRenderer.ProjectDomainXToStageX(
                _domainMinimumX, _domainMaximumX, participant.PositionX);
            float playableY = ExpeditionPathRenderer.RowScreenY(
                ExpeditionPathRenderer.PlayableDepth);
            Vector2I target = new(
                Mathf.RoundToInt(stageX) - CombatantWidth / 2,
                Mathf.RoundToInt(playableY) - CombatantHeight + baselineOffset);
            bool animate = _lastPresentedStep >= 0 && step > _lastPresentedStep;
            view.ApplySnapshot(
                participant,
                side,
                index,
                target,
                animate,
                step > _lastPresentedStep ? EventsFor(participant.Id, log, step) : Array.Empty<CombatLogEntry>());
        }
    }

    private int ProjectPosition(double domainX)
    {
        double ratio = Math.Clamp(
            (domainX - _domainMinimumX) / (_domainMaximumX - _domainMinimumX),
            0,
            1);
        int width = Math.Max(1, Mathf.RoundToInt(Size.X) - HorizontalPadding * 2);
        return HorizontalPadding + Mathf.RoundToInt((float)(ratio * width));
    }

    private int GroundY() => Mathf.RoundToInt(Size.Y) * GroundRatioPercent / 100;

    private static IReadOnlyList<CombatLogEntry> EventsFor(
        string participantId,
        IReadOnlyList<CombatLogEntry> log,
        int step)
    {
        var events = new List<CombatLogEntry>();
        foreach (CombatLogEntry entry in log)
        {
            if (entry.Step == step
                && (entry.ActorId == participantId || entry.TargetId == participantId))
            {
                events.Add(entry);
            }
        }
        return events;
    }

    private void RemoveMissing(HashSet<string> activeIds)
    {
        var missing = new List<string>();
        foreach ((string id, CombatantView view) in _views)
        {
            if (activeIds.Contains(id)) continue;
            view.QueueFree();
            missing.Add(id);
        }
        foreach (string id in missing) _views.Remove(id);
    }

    private void DrawLandscapeSilhouette(Vector2I logicalSize, int horizon, Color color)
    {
        for (int x = 20; x < logicalSize.X; x += 112)
        {
            int height = 10 + (x / 112 % 3) * 6;
            DrawRect(new Rect2I(x, horizon - height, 32, height), color.Darkened(0.25f));
            DrawRect(new Rect2I(x + 8, horizon - height - 6, 16, 6), color.Darkened(0.25f));
        }
        DrawLine(
            new Vector2I(0, horizon),
            new Vector2I(logicalSize.X, horizon),
            color,
            width: 2,
            antialiased: false);
    }

    private void DrawSpiritTrailManifestation(int horizon)
    {
        float stageX = ExpeditionPathRenderer.ProjectDomainXToStageX(
            _domainMinimumX, _domainMaximumX, _objectivePositionX);
        int centerX = Mathf.RoundToInt(stageX);
        int centerY = horizon - 54;
        Color border = GetThemeColor(_objectiveReached ? "border_ready" : "border_locked");
        var octagon = new Vector2[]
        {
            new(centerX - 12, centerY - 20),
            new(centerX + 12, centerY - 20),
            new(centerX + 20, centerY - 12),
            new(centerX + 20, centerY + 12),
            new(centerX + 12, centerY + 20),
            new(centerX - 12, centerY + 20),
            new(centerX - 20, centerY + 12),
            new(centerX - 20, centerY - 12),
        };
        DrawPolyline(
            [.. octagon, octagon[0]],
            border,
            width: 2,
            antialiased: false);
        DrawLine(
            new Vector2I(centerX - 8, centerY + 7),
            new Vector2I(centerX + 8, centerY - 7),
            border,
            width: 2,
            antialiased: false);
        DrawLine(
            new Vector2I(centerX - 4, centerY - 8),
            new Vector2I(centerX + 7, centerY + 3),
            border,
            width: 2,
            antialiased: false);
    }

    private static CombatParticipantState FixtureParticipant(
        string id,
        string name,
        double positionX,
        CombatFacing facing) => new(
            id, null, name, 100, 100, false, positionX, 48, 12, facing,
            CombatSpatialActivity.Idle, 0, CombatStature.Standard);

    private static string SafeNodeName(string id) => id
        .Replace(".", "_", StringComparison.Ordinal)
        .Replace(":", "_", StringComparison.Ordinal);
}
