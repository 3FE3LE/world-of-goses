#nullable enable
namespace WorldofGoses.Ui;

/// <summary>
/// One ground row of the expedition path, already resolved to screen
/// coordinates and to the plane it belongs to.
///
/// <para>
/// The point of handing the stage a resolved band instead of letting
/// it compute one per <c>_Draw</c> is that a band can then be asserted
/// on. A test can ask where the playable row actually landed and
/// whether the party's Y agrees, instead of comparing two constants
/// and hoping the drawing code used them.
/// </para>
/// </summary>
public readonly record struct ExpeditionPathBand(
    float Depth,
    ExpeditionPathLayer Layer,
    bool IsPlayable,
    float ScreenYNear,
    float ScreenYFar,
    float LeftNear,
    float RightNear,
    float LeftFar,
    float RightFar);
