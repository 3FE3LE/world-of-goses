using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// #23 contract: <c>Travel.PositionX</c> moves the world, and the
/// party holds a focal position while it goes past.
///
/// <para>
/// The reopening put it exactly: the offset was being computed and fed
/// to a chunk pool nothing drew, while the founder was projected
/// straight onto screen X. So the party crossed a world that never
/// moved — the reverse of the contract, with a green suite on top
/// because the tests only ever asked the pool about itself. These
/// assertions read screen coordinates out of the same composition the
/// stage draws.
/// </para>
/// </summary>
public class ExpeditionWorldScrollTests
{
    private static readonly ExpeditionPathAnchor Anchor =
        ExpeditionPathAnchor.For(new Vector2(800f, 460f));
    private const long ChunkWidth = (long)ExpeditionPathChunkPool.ChunkWidthUnits;

    [Fact]
    public void SamePositionX_ProducesTheSamePicture()
    {
        // Presentation derived from a snapshot has to be a pure
        // function of it; anything else means a hidden clock.
        var first = new ExpeditionPathCamera();
        var second = new ExpeditionPathCamera();
        first.FollowTravel(640.4, seed: 7);
        second.FollowTravel(640.4, seed: 7);

        Assert.Equal(first.WorldOffsetUnits, second.WorldOffsetUnits);
        Assert.Equal(Seams(first), Seams(second));
    }

    [Fact]
    public void SustainedTravel_MovesTheTerrainAndNotTheParty()
    {
        // The heart of #23, stated as two measurements taken at the
        // same moments: where the ground is, and where the founder is.
        var camera = new ExpeditionPathCamera();
        var groundPositions = new List<float>();
        var partyPositions = new List<float>();

        for (double positionX = 100; positionX <= 900; positionX += 37)
        {
            camera.FollowTravel(positionX, seed: 11);
            groundPositions.Add(ExpeditionPathRenderer.PlayableScreenX(
                850, camera.WorldOffsetUnits, Anchor));
            // Read the same property the stage reads. Recomputing the walker's
            // position here would re-enact what the test believes the stage
            // does, which is the exact failure this file's header describes.
            partyPositions.Add(ExpeditionPathRenderer.PlayableScreenX(
                camera.TravelDrawPositionX, camera.WorldOffsetUnits, Anchor));
        }

        Assert.True(
            groundPositions[^1] < groundPositions[0] - 700f,
            "Eight hundred units of travel must move the world by roughly as much.");
        foreach (float partyX in partyPositions)
        {
            // "A stable focus", made specific: the walker holds the anchor
            // exactly, and it is the ground that steps past them.
            Assert.InRange(partyX, Anchor.CenterX - 1f, Anchor.CenterX + 1f);
        }
    }

    [Fact]
    public void ObjectiveIsProjectedInWorldSpace_AndApproaches()
    {
        // Not pinned to a fraction of the stage: it is a place, and it
        // gets nearer because the party gets nearer to it.
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(200, seed: 2);
        float far = ExpeditionPathRenderer.PlayableScreenX(850, camera.WorldOffsetUnits, Anchor);

        camera.FollowTravel(600, seed: 2);
        float near = ExpeditionPathRenderer.PlayableScreenX(850, camera.WorldOffsetUnits, Anchor);

        camera.FollowTravel(850, seed: 2);
        float arrived = ExpeditionPathRenderer.PlayableScreenX(
            850, camera.WorldOffsetUnits, Anchor);

        Assert.True(near < far);
        Assert.True(arrived < near);
        // Standing on it means standing on it — to within one step. In a world
        // that advances a locomotion step at a time, an objective at an
        // arbitrary world position cannot land on the grid, and pretending it
        // does would mean un-quantizing the camera for one marker.
        Assert.InRange(
            arrived,
            Anchor.CenterX - (float)ExpeditionPathCamera.StepUnits,
            Anchor.CenterX + (float)ExpeditionPathCamera.StepUnits);
    }

    [Fact]
    public void OutboundAndReturn_InvertThroughOneAuthority()
    {
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(300, seed: 4);
        IReadOnlyList<float> outbound = Seams(camera);

        camera.FollowTravel(900, seed: 4);
        camera.FollowTravel(300, seed: 4);

        // No second clock and no persisted offset: the same
        // PositionX yields the same frame whichever leg produced it.
        Assert.Equal(outbound, Seams(camera));
    }

    [Fact]
    public void ThousandsOfUpdates_DoNotGrowThePool()
    {
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(0, seed: 19);
        ExpeditionPathChunkPool pool = camera.Chunks!;

        for (int step = 1; step <= 1000; step++)
        {
            camera.FollowTravel(step * ChunkWidth, seed: 19);
            Assert.Equal(ExpeditionPathChunkPool.ChunkCount, camera.Chunks!.Chunks.Count);
        }
        for (int step = 999; step >= 500; step--)
        {
            camera.FollowTravel(step * ChunkWidth, seed: 19);
        }

        Assert.Same(pool, camera.Chunks);
        Assert.Equal(ExpeditionPathChunkPool.ChunkCount, camera.Chunks!.Chunks.Count);
    }

    [Fact]
    public void AStretchOfWorld_KeepsItsChunkIndexAcrossARecycle()
    {
        var camera = new ExpeditionPathCamera();
        long objective = 25 * ChunkWidth;
        camera.FollowTravel(objective, seed: 13);
        long indexAtObjective = camera.Chunks!.FocusLogicalIndex;

        for (int s = 26; s <= 200; s++) camera.FollowTravel(s * ChunkWidth, seed: 13);
        for (int s = 200; s >= 25; s--) camera.FollowTravel(s * ChunkWidth, seed: 13);

        Assert.Equal(indexAtObjective, camera.Chunks!.FocusLogicalIndex);
        Assert.Equal(objective, camera.WorldOffsetUnits);
    }

    [Fact]
    public void ScrollIsQuantisedToWholeLocomotionSteps()
    {
        // This asserted rounding to the nearest world unit, and called that
        // "the ground steps rather than sliding sub-pixel". A one-pixel grid at
        // 96 px/s is not a step, it is a slide with the rounding error taken
        // out: the intent was right and the granularity was not. The offset now
        // lands on the same cadence a character walks by.
        var camera = new ExpeditionPathCamera();
        foreach (double positionX in new[] { 100.2, 100.5, 100.9, 101.4, 103.9 })
        {
            camera.FollowTravel(positionX, seed: 1);
            Assert.Equal(0, camera.WorldOffset % ExpeditionPathCamera.StepUnits);
            Assert.True(
                Math.Abs(camera.WorldOffset - positionX) <= ExpeditionPathCamera.StepUnits / 2,
                $"{positionX} snapped to {camera.WorldOffset}, further than half a step.");
        }
    }

    private static IReadOnlyList<float> Seams(ExpeditionPathCamera camera) =>
        ExpeditionPathComposition.ChunkSeams(
            camera.Chunks!.Chunks, camera.WorldOffsetUnits, Anchor);
}
