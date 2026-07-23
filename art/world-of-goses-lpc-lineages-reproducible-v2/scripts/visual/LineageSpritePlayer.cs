using Godot;

namespace WorldOfGoses.Visuals;

public partial class LineageSpritePlayer : AnimatedSprite2D
{
    private Vector2 _facing = Vector2.Down;

    public void PlayDirectional(string animationName, Vector2 direction)
    {
        _facing = NormalizeDirection(direction, _facing);
        Play($"{animationName}_{ToSuffix(_facing)}");
    }

    public void PlayIdle(Vector2 direction) => PlayDirectional("idle", direction);
    public void PlayWalk(Vector2 direction) => PlayDirectional("walk", direction);
    public void PlaySlash(Vector2 direction) => PlayDirectional("slash", direction);
    public void ResumeIdle() => Play($"idle_{ToSuffix(_facing)}");

    private static Vector2 NormalizeDirection(Vector2 direction, Vector2 fallback)
    {
        if (direction.IsZeroApprox())
        {
            return fallback;
        }

        return Mathf.Abs(direction.X) > Mathf.Abs(direction.Y)
            ? new Vector2(Mathf.Sign(direction.X), 0)
            : new Vector2(0, Mathf.Sign(direction.Y));
    }

    private static string ToSuffix(Vector2 direction)
    {
        if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
        {
            return direction.X < 0 ? "left" : "right";
        }

        return direction.Y < 0 ? "up" : "down";
    }
}
