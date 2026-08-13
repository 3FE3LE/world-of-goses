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
    private const int CombatantWidth = 64;
    private const int CombatantHeight = 96;
    private const int GroundRatioPercent = 68;
    private const string CombatantScenePath = "res://scenes/Components/CombatantView.tscn";
    private const string TerrainAtlasPath =
        "res://assets/terrain/kenney/roguelike-rpg/roguelike_sheet_transparent.png";
    // Which of these is the road decides how the whole stage reads.
    // While the playable band was the row nearest the horizon (#27),
    // 538's green covered it and the trodden earth covered everything
    // else — a strip of grass at the skyline with the party walking on
    // bare ground. The names now say what each one draws.
    private const int TroddenPathTileId = 786; // worn earth, the road itself
    private const int GroundCoverTileId = 538; // green cover either side of it

    private readonly Dictionary<string, CombatantView> _views = new();
    private readonly List<PlacedParticipant> _placed = new();
    private PackedScene _combatantScene = null!;
    private int _lastPresentedStep = -1;
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
        Resized += OnResized;
        QueueRedraw();
    }

    public override void _ExitTree() => Resized -= OnResized;

    public void Configure(
        IReadOnlyList<CombatParticipantState> party,
        IReadOnlyList<CombatParticipantState> enemies,
        IReadOnlyList<CombatLogEntry> log,
        int step,
        double domainMinimumX,
        double domainMaximumX)
    {
        // An encounter frames itself. The chunk window and the world
        // grid are untouched — the same chunks keep their logical
        // indices and their dressing, so the fight happens on the
        // stretch of path the party walked in on (#24) — but the
        // camera settles on the middle of the arena so both sides are
        // on screen. Travel left the offset at the party's own
        // position, which would have put the enemies past the right
        // edge. Framing is a camera decision; it is not a second
        // source of truth for where anything *is*.
        _camera.FrameEncounter(domainMinimumX, domainMaximumX, seed: step);
        ConfigureCore(party, enemies, log, step, domainMinimumX, domainMaximumX);
    }

    private void ConfigureCore(
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
        _objectiveVisible = false;
        _objectiveReached = false;

        var activeIds = new HashSet<string>(StringComparer.Ordinal);
        _placed.Clear();
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
        //
        // The offset moves the world, not the party. Everything with a
        // world coordinate — terrain, chunk seams, dressing, the
        // objective — is projected through it, so advancing it slides
        // all of them leftward past a founder who stays at the anchor's
        // centre. Before #23 was reconnected this value fed a pool
        // nothing drew, while the founder was projected straight onto
        // screen X: the party crossed a world that never moved.
        _camera.FollowTravel(travel.PositionX, seed: worldTick);
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
        ConfigureCore(
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
    internal ExpeditionPathChunkPool? ChunkPool => _camera.Chunks;

    /// <summary>The most recent world offset driven by the
    /// authoritative Travel.PositionX. Returns 0 before the first
    /// ConfigureTravel call.</summary>
    internal long WorldOffsetUnits => _camera.WorldOffsetUnits;

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
        ExpeditionPathAnchor anchor = PathAnchor;
        // Sky: solid colour is still the right call (no perspective on
        // the void); everything from the rear band down uses the
        // shared depth-band rasterizer.
        DrawRect(new Rect2I(0, 0, logicalSize.X, logicalSize.Y), sky);

        // Farthest plane first. The blocks belong to world positions,
        // so they crawl leftward as the party advances rather than
        // sitting still behind a moving foreground.
        foreach (ExpeditionPathProp block in ExpeditionPathComposition.DistanceBlocks(
            _camera.WorldOffsetUnits, logicalSize.X, anchor))
        {
            DrawProp(block, distance.Darkened(0.15f + block.BiomeId * 0.08f));
        }

        // Ground rows, far to near, so nearer bands overdraw the seam
        // of the one behind them. Which row gets the worn-path tile is
        // asked, not inferred: IsPlayable is the same authority the
        // combatants and the objective resolve their Y through (#27).
        IReadOnlyList<ExpeditionPathBand> bands = ExpeditionPathComposition.Bands(anchor);
        if (_terrainAtlas is not null)
        {
            for (int i = bands.Count - 1; i >= 0; i--)
            {
                ExpeditionPathBand band = bands[i];
                SharedDepthBands.DrawStaircaseTrapezoid(
                    this,
                    band.ScreenYNear, band.ScreenYFar,
                    band.LeftNear, band.RightNear,
                    band.LeftFar, band.RightFar,
                    _terrainAtlas,
                    TerrainAtlas.RegionOfId(
                        band.IsPlayable ? TroddenPathTileId : GroundCoverTileId),
                    ExpeditionPathRenderer.PixelStep);
            }
        }

        float playableY = ExpeditionPathRenderer.PlayableScreenY(anchor);
        DrawRearDressing(ground, playableY);

        // Tread marks at the chunk boundaries. A road of identical
        // tiles slides under the party without a pixel appearing to
        // move; these are what make the scroll legible on the playable
        // band itself rather than only on the dressing beside it.
        if (_camera.Chunks is not null)
        {
            int treadTop = Mathf.RoundToInt(playableY) - 3;
            foreach (float seamX in ExpeditionPathComposition.ChunkSeams(
                _camera.Chunks.Chunks, _camera.WorldOffsetUnits, anchor))
            {
                if (seamX < 0 || seamX > logicalSize.X) continue;
                DrawRect(
                    new Rect2I(Mathf.RoundToInt(seamX) - 1, treadTop, 3, 7),
                    ground.Darkened(0.35f));
            }
        }

        // No horizon rule across the playable band any more. It was
        // drawn when the terrain ended there and the void began; now
        // the fringe continues the ground toward the camera, and a
        // full-width line across it only cut one continuous surface in
        // half. The band already reads as the road because it is the
        // one wearing the trodden tile.
        if (_terrainAtlas is null)
        {
            int horizon = logicalSize.Y * GroundRatioPercent / 100;
            DrawLandscapeSilhouette(logicalSize, horizon, outline);
        }
        if (_objectiveVisible) DrawSpiritTrailManifestation(Mathf.RoundToInt(playableY));
        DrawForegroundDressing(ground);
    }

    /// <summary>The anchor for the size this Control actually has.
    /// Falls back to the authored 1280x720 anchor before the first
    /// layout pass, when Size is still zero.</summary>
    private ExpeditionPathAnchor PathAnchor =>
        Size.X > 1f && Size.Y > 1f
            ? ExpeditionPathAnchor.For(Size)
            : ExpeditionPathAnchor.Default;

    private void DrawRearDressing(Color ground, float playableY)
    {
        if (_camera.Chunks is null) return;
        foreach (ExpeditionPathProp prop in ExpeditionPathComposition.Props(
            _camera.Chunks.Chunks, _camera.WorldOffsetUnits, PathAnchor))
        {
            if (prop.Layer != ExpeditionPathLayer.Rear) continue;
            if (prop.ScreenBaseY >= playableY) continue;
            DrawProp(prop, BiomeTint(ground, prop.BiomeId));
        }
    }

    private void DrawForegroundDressing(Color ground)
    {
        if (_camera.Chunks is null) return;
        foreach (ExpeditionPathProp prop in ExpeditionPathComposition.Props(
            _camera.Chunks.Chunks, _camera.WorldOffsetUnits, PathAnchor))
        {
            if (prop.Layer != ExpeditionPathLayer.Foreground) continue;
            // The fringe is drawn after the combatants, so it must not
            // be allowed to sit on top of them: it lives below the
            // playable band, where only the ground is.
            DrawProp(prop, BiomeTint(ground, prop.BiomeId).Darkened(0.45f));
        }
    }

    private void DrawProp(in ExpeditionPathProp prop, Color color)
    {
        int width = Mathf.Max(1, Mathf.RoundToInt(prop.WidthPx));
        int height = Mathf.Max(1, Mathf.RoundToInt(prop.HeightPx));
        int left = Mathf.RoundToInt(prop.ScreenX) - width / 2;
        int top = Mathf.RoundToInt(prop.ScreenBaseY) - height;
        DrawRect(new Rect2I(left, top, width, height), color);
        // A second, narrower block on top turns a bar into a silhouette
        // without paying for a sprite the dressing does not have yet.
        DrawRect(
            new Rect2I(left + width / 4, top - height / 3, Mathf.Max(1, width / 2), height / 3),
            color.Darkened(0.2f));
    }

    private static Color BiomeTint(Color ground, int biomeId) => biomeId switch
    {
        0 => ground.Darkened(0.1f),
        1 => ground.Lightened(0.12f),
        2 => ground.Darkened(0.3f),
        _ => ground.Lightened(0.28f),
    };

    private Texture2D? _terrainAtlas;
    private readonly ExpeditionPathCamera _camera = new();

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
            // the playable band of the depth-band stage, through the
            // same world-to-screen rule the terrain uses. The Y is the
            // stage's playable row — never a domain Y, never a combat
            // position — and the X is the read-only projection of the
            // domain coordinate. The stage is still 1D; the rendering
            // is what gives the playable band depth.
            //
            // During travel this is what keeps the party at a stable
            // focus: the offset tracks the founder's own PositionX, so
            // the difference between them is what is drawn, and it is
            // ~0. The founder holds the centre and the world goes past.
            _placed.Add(new PlacedParticipant(participant.Id, participant.PositionX, index));
            Vector2I target = PlacementFor(participant.PositionX, baselineOffset);
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

    private Vector2I PlacementFor(double worldX, int baselineOffset)
    {
        ExpeditionPathAnchor anchor = PathAnchor;
        float stageX = ExpeditionPathRenderer.PlayableScreenX(
            worldX, _camera.WorldOffsetUnits, anchor);
        float playableY = ExpeditionPathRenderer.PlayableScreenY(anchor);
        return new Vector2I(
            Mathf.RoundToInt(stageX) - CombatantWidth / 2,
            Mathf.RoundToInt(playableY) - CombatantHeight + baselineOffset);
    }

    /// <summary>
    /// Re-places the combatants when the Control changes size.
    /// The anchor is derived from that size, so without this the views
    /// would keep the coordinates of the previous layout while the
    /// terrain moved under them.
    /// </summary>
    private void OnResized()
    {
        foreach (PlacedParticipant placed in _placed)
        {
            if (!_views.TryGetValue(placed.Id, out CombatantView? view)) continue;
            view.Position = PlacementFor(placed.WorldX, ((placed.Index % 3) - 1) * 4);
        }
        QueueRedraw();
    }

    /// <summary>What the stage needs to put a combatant back where it
    /// belongs after a resize: who, where in the world, and which slot
    /// in its side's ordering (the slot decides the baseline jitter).</summary>
    private readonly record struct PlacedParticipant(string Id, double WorldX, int Index);

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
        // The objective is a place in the world, so it is projected the
        // way every other place is: through the world offset, on the
        // playable band. It therefore approaches as the party walks
        // toward it instead of hanging at a fixed fraction of the
        // stage.
        float stageX = ExpeditionPathRenderer.PlayableScreenX(
            _objectivePositionX, _camera.WorldOffsetUnits, PathAnchor);
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
