#nullable enable
using System;
using System.Collections.Generic;
using WorldofGoses.Prototypes;

namespace WorldofGoses.Ui;

/// <summary>
/// Turns a world offset and a window of chunks into the exact screen
/// geometry the expedition stage paints.
///
/// <para>
/// This class exists because of what the reopening of #22-#25 found:
/// the recycler, the parallax factors and the dressing all existed and
/// were all tested, and none of them reached a pixel. The stage drew
/// static bands beside them. Tests that build their own pool prove the
/// pool; they cannot prove the picture. So the decisions about what
/// the path looks like live here, in pure functions the stage calls
/// and a test can call too — the same call, not a re-enactment of it.
/// </para>
///
/// <para>
/// Nothing here is a Godot node and nothing here is mechanical state.
/// Every output is a pure function of (chunks, world offset, anchor).
/// </para>
/// </summary>
public static class ExpeditionPathComposition
{
    /// <summary>
    /// Depth the rear dressing stands on: the lot depth immediately behind the
    /// calle the party walks.
    /// </summary>
    /// <remarks>
    /// Derived from the playable band rather than authored as <c>2f</c>, which
    /// was behind the party only while the playable band was row 0. With the
    /// band on the calle at row 3 that literal put every rear prop *in front*
    /// of the group — <c>RearDressing_StaysBehindThePlayableBand</c> is the
    /// test that says so.
    /// </remarks>
    public const float RearPropDepth = ExpeditionPathRenderer.PlayableDepth + 1f;

    /// <summary>How tall a rear prop is on the playable band's scale,
    /// before its own row's perspective shrinks it.</summary>
    public const float RearPropBaseHeightPx = 26f;

    /// <summary>How tall a foreground prop is. Larger than the rear so
    /// the fringe reads as close even before it moves.</summary>
    public const float ForegroundPropHeightPx = 58f;

    /// <summary>Width of one distant silhouette block.</summary>
    public const float DistanceBlockWidthPx = 34f;

    /// <summary>World-space spacing between distant silhouette blocks.
    /// The backdrop owns no chunk, so it repeats on its own rhythm.</summary>
    public const float DistanceBlockSpacingUnits = 150f;

    /// <summary>
    /// The ground rows, nearest first, including the foreground
    /// fringe at negative depth.
    /// </summary>
    public static IReadOnlyList<ExpeditionPathBand> Bands(in ExpeditionPathAnchor anchor)
    {
        var bands = new List<ExpeditionPathBand>(ExpeditionPathRenderer.RowCount + 1);
        for (float depth = ExpeditionPathRenderer.ForegroundDepth;
            depth < ExpeditionPathRenderer.RowCount;
            depth += 1f)
        {
            float depthFar = depth + 1f;
            float scaleNear = StreetDepthProjection.HorizontalScale(depth);
            float scaleFar = StreetDepthProjection.HorizontalScale(depthFar);
            bands.Add(new ExpeditionPathBand(
                Depth: depth,
                Layer: ExpeditionPathRenderer.LayerForDepth(depth),
                IsPlayable: ExpeditionPathRenderer.IsPlayableDepth(depth),
                ScreenYNear: ExpeditionPathRenderer.RowScreenY(depth, anchor),
                ScreenYFar: ExpeditionPathRenderer.RowScreenY(depthFar, anchor),
                LeftNear: anchor.CenterX - anchor.HalfWidthPx * scaleNear,
                RightNear: anchor.CenterX + anchor.HalfWidthPx * scaleNear,
                LeftFar: anchor.CenterX - anchor.HalfWidthPx * scaleFar,
                RightFar: anchor.CenterX + anchor.HalfWidthPx * scaleFar));
        }
        return bands;
    }

