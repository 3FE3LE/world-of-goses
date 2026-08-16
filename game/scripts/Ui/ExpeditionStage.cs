#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
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
    /// <summary>
    /// Defensive cap on tiles per band, so a degenerate parallax factor cannot
    /// turn one frame into an unbounded loop.
    /// </summary>
    private const int MaxTilesPerBand = 160;

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
        Resized += OnResized;
        QueueRedraw();
    }

    public override void _ExitTree() => Resized -= OnResized;

    /// <summary>
    /// The ground the path is made of — the same profile the macro city floor
    /// draws from, so the two are literally the same terrain.
    /// </summary>
    /// <remarks>
    /// The stage used to hold its own <c>res://</c> path to the Kenney sheet
    /// and two hard-coded ids, 538 for cover and 786 for the road. That was a
    /// second, biome-blind terrain implementation: the macro could paint an
    /// authored Eirune meadow while the expedition painted Kenney green for
    /// every lineage, forever. Null leaves the ground unpainted rather than
    /// guessing a sheet.
    /// </remarks>
    public GroundAtlasProfile? GroundProfile { get; set; }

    /// <summary>The visual identity of one citizen, as the stage needs it.</summary>
    public sealed record CombatantAppearance(
        LineageId Lineage, GenderId Gender, AppearanceVariantId Appearance);

    /// <summary>
    /// Resolves a party member's citizen id to the sprite it should wear.
    /// Null, or a null result, leaves that combatant on the drawn placeholder.
    /// </summary>
    /// <remarks>
    /// A delegate rather than a world reference: the stage projects combat
    /// state and must not acquire a way to read the city. The owner —
    /// <c>ExpeditionLiveView</c>, which already holds the controller — supplies
    /// the lookup.
    /// </remarks>
    public Func<CitizenId, CombatantAppearance?>? AppearanceResolver { get; set; }

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
        bool isNewStep = step > _lastPresentedStep;
        ApplyParticipants(party, CombatSide.Party, activeIds, log, step);
        ApplyParticipants(enemies, CombatSide.Enemy, activeIds, log, step);
        RemoveMissing(activeIds);
        // A second pass, because a shove needs both ends of the blow and the
        // first pass runs once per side: while the party is being placed the
        // enemies have no screen position yet.
        if (isNewStep) ApplyHitReactions(party, enemies, log, step);
        _lastPresentedStep = Math.Max(_lastPresentedStep, step);
        QueueRedraw();
    }

    public void ConfigureTravel(
        ExpeditionLiveSnapshot.Travel travel,
        string displayName,
        double? healthRatio,
        int worldTick,
        CitizenId? travellerId = null)
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
        // Where the walker is drawn is the camera's decision, not the stage's —
        // see TravelDrawPositionX. The stage cannot be instantiated in a test,
        // so anything it decides for itself is a decision no assertion reaches.
        double drawnPositionX = _camera.TravelDrawPositionX;
        double maximumHealth = 100;
        double currentHealth = maximumHealth * Math.Clamp(healthRatio ?? 1, 0, 1);
        // The traveller's citizen id, so the party wears its own character
        // sprite while walking and not only once a fight starts. This was null,
        // which is what made the drawn placeholder reappear the moment the
        // encounter ended: AppearanceResolver has nothing to resolve without it.
        var founder = new CombatParticipantState(
            "travel.founder",
            travellerId,
            displayName,
            currentHealth,
            maximumHealth,
            false,
            drawnPositionX,
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
        if (GroundProfile is { Fill.Length: > 0 } profile && profile.Atlas is { } groundAtlas)
        {
            for (int i = bands.Count - 1; i >= 0; i--)
            {
                DrawGroundBand(bands[i], profile, groundAtlas, _camera.WorldOffsetUnits, anchor);
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
        // Fallback silhouette for when no biome profile has been supplied —
        // a fixture that configures the stage without a city behind it, for
        // instance. It draws a horizon rather than leaving the frame empty.
        if (GroundProfile?.Atlas is null)
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

    /// <summary>
    /// Paints one ground row as a run of individual tiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This used to be a single trapezoid per band with one tile region
    /// stretched across it — one 32 px tile smeared over the full width of the
    /// stage, which is why the path read as flat plastic next to a macro floor
    /// made of actual tiles. The macro has always drawn a trapezoid per tile;
    /// this now does the same, from the same profile, with the same spatial
    /// hash choosing the variant, so the two surfaces are the same terrain and
    /// not merely the same colour.
    /// </para>
    /// <para>
    /// Tiles are indexed in world space, so a tile keeps its variant as the
    /// path scrolls instead of shimmering. The row index is the band's depth,
    /// which makes the pattern continuous across the parcel's nine rows.
    /// </para>
    /// </remarks>
    private void DrawGroundBand(
        ExpeditionPathBand band,
        GroundAtlasProfile profile,
        Texture2D atlas,
        long worldOffsetUnits,
        in ExpeditionPathAnchor anchor)
    {
        // Perspective at both edges, never the authored parallax factors.
        // ParallaxFactorForDepth hands the fringe at depth -1 the authored
        // ForegroundFactor of 1.40 while its far edge at depth 0 gets the
        // perspective 1.0, so every tile in that row was sheared by the
        // difference — the badly stretched extra row. A ground row is a
        // rectangle in the world and both of its edges belong to the same
        // projection; only props, which own no world coordinate, take an
        // authored factor.
        float factorNear = StreetDepthProjection.HorizontalScale(band.Depth);
        float factorFar = StreetDepthProjection.HorizontalScale(band.Depth + 1f);
        if (factorNear <= 0f || factorFar <= 0f) return;

        // Whole parcels, snapped to parcel boundaries, centred on the one the
        // party is on — so the composition is always a parcel in focus with
        // complete neighbours either side and the seams land where the grid
        // says, never mid-tile.
        //
        // The count is the chunk pool's own window rather than a number picked
        // here, and it has to be that wide. Three parcels is 864 units, which
        // covers the playable band at 864 px but only reaches 458 px on the
        // furthest row, where the perspective factor is 0.53 — 340 px short of
        // the stage. The terrain then reads as a floating trapezoid with
        // diagonal edges instead of a plane. Seven parcels clears the widest
        // row, and the dressing already windows the same seven.
        const float tileUnits = MacroViewConstants.TileUnitPx;
        const float parcelUnits = ExpeditionPathChunkPool.ChunkWidthUnits;
        const int tilesPerParcel = (int)(parcelUnits / tileUnits);
        const int parcelsDrawn = ExpeditionPathChunkPool.ChunkCount;
        float centerX = anchor.CenterX;

        long focusParcel = (long)Math.Floor(worldOffsetUnits / parcelUnits);
        int firstTile = (int)((focusParcel - (parcelsDrawn / 2)) * tilesPerParcel);
        int lastTile = firstTile + (parcelsDrawn * tilesPerParcel) - 1;
        if (lastTile - firstTile > MaxTilesPerBand) lastTile = firstTile + MaxTilesPerBand;

        int row = Mathf.RoundToInt(band.Depth);
        for (int tile = firstTile; tile <= lastTile; tile++)
        {
            double nearWorld = tile * (double)tileUnits;
            double farWorld = nearWorld + tileUnits;

            // The playable band is the calle, so it wears the path tile; every
            // other row is the biome's material. IsPlayable is asked, never
            // re-derived from a row index (#27).
            int tileId = band.IsPlayable
                ? profile.Path
                : profile.Fill[TerrainAtlas.GroundVariantIndex(tile, row, profile.Fill.Length)];

            SharedDepthBands.DrawStaircaseTrapezoid(
                this,
                band.ScreenYNear, band.ScreenYFar,
                ScreenX(nearWorld, factorNear), ScreenX(farWorld, factorNear),
                ScreenX(nearWorld, factorFar), ScreenX(farWorld, factorFar),
                atlas,
                profile.RegionOfId(tileId),
                ExpeditionPathRenderer.PixelStep);
        }

        float ScreenX(double worldX, float factor) =>
            centerX + (float)((worldX - worldOffsetUnits) * factor);
    }

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

    private readonly ExpeditionPathCamera _camera = new();

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

                // Party members are citizens and already have a character
                // sprite; enemies carry no CitizenId and no art, so they keep
                // the drawn placeholder. The stage resolves nothing itself —
                // it has no world access and should not gain any — so the
                // owner supplies the lookup.
                if (participant.CitizenId is { } citizenId
                    && AppearanceResolver?.Invoke(citizenId) is { } appearance)
                {
                    view.UseCharacterSprite(
                        appearance.Lineage, appearance.Gender, appearance.Appearance);
                }
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

    /// <summary>
    /// Shoves everyone who was hit this step.
    /// </summary>
    /// <remarks>
    /// Only Knockdown moves anyone in the domain, which is correct and which
    /// also left a spear thrust looking like it landed on a statue. This is the
    /// flinch: transient, presentational, and it decays back to the
    /// authoritative position, so an encounter watched and an encounter resolved
    /// offline end with everyone in the same place.
    /// <para>
    /// The magnitudes come from <see cref="HitReaction"/> rather than from here,
    /// because this class is a <c>Control</c> and cannot be run in a test.
    /// </para>
    /// </remarks>
    private void ApplyHitReactions(
        IReadOnlyList<CombatParticipantState> party,
        IReadOnlyList<CombatParticipantState> enemies,
        IReadOnlyList<CombatLogEntry> log,
        int step)
    {
        var strikers = new Dictionary<string, Striker>(StringComparer.Ordinal);
        foreach (PlacedParticipant placed in _placed)
        {
            CombatParticipantState? participant = Find(placed.Id, party, enemies);
            if (participant is null) continue;
            strikers[placed.Id] = new Striker(
                participant.Impulse,
                ScreenXOf(placed));
        }

        foreach (PlacedParticipant placed in _placed)
        {
            CombatParticipantState? participant = Find(placed.Id, party, enemies);
            if (participant is null) continue;
            if (!_views.TryGetValue(placed.Id, out CombatantView? view)) continue;

            view.ReactToHit(HitReaction.ForEvents(
                placed.Id,
                participant.Stability,
                ScreenXOf(placed),
                EventsFor(placed.Id, log, step),
                strikers));
        }
    }

    private float ScreenXOf(PlacedParticipant placed) =>
        PlacementFor(placed.WorldX, ((placed.Index % 3) - 1) * 4).X;

    private static CombatParticipantState? Find(
        string id,
        IReadOnlyList<CombatParticipantState> party,
        IReadOnlyList<CombatParticipantState> enemies)
    {
        foreach (CombatParticipantState participant in party)
        {
            if (participant.Id == id) return participant;
        }
        foreach (CombatParticipantState participant in enemies)
        {
            if (participant.Id == id) return participant;
        }
        return null;
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
