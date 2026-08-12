#nullable enable
using Godot;

namespace WorldofGoses;

/// <summary>
/// Typed contract that the macro street view publishes when its
/// world-projected anchors change. Carries the founder's projected
/// screen position and (when a campfire exists) the campfire's
/// projected screen position, plus a flag that tells the night
/// whether a real anchor was resolved.
///
/// <para>
/// Architecture Hardening A9 closes the dynamic-dispatch seam that
/// the previous prototype used to invoke
/// <c>GetFoundingArrivalGlobalPosition()</c> and
/// <c>GetBuildingGlobalPosition(int)</c> through
/// <c>HasMethod</c> + <c>Node.Call</c>. The macro view now exposes
/// the anchors as an immutable record and notifies subscribers
/// through a typed Godot signal, so <see cref="FirstNightScene"/>
/// refreshes its cached positions only when the camera or projection
/// moves. The night stops polling every frame to read positions the
/// camera never changed.</para>
///
/// <para>
/// Coordinates are screen pixels (after <c>ToGlobal</c>) — the unit
/// the speech bubble, embers and spirit visual already speak. The
/// record lives in Presentation because these coordinates are
/// presentation state; Domain and Application never see them.</para>
/// </summary>
public readonly record struct WorldDialogueAnchors(
    Vector2 FounderScreenPosition,
    Vector2 CampfireScreenPosition,
    bool HasCampfireAnchor)
{
    /// <summary>
    /// Placeholder anchors used by tests and editor fixtures where the
    /// macro view is absent. The founder lands at the viewport
    /// centre; the campfire sits 20 px below it and starts unresolved
    /// so the spirit waits beside the founder until the founding
    /// site module actually completes.
    /// </summary>
    public static WorldDialogueAnchors Placeholder() =>
        new(new Vector2(640, 360), new Vector2(640, 380), HasCampfireAnchor: false);
}
