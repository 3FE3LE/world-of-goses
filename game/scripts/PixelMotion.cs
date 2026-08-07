using Godot;

namespace WorldofGoses;

public static class PixelMotion
{
    /// <summary>
    /// The shared step cadence. Motion in this game is deliberately discrete —
    /// characters and the camera advance in whole steps, never by continuous
    /// interpolation — but 8 px at 12 Hz landed hard enough to read as a jerk
    /// rather than as a gait. Halving the step and doubling the rate keeps the
    /// grammar and the effective speed (<see cref="StepPixels"/> ÷
    /// <see cref="CadenceSeconds"/> is unchanged at 96 px/s) while halving how
    /// far anything jumps at once.
    ///
    /// <para>
    /// Anything that advances a fixed fraction per cadence tick — the camera's
    /// depth pan, the building-entry zoom — must double its step count to keep
    /// its duration, since ticks now arrive twice as often.
    /// </para>
    /// </summary>
    public const float CadenceSeconds = 1f / 24f;

    public const float StepPixels = 4f;

    public static Vector2 Snap(Vector2 value) =>
        new(Mathf.Round(value.X), Mathf.Round(value.Y));

    public static Vector2 StepCardinal(Vector2 current, Vector2 target)
    {
        current = Snap(current);
        target = Snap(target);
        Vector2 remaining = target - current;
        return Mathf.Abs(remaining.X) > 0f
            ? new Vector2(
                Mathf.MoveToward(current.X, target.X, StepPixels),
                current.Y)
            : new Vector2(
                current.X,
                Mathf.MoveToward(current.Y, target.Y, StepPixels));
    }
}
