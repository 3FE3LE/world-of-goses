#nullable enable
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Shared presentation-only motion grammar. Durations and distances live here
/// so future reduced-motion settings can tune every consumer consistently.
/// World-state and simulation code must never depend on these tweens.
/// </summary>
public static class UiMotion
{
    public const double ModalRevealSeconds = 0.16;
    public const double ModalHideSeconds = 0.12;
    public const float ModalTravelPixels = 8f;

    /// <summary>Total length of a "large event" feedback flash. Long
    /// enough to read at the edge of the viewport but short enough to
    /// not block the next input.</summary>
    public const double LargeEventSeconds = 0.45;

    public const double BuildingEntrySeconds = 0.16;

    /// <summary>
    /// Plain fade-in, no scale/pivot animation — the "camera push" toward
    /// a clicked building happens on the map itself before this content
    /// ever shows (<c>MacroStreetLiveView.BeginBuildingEntry</c>), so this
    /// view doesn't need its own zoom too (2026-07-27: it used to, but
    /// that zoomed the UI panel instead of the world the user actually
    /// asked to zoom toward).
    /// </summary>
    public static Tween FadeIn(CanvasItem content)
    {
        content.Modulate = new Color(1f, 1f, 1f, 0f);
        Tween tween = content.CreateTween();
        tween.TweenProperty(content, "modulate:a", 1f, BuildingEntrySeconds);
        return tween;
    }

    public static Tween RevealModal(
        Node owner,
        ColorRect scrim,
        Control content,
        Color scrimTarget,
        Vector2 restingPosition)
    {
        Color hiddenScrim = scrimTarget;
        hiddenScrim.A = 0f;
        scrim.Color = hiddenScrim;
        content.Position = restingPosition + new Vector2(0, ModalTravelPixels);
        content.Modulate = new Color(1f, 1f, 1f, 0f);

        Tween tween = owner.CreateTween().SetParallel(true);
        tween.TweenProperty(scrim, "color", scrimTarget, ModalRevealSeconds)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(content, "modulate:a", 1f, ModalRevealSeconds);
        tween.TweenMethod(
                Callable.From<float>(offset =>
                    content.Position = restingPosition
                        + new Vector2(0, Mathf.Round(offset))),
                ModalTravelPixels,
                0f,
                ModalRevealSeconds)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        return tween;
    }

    public static Tween HideModal(
        Node owner,
        ColorRect scrim,
        Control content,
        Vector2 restingPosition,
        Callable completed)
    {
        Tween tween = owner.CreateTween().SetParallel(true);
        tween.TweenProperty(scrim, "color:a", 0f, ModalHideSeconds);
        tween.TweenProperty(content, "modulate:a", 0f, ModalHideSeconds);
        tween.TweenMethod(
                Callable.From<float>(offset =>
                    content.Position = restingPosition
                        + new Vector2(0, Mathf.Round(offset))),
                0f,
                4f,
                ModalHideSeconds)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.Chain().TweenCallback(completed);
        return tween;
    }

    public static void Pulse(CanvasItem item, Color accent)
    {
        if (!GodotObject.IsInstanceValid(item)) return;
        item.Modulate = new Color(accent.R, accent.G, accent.B, 0.58f);
        Tween tween = item.CreateTween();
        tween.TweenProperty(item, "modulate", Colors.White, 0.18)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// Long-form flash reserved for events the player must notice
    /// without the input ever stalling: a construction completing, an
    /// expedition returning, a new citizen arriving. The modulation
    /// dips to the lineage accent and then returns to white over
    /// <see cref="LargeEventSeconds"/>. Does not block input.
    /// </summary>
    public static void FlashLarge(CanvasItem item, Color accent)
    {
        if (!GodotObject.IsInstanceValid(item)) return;
        item.Modulate = Colors.White;
        double rampUp = LargeEventSeconds * 0.4;
        double rampDown = LargeEventSeconds - rampUp;
        Tween tween = item.CreateTween();
        tween.TweenProperty(
            item, "modulate",
            new Color(accent.R, accent.G, accent.B, 0.92f), rampUp)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(item, "modulate", Colors.White, rampDown)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }
}
