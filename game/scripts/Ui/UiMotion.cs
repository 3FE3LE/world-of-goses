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
}
