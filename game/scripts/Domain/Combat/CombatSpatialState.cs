#nullable enable
using System;

namespace WorldofGoses.Domain.Combat;

public enum CombatFacing
{
    Left = -1,
    Right = 1,
}

public enum CombatSpatialActivity
{
    Idle,
    Approaching,
    BasicAttack,
    ActiveSkill,
    Hit,
    Knockback,
    Defeated,
}

/// <summary>
/// Authoritative one-dimensional spatial state for one encounter participant.
/// Godot nodes only project this state; they never write positions back into it.
/// </summary>
public sealed class CombatSpatialState
{
    public CombatSpatialState(
        double positionX = 0,
        double movementSpeed = 1,
        double attackRange = 48,
        double bodyRadius = 12,
        double stability = 50,
        double impulse = 50,
        CombatFacing facing = CombatFacing.Right)
    {
        if (!double.IsFinite(positionX)) throw new ArgumentOutOfRangeException(nameof(positionX));
        if (!double.IsFinite(movementSpeed) || movementSpeed < 0)
            throw new ArgumentOutOfRangeException(nameof(movementSpeed));
        if (!double.IsFinite(attackRange) || attackRange < 0)
            throw new ArgumentOutOfRangeException(nameof(attackRange));
        if (!double.IsFinite(bodyRadius) || bodyRadius <= 0)
            throw new ArgumentOutOfRangeException(nameof(bodyRadius));
        if (!double.IsFinite(stability) || stability < 0)
            throw new ArgumentOutOfRangeException(nameof(stability));
        if (!double.IsFinite(impulse) || impulse < 0)
            throw new ArgumentOutOfRangeException(nameof(impulse));
        if (!Enum.IsDefined(facing)) throw new ArgumentOutOfRangeException(nameof(facing));

        PositionX = positionX;
        MovementSpeed = movementSpeed;
        AttackRange = attackRange;
        BodyRadius = bodyRadius;
        Stability = stability;
        Impulse = impulse;
        Facing = facing;
    }

    public double PositionX { get; private set; }
    public double MovementSpeed { get; }
    public double AttackRange { get; }
    public double BodyRadius { get; }
    public double Stability { get; }
    public double Impulse { get; }
    public CombatFacing Facing { get; private set; }
    public CombatSpatialActivity Activity { get; private set; } = CombatSpatialActivity.Idle;
    public double LastDisplacement { get; private set; }

    public double EdgeDistanceTo(CombatSpatialState other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Math.Max(0, Math.Abs(PositionX - other.PositionX) - BodyRadius - other.BodyRadius);
    }

    public bool IsWithinAttackRange(CombatSpatialState other) =>
        EdgeDistanceTo(other) <= AttackRange;

    internal void BeginStep(bool defeated)
    {
        Activity = defeated ? CombatSpatialActivity.Defeated : CombatSpatialActivity.Idle;
        LastDisplacement = 0;
    }

    internal double Approach(
        CombatSpatialState target,
        double maximumDistance,
        double minimumX,
        double maximumX)
    {
        ArgumentNullException.ThrowIfNull(target);
        double direction = DirectionTo(target);
        Facing = direction < 0 ? CombatFacing.Left : CombatFacing.Right;
        if (maximumDistance <= 0 || IsWithinAttackRange(target)) return 0;
        double required = Math.Max(0, EdgeDistanceTo(target) - AttackRange);
        double displacement = direction * Math.Min(maximumDistance, required);
        double before = PositionX;
        PositionX = Math.Clamp(PositionX + displacement, minimumX, maximumX);
        LastDisplacement = PositionX - before;
        if (Math.Abs(LastDisplacement) > double.Epsilon)
            Activity = CombatSpatialActivity.Approaching;
        return LastDisplacement;
    }

    internal double ApplyKnockback(
        double signedDistance,
        double minimumX,
        double maximumX)
    {
        if (!double.IsFinite(signedDistance))
            throw new ArgumentOutOfRangeException(nameof(signedDistance));
        double before = PositionX;
        PositionX = Math.Clamp(PositionX + signedDistance, minimumX, maximumX);
        LastDisplacement = PositionX - before;
        if (Math.Abs(LastDisplacement) > double.Epsilon)
            Activity = CombatSpatialActivity.Knockback;
        return LastDisplacement;
    }

    internal void MarkActivity(CombatSpatialActivity activity) => Activity = activity;

    internal double DirectionAwayFrom(CombatSpatialState source, CombatSide side)
    {
        ArgumentNullException.ThrowIfNull(source);
        double direction = Math.Sign(PositionX - source.PositionX);
        if (direction == 0) direction = side == CombatSide.Party ? -1 : 1;
        return direction;
    }

    private double DirectionTo(CombatSpatialState target)
    {
        double direction = Math.Sign(target.PositionX - PositionX);
        if (direction == 0) direction = Facing == CombatFacing.Right ? 1 : -1;
        return direction;
    }
}
