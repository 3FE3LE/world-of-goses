#nullable enable
using System;

namespace WorldofGoses.Ui;

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

    /// <summary>The chunk window, or <c>null</c> before the first
    /// travel snapshot has seeded it.</summary>
    public ExpeditionPathChunkPool? Chunks => _chunks;

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
        WorldOffsetUnits = (long)Math.Round(positionX);
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
        WorldOffsetUnits = (long)Math.Round((arenaMinimumX + arenaMaximumX) / 2d);
        _chunks ??= new ExpeditionPathChunkPool(seed);
        _chunks.SetWorldOffset(WorldOffsetUnits);
    }
}
