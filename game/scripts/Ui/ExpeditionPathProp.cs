#nullable enable
namespace WorldofGoses.Ui;

/// <summary>
/// One piece of dressing owned by a chunk, resolved to screen space.
///
/// <para>
/// Props are what make the scroll visible. A road of identical tiles
/// slides underneath the party without a single pixel appearing to
/// change; the bushes, rocks and far silhouettes moving past at
/// different rates are the whole illusion. Which is why they hang off
/// the chunk's deterministic dressing rather than off a timer: the
/// same stretch of world always grows the same things.
/// </para>
/// </summary>
public readonly record struct ExpeditionPathProp(
    ExpeditionPathLayer Layer,
    long LogicalIndex,
    int BiomeId,
    float ScreenX,
    float ScreenBaseY,
    float WidthPx,
    float HeightPx);
