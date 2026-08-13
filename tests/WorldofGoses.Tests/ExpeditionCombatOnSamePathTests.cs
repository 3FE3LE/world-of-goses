using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// #24 contract: travel and combat happen on the same path.
///
/// <para>
/// These drive <see cref="ExpeditionPathCamera"/> — the object the
/// stage delegates its world offset to — through the real sequence
/// Travel → Encounter → outcome → Return, and then read the terrain
/// coordinates that sequence produces. The previous version of this
/// file built its own pool and asserted that a pool behaves like a
/// pool; it could not have failed when the renderer ignored the pool
/// entirely, which is exactly what it was doing.
/// </para>
/// </summary>
public class ExpeditionCombatOnSamePathTests
{
    private static readonly ExpeditionPathAnchor Anchor =
        ExpeditionPathAnchor.For(new Vector2(800f, 460f));

    // The route the domain actually walks: city at 100, encounter at
    // 360, objective at 850, arena spanning the same 0..1000 space.
    private const double CityX = 100;
    private const double EncounterX = 360;
    private const double ObjectiveX = 850;
    private const double ArenaMinimumX = 0;
    private const double ArenaMaximumX = 1000;

    [Fact]
    public void EnteringCombat_KeepsTheChunkWindowAndItsDressing()
    {
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(EncounterX, seed: 41);
        ExpeditionPathChunkPool pool = camera.Chunks!;
        Dictionary<long, int> dressingBefore = pool.Chunks
            .ToDictionary(c => c.LogicalIndex, c => c.BiomeId);

        camera.FrameEncounter(ArenaMinimumX, ArenaMaximumX);

        // Same instance: combat does not rebuild the world.
        Assert.Same(pool, camera.Chunks);
        Assert.Equal(ExpeditionPathChunkPool.ChunkCount, camera.Chunks!.Chunks.Count);
        // And the chunks the party walked in on still wear what they
        // wore, so the fight is on that stretch and not on a new one.
        foreach (ExpeditionPathChunk chunk in camera.Chunks.Chunks)
        {
            if (dressingBefore.TryGetValue(chunk.LogicalIndex, out int biome))
            {
                Assert.Equal(biome, chunk.BiomeId);
            }
        }
    }

    [Fact]
    public void CombatFramesBothSidesOfTheArena_OnTheSameGround()
    {
        // The reason the camera settles at all: travel leaves the
        // offset at the party's position, which puts an enemy waiting
        // at the far end of the arena off the right edge.
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(EncounterX, seed: 5);
        camera.FrameEncounter(ArenaMinimumX, ArenaMaximumX);

        float partyX = ExpeditionPathRenderer.PlayableScreenX(
            140, camera.WorldOffsetUnits, Anchor);
        float enemyX = ExpeditionPathRenderer.PlayableScreenX(
            850, camera.WorldOffsetUnits, Anchor);

        Assert.InRange(partyX, 0f, 800f);
        Assert.InRange(enemyX, 0f, 800f);
        Assert.True(partyX < enemyX, "The party approaches from the near side.");
    }

    [Fact]
    public void CombatantsAndTerrainShareOneBand()
    {
        // Whatever the camera is doing, the combatants stand on the
        // band the terrain marks as the path (#27).
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(EncounterX, seed: 5);
        camera.FrameEncounter(ArenaMinimumX, ArenaMaximumX);

        ExpeditionPathBand playable = ExpeditionPathComposition
            .Bands(Anchor)
            .Single(b => b.IsPlayable);
        Assert.Equal(
            ExpeditionPathRenderer.PlayableScreenY(Anchor),
            playable.ScreenYNear,
            precision: 3);
    }

    [Fact]
    public void DuringCombat_TheWorldStopsCommunicatingProgress()
    {
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(EncounterX, seed: 17);
        camera.FrameEncounter(ArenaMinimumX, ArenaMaximumX);
        IReadOnlyList<float> firstStep = ExpeditionPathComposition.ChunkSeams(
            camera.Chunks!.Chunks, camera.WorldOffsetUnits, Anchor);

        // Every combat step re-frames the same arena. The terrain must
        // not drift a pixel between steps, or combat would read as
        // travel.
        for (int step = 0; step < 12; step++)
        {
            camera.FrameEncounter(ArenaMinimumX, ArenaMaximumX);
            Assert.Equal(
                firstStep,
                ExpeditionPathComposition.ChunkSeams(
                    camera.Chunks!.Chunks, camera.WorldOffsetUnits, Anchor));
        }
    }

