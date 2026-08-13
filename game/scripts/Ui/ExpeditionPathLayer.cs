#nullable enable
namespace WorldofGoses.Ui;

/// <summary>
/// The four planes of the expedition path, nearest last.
///
/// <para>
/// A layer is a purely visual grouping: it says how fast a plane
/// slides against the world offset and what it is allowed to carry.
/// It never carries a mechanical coordinate. The party's position is
/// still one number, <c>PositionX</c>; the layers decide where that
/// number is drawn, not what it means.
/// </para>
/// </summary>
public enum ExpeditionPathLayer
{
    /// <summary>The far backdrop. Owns no chunk and no gameplay.</summary>
    Distance = 0,

    /// <summary>Ground rows behind the party, plus their dressing.</summary>
    Rear = 1,

    /// <summary>
    /// The one band the party, the enemies and the objective stand
    /// on. There is exactly one, and
    /// <see cref="ExpeditionPathRenderer.PlayableDepth"/> names it.
    /// </summary>
    Playable = 2,

    /// <summary>The fringe drawn in front of the party.</summary>
    Foreground = 3,
}