    /// <summary>
    /// Screen X of every chunk boundary on the playable band. Drawn as
    /// a tread mark so the road itself, not only the dressing beside
    /// it, is seen to move.
    /// </summary>
    public static IReadOnlyList<float> ChunkSeams(
        IReadOnlyList<ExpeditionPathChunk> chunks,
        long worldOffsetUnits,
        in ExpeditionPathAnchor anchor)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        var seams = new List<float>(chunks.Count);
        foreach (ExpeditionPathChunk chunk in chunks)
        {
            seams.Add(ExpeditionPathRenderer.PlayableScreenX(
                chunk.WorldStartUnits, worldOffsetUnits, anchor));
        }
        return seams;
    }

    /// <summary>
    /// The dressing of the chunk window: rear props from every chunk's
    /// deterministic prop count, plus one foreground prop per chunk.
    /// Bounded by construction — the pool never grows, so neither does
    /// this list.
    /// </summary>
    public static IReadOnlyList<ExpeditionPathProp> Props(
        IReadOnlyList<ExpeditionPathChunk> chunks,
        long worldOffsetUnits,
        in ExpeditionPathAnchor anchor)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        float rearFactor = ExpeditionPathParallax.RearFactor;
        float rearScale = StreetDepthProjection.VerticalScale(RearPropDepth);
        float rearY = ExpeditionPathRenderer.RowScreenY(RearPropDepth, anchor);
        float foregroundFactor = ExpeditionPathRenderer.ParallaxFactorForDepth(
            ExpeditionPathRenderer.ForegroundDepth);
        float foregroundY = ExpeditionPathRenderer.RowScreenY(
            ExpeditionPathRenderer.ForegroundDepth, anchor);

        var props = new List<ExpeditionPathProp>();
        foreach (ExpeditionPathChunk chunk in chunks)
        {
            for (int i = 0; i < chunk.PropCount; i++)
            {
                double worldX = chunk.PropWorldX(i);
                props.Add(new ExpeditionPathProp(
                    Layer: ExpeditionPathLayer.Rear,
                    LogicalIndex: chunk.LogicalIndex,
                    BiomeId: chunk.BiomeId,
                    ScreenX: ExpeditionPathRenderer.WorldToScreenX(
                        worldX, worldOffsetUnits, rearFactor, anchor),
                    ScreenBaseY: rearY,
                    WidthPx: RearPropBaseHeightPx * rearScale * 0.7f,
                    HeightPx: RearPropBaseHeightPx * rearScale * (1f + (i & 1) * 0.4f)));
            }

            // One fringe prop per chunk, anchored to the chunk's middle
            // so the foreground density stays flat however the biome
            // decides to dress the rear.
            double fringeWorldX = chunk.WorldStartUnits
                + ExpeditionPathChunkPool.ChunkWidthUnits * 0.5;
            props.Add(new ExpeditionPathProp(
                Layer: ExpeditionPathLayer.Foreground,
                LogicalIndex: chunk.LogicalIndex,
                BiomeId: chunk.BiomeId,
                ScreenX: ExpeditionPathRenderer.WorldToScreenX(
                    fringeWorldX, worldOffsetUnits, foregroundFactor, anchor),
                ScreenBaseY: foregroundY,
                WidthPx: ForegroundPropHeightPx * 0.55f,
                HeightPx: ForegroundPropHeightPx));
        }
        return props;
    }

    /// <summary>
    /// The far silhouette. It owns no chunk — it is not a place, it is
    /// a horizon — so it repeats on its own world rhythm and crawls at
    /// <see cref="ExpeditionPathParallax.DistanceFactor"/>.
    /// </summary>
    public static IReadOnlyList<ExpeditionPathProp> DistanceBlocks(
        long worldOffsetUnits,
        float stageWidth,
        in ExpeditionPathAnchor anchor)
    {
        float factor = ExpeditionPathRenderer.ParallaxFactorForDepth(
            ExpeditionPathRenderer.RowCount - 1f);
        float horizonY = ExpeditionPathRenderer.RowScreenY(
            ExpeditionPathRenderer.RowCount - 1f, anchor);

        // Walk world positions around the offset rather than screen
        // positions, so the blocks belong to the world and reappear in
        // the same places on a return leg.
        long firstIndex = (long)Math.Floor(
            (worldOffsetUnits - stageWidth) / DistanceBlockSpacingUnits);
        long lastIndex = (long)Math.Ceiling(
            (worldOffsetUnits + stageWidth) / DistanceBlockSpacingUnits);
        var blocks = new List<ExpeditionPathProp>();
        for (long index = firstIndex; index <= lastIndex; index++)
        {
            float screenX = ExpeditionPathRenderer.WorldToScreenX(
                index * DistanceBlockSpacingUnits, worldOffsetUnits, factor, anchor);
            if (screenX < -DistanceBlockWidthPx || screenX > stageWidth + DistanceBlockWidthPx)
            {
                continue;
            }
            int silhouette = (int)(((index % 3) + 3) % 3);
            blocks.Add(new ExpeditionPathProp(
                Layer: ExpeditionPathLayer.Distance,
                LogicalIndex: index,
                BiomeId: silhouette,
                ScreenX: screenX,
                ScreenBaseY: horizonY,
                WidthPx: DistanceBlockWidthPx,
                HeightPx: 12f + silhouette * 7f));
        }
        return blocks;
    }
}