    [Fact]
    public void LeavingCombat_ResumesFromTheAuthoritativePositionX()
    {
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(EncounterX, seed: 23);
        long travelling = camera.WorldOffsetUnits;

        camera.FrameEncounter(ArenaMinimumX, ArenaMaximumX);
        camera.FollowTravel(EncounterX, seed: 23);

        // Resuming reads Travel.PositionX back, not some offset combat
        // banked on the side.
        Assert.Equal(travelling, camera.WorldOffsetUnits);
        Assert.Same(camera.Chunks, camera.Chunks);
    }

    [Fact]
    public void FullSequence_TravelEncounterObjectiveReturn_MovesTheWorldOneWayThenBack()
    {
        // Tracked through a fixed landmark rather than the window's
        // leftmost seam: the window recentres every time the offset
        // crosses a chunk boundary, so its edge sawtooths while the
        // world underneath is moving perfectly steadily.
        var camera = new ExpeditionPathCamera();
        var outbound = new List<float>();

        foreach (double positionX in Route(CityX, EncounterX))
        {
            camera.FollowTravel(positionX, seed: 3);
            outbound.Add(LandmarkX(camera));
        }
        for (int i = 1; i < outbound.Count; i++)
        {
            Assert.True(
                outbound[i] <= outbound[i - 1],
                "Walking forward must slide the world leftward, never back.");
        }
        Assert.True(outbound[^1] < outbound[0]);

        camera.FrameEncounter(ArenaMinimumX, ArenaMaximumX);
        ExpeditionPathChunkPool pool = camera.Chunks!;

        camera.FollowTravel(ObjectiveX, seed: 3);
        float atObjective = LandmarkX(camera);

        var home = new List<float>();
        foreach (double positionX in Route(ObjectiveX, CityX))
        {
            camera.FollowTravel(positionX, seed: 3);
            home.Add(LandmarkX(camera));
        }

        for (int i = 1; i < home.Count; i++)
        {
            Assert.True(
                home[i] >= home[i - 1],
                "The return leg must slide the world back the other way.");
        }
        Assert.True(home[^1] > atObjective);
        // One pool for the whole expedition; the return leg reverses
        // the same window rather than rebuilding it.
        Assert.Same(pool, camera.Chunks);
        Assert.Equal(ExpeditionPathChunkPool.ChunkCount, camera.Chunks!.Chunks.Count);
    }

    [Fact]
    public void ReturningToAStretch_ShowsTheSameStretch()
    {
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(EncounterX, seed: 77);
        IReadOnlyList<float> outbound = ExpeditionPathComposition.ChunkSeams(
            camera.Chunks!.Chunks, camera.WorldOffsetUnits, Anchor);
        Dictionary<long, (int Biome, int Props)> dressing = camera.Chunks.Chunks
            .ToDictionary(c => c.LogicalIndex, c => (c.BiomeId, c.PropCount));

        camera.FollowTravel(ObjectiveX, seed: 77);
        camera.FollowTravel(EncounterX, seed: 77);

        Assert.Equal(
            outbound,
            ExpeditionPathComposition.ChunkSeams(
                camera.Chunks!.Chunks, camera.WorldOffsetUnits, Anchor));
        foreach (ExpeditionPathChunk chunk in camera.Chunks.Chunks)
        {
            Assert.Equal(dressing[chunk.LogicalIndex], (chunk.BiomeId, chunk.PropCount));
        }
    }

    /// <summary>Screen X of the objective — a fixed place in the world
    /// — under the camera's current offset.</summary>
    private static float LandmarkX(ExpeditionPathCamera camera) =>
        ExpeditionPathRenderer.PlayableScreenX(
            ObjectiveX, camera.WorldOffsetUnits, Anchor);

    private static IEnumerable<double> Route(double from, double to)
    {
        const int steps = 20;
        for (int i = 0; i <= steps; i++)
        {
            yield return from + (to - from) * (i / (double)steps);
        }
    }
}
