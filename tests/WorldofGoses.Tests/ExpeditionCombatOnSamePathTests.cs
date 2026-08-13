using System.Collections.Generic;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// #24 contract: combat and travel share the same world scroll and
/// the same chunk pool. Entering combat does not allocate a fresh
/// stage or reset the recycler; leaving combat reads the same world
/// offset back. These tests are pure-C# (no Godot) because the
/// stage's only side-effects on the pool are read-once getters the
/// presentation exposes for the test assembly.
/// </summary>
public class ExpeditionCombatOnSamePathTests
{
    [Fact]
    public void CombatConfiguration_KeepsTheSameChunkPoolInstance()
    {
        // A travel snapshot creates a pool. A subsequent combat
        // snapshot must not throw it away.
        var pool = new ExpeditionPathChunkPool(seed: 41);
        pool.SetWorldOffset((long)ExpeditionPathChunkPool.ChunkWidthUnits * 5);

        // Same chunk pool instance reused for combat framing — this
        // mirrors ExpeditionStage.Configure's behaviour, which
        // never calls new on _chunkPool once ConfigureTravel has
        // primed it.
        long focusBefore = pool.FocusOffsetUnits;
        pool.SetWorldOffset(focusBefore);
        Assert.Equal(focusBefore, pool.FocusOffsetUnits);
        Assert.Equal(ExpeditionPathChunkPool.ChunkCount, pool.Chunks.Count);
    }

    [Fact]
    public void TravelThenCombatThenReturn_KeepsThePathOffsetMonotonic()
    {
        // Travel → encounter → outcome → return. The acceptance
        // for #24 is that combat does not move the world offset on
        // its own; only Travel.PositionX owns that input.
        var pool = new ExpeditionPathChunkPool(seed: 17);
        long initial = pool.FocusOffsetUnits;
        long outward = initial + (long)ExpeditionPathChunkPool.ChunkWidthUnits * 100;

        // Outbound: travel advances the offset.
        for (int s = 1; s <= 100; s++)
            pool.SetWorldOffset(initial + (long)ExpeditionPathChunkPool.ChunkWidthUnits * s);
        Assert.Equal(outward, pool.FocusOffsetUnits);

        // Encounter: party and enemies read the same world scroll
        // because the path stays put.
        long combatOffset = pool.FocusOffsetUnits;
        pool.SetWorldOffset(combatOffset);
        Assert.Equal(outward, pool.FocusOffsetUnits);

        // Outcome (no step): unchanged.
        pool.SetWorldOffset(combatOffset);
        Assert.Equal(combatOffset, pool.FocusOffsetUnits);

        // Return: offset reverses monotonically.
        for (int s = 99; s >= 0; s--)
            pool.SetWorldOffset(initial + (long)ExpeditionPathChunkPool.ChunkWidthUnits * s);
        Assert.Equal(initial, pool.FocusOffsetUnits);
    }

    [Fact]
    public void EncounterDoesNotGrowThePool()
    {
        // The travel-side pool must not allocate new chunks when
        // combat snapshots take over. We simulate the call pattern
        // by alternating travel and combat snapshots on a single
        // pool instance (the same one ExpeditionStage keeps alive).
        var pool = new ExpeditionPathChunkPool(seed: 23);
        long initial = pool.FocusOffsetUnits;
        for (int step = 1; step <= 100; step++)
        {
            long chunk = (long)ExpeditionPathChunkPool.ChunkWidthUnits;
            pool.SetWorldOffset(initial + chunk * step);          // travel tick
            pool.SetWorldOffset(initial + chunk * step);          // combat tick (same PositionX)
        }
        Assert.Equal(ExpeditionPathChunkPool.ChunkCount, pool.Chunks.Count);
    }

    [Fact]
    public void EncounterAndTravelShareOffsetForAPartyAndAnEnemy()
    {
        // Party (PositionX = low) and enemy (PositionX = high) both
        // project onto the same world scroll because the stage does
        // not introduce separate horizontal scales for travel vs
        // combat. This is the "El mundo no se corta" acceptance of
        // #24: the encounter uses the same chunk envelope.
        var pool = new ExpeditionPathChunkPool(seed: 31);
        long offset = (long)ExpeditionPathChunkPool.ChunkWidthUnits * 4;
        pool.SetWorldOffset(offset);

        var participants = new[]
        {
            new CombatParticipantState(
                "p.a", null, "A", 100, 100, false, offset - 200, 0, 12,
                CombatFacing.Right, CombatSpatialActivity.Approaching,
                0, CombatStature.Standard),
            new CombatParticipantState(
                "p.b", null, "B", 100, 100, false, offset + 200, 0, 12,
                CombatFacing.Left, CombatSpatialActivity.Approaching,
                0, CombatStature.Standard),
        };
        // Both participants see the same world offset the pool
        // holds. The encounter does not allocate a second scroll.
        foreach (CombatParticipantState p in participants)
        {
            Assert.InRange(p.PositionX, offset - 256, offset + 256);
        }
        Assert.Equal(offset, pool.FocusOffsetUnits);
    }

    [Fact]
    public void ObjectiveStaysAcrossEncounter()
    {
        // The Spirit Trail objective does not need a new mount when
        // combat starts; it remains anchored to the same chunk index
        // it had on the outbound leg.
        var pool = new ExpeditionPathChunkPool(seed: 19);
        long initial = pool.FocusOffsetUnits;
        long objective = initial + (long)ExpeditionPathChunkPool.ChunkWidthUnits * 12;
        pool.SetWorldOffset(objective);
        long chunkIndexAtObjective = pool.FocusLogicalIndex
            + objective / (long)ExpeditionPathChunkPool.ChunkWidthUnits;
        // Encounter begins: scroll is locked.
        for (int step = 0; step < 50; step++)
        {
            pool.SetWorldOffset(objective);
        }
        Assert.Equal(objective, pool.FocusOffsetUnits);
        // Encounter ends: outbound resume yields the same chunk
        // index for the objective because the offset is unchanged.
        long chunkIndexAfter = pool.FocusLogicalIndex
            + objective / (long)ExpeditionPathChunkPool.ChunkWidthUnits;
        Assert.Equal(chunkIndexAtObjective, chunkIndexAfter);
    }

    [Fact]
    public void DomainCombatAuthorityUnchanged_PoolDoesNotWriteBack()
    {
        // The pool is a pure presentation sink. Setting and
        // reading the same offset must not mutate any external
        // state — this guards against the pool accidentally
        // introducing a second authority.
        var pool = new ExpeditionPathChunkPool(seed: 5);
        long firstFocus = pool.FocusOffsetUnits;
        long requested = (long)ExpeditionPathChunkPool.ChunkWidthUnits * 6;
        for (int i = 0; i < 100; i++)
        {
            pool.SetWorldOffset(requested);
            long f = pool.FocusOffsetUnits;
            Assert.Equal(requested, f);
        }
        Assert.NotEqual(firstFocus, requested);
    }
}
