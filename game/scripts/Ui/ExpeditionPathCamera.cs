#nullable enable
using System;

namespace WorldofGoses.Ui;

/// <summary>
/// How the expedition world is allowed to move.
/// </summary>
/// <remarks>
/// The game's motion grammar is discrete: characters and the camera advance in
/// whole <see cref="PixelMotion.StepPixels"/> steps rather than sliding. Combat
/// is the one exception, and a deliberate one — impact reactions and camera pans
/// are readable only against continuous motion, and a fight is where the game
/// stops being a walk and asks to be watched.
/// </remarks>
public enum ExpeditionMotionMode
{
    /// <summary>Travelling. The world advances one locomotion step at a time.</summary>
    Quantized,

    /// <summary>
    /// Fighting. The offset moves freely so displacement and framing can be
    /// interpolated.
    /// </summary>
    Continuous,
}

/// <summary>
/// Owns the one number that decides where the expedition world is
/// drawn: the world offset, and the chunk window centred on it.
///
/// <para>
/// It exists so the sequence Travel → Encounter → outcome → Return can
/// be exercised as itself. The stage that draws it is a Godot
/// <c>Control</c> and cannot be instantiated outside the engine, so
/// every test of that sequence used to build its own pool and re-enact
/// the calls it believed the stage made — which is how the stage came
/// to make none of them while the suite stayed green. Moving the
/// decisions here leaves the stage with drawing, and leaves the
/// decisions somewhere they can be run.
/// </para>
///
/// <para>
/// Presentation only. The offset is derived from
/// <c>Travel.PositionX</c> and the combat arena bounds; nothing here is
/// persisted, and nothing here is an authority for where anything
/// <em>is</em> — only for where the camera is looking.
/// </para>
/// </summary>
public sealed class ExpeditionPathCamera
{
    private ExpeditionPathChunkPool? _chunks;

    /// <summary>Where the camera is looking, in world units.</summary>
    public long WorldOffsetUnits { get; private set; }

    /// <summary>
    /// Where the camera is looking, unrounded.
    /// </summary>
    /// <remarks>
    /// While <see cref="ExpeditionMotionMode.Quantized"/> this equals
    /// <see cref="WorldOffsetUnits"/> — the whole point of the mode is that
    /// there is no fractional position to keep. While
    /// <see cref="ExpeditionMotionMode.Continuous"/> it keeps the sub-pixel
    /// remainder the integer offset discards, which is what a pan or an impact
    /// reaction needs in order not to stutter.
    /// </remarks>
    public double WorldOffset { get; private set; }

    /// <summary>Whether the world is currently stepping or sliding.</summary>
    public ExpeditionMotionMode MotionMode { get; private set; } =
        ExpeditionMotionMode.Quantized;

    /// <summary>
    /// The world position a travelling party is to be drawn at.
    /// </summary>
    /// <remarks>
    /// The camera's own offset, so the projection of the two resolves to exactly
    /// the anchor centre and the walker holds it while the ground steps past.
    /// <para>
    /// This is a property rather than something the stage works out because the
    /// stage is a Godot <c>Control</c> and cannot be run in a test. Drawing the
    /// party at the raw <c>Travel.PositionX</c> against a stepped offset is the
    /// mistake available here — the ground jumps and the walker slides across
    /// it, which is the grammar inverted — and a decision left inside the stage
    /// is one no assertion can reach.
    /// </para>
    /// </remarks>
    public double TravelDrawPositionX => WorldOffset;

    /// <summary>The chunk window, or <c>null</c> before the first
    /// travel snapshot has seeded it.</summary>
    public ExpeditionPathChunkPool? Chunks => _chunks;

    /// <summary>
    /// The locomotion step the travelling world advances by.
    /// </summary>
    /// <remarks>
    /// Mirrors <see cref="PixelMotion.StepPixels"/>, which is a compile-time
    /// constant and so costs this file no engine dependency — it stays
    /// instantiable outside Godot, which is why the travel/encounter sequence
    /// can be tested at all.
    /// </remarks>
    public const double StepUnits = PixelMotion.StepPixels;

    /// <summary>
    /// Follows the party. The offset is the party's own position, so
    /// what the projection draws is the difference between them — zero
    /// — and the traveller holds the centre while the world goes past.
    /// </summary>
    /// <param name="positionX">The authoritative <c>Travel.PositionX</c>.</param>
    /// <param name="seed">Seeds the chunk window on first use only.
    /// Re-seeding per tick would re-dress the same stretch of world
    /// every frame.</param>
    public void FollowTravel(double positionX, int seed)
    {
        // Walking is quantized. The offset moves the world past a traveller who
        // holds the centre, so snapping it to whole locomotion steps is what
        // makes the walk read as a gait instead of a slide — and rounding to
        // the nearest world unit, as this did, is not that: at 96 px/s a
        // one-pixel grid is continuous motion with extra steps.
        MotionMode = ExpeditionMotionMode.Quantized;
        WorldOffset = Math.Round(positionX / StepUnits) * StepUnits;
        WorldOffsetUnits = (long)Math.Round(WorldOffset);
        _chunks ??= new ExpeditionPathChunkPool(seed);
        _chunks.SetWorldOffset(WorldOffsetUnits);
    }

    /// <summary>
    /// Frames an encounter on the middle of its arena.
    ///
    /// <para>The chunk window is not rebuilt: the same chunks keep
    /// their logical indices and therefore their dressing, so the fight
    /// happens on the stretch of path the party walked in on. Only the
    /// camera moves, and it moves because travel leaves the offset at
    /// the party's own position, which would put the enemies waiting at
    /// the far end of the arena past the right edge of the stage.</para>
    /// </summary>
    /// <param name="seed">Seeds the chunk window if travel has not
    /// already done so. A fight can be the first thing the stage is
    /// asked to draw — a fixture, a reload mid-encounter — and the
    /// ground it happens on exists either way.</param>
    public void FrameEncounter(double arenaMinimumX, double arenaMaximumX, int seed = 0)
    {
        if (arenaMaximumX <= arenaMinimumX)
        {
            throw new ArgumentOutOfRangeException(nameof(arenaMaximumX));
        }
        // A fight is the one place the game stops stepping. Impact reactions and
        // camera pans are only readable against continuous motion, so the offset
        // keeps its fractional part here and the step grid is dropped until
        // FollowTravel resumes — which is what ends the fight, so the walk goes
        // back to being a walk with no extra call to forget.
        MotionMode = ExpeditionMotionMode.Continuous;
        WorldOffset = (arenaMinimumX + arenaMaximumX) / 2d;
        WorldOffsetUnits = (long)Math.Round(WorldOffset);
        _chunks ??= new ExpeditionPathChunkPool(seed);
        _chunks.SetWorldOffset(WorldOffsetUnits);
    }
}
