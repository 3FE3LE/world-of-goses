using Godot;

namespace WorldofGoses;

public static class PixelMotion
{
    public const float CadenceSeconds = 1f / 12f;
    public const float StepPixels = 8f;

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
