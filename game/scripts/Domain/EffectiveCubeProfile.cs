using System;

namespace WorldofGoses.Domain;

/// <summary>Transient cube faces after the five armor supports are summed.</summary>
public sealed record EffectiveCubeProfile(
    double Body,
    double Bond,
    double Stability,
    double Impulse,
    double Domain,
    double Reach)
{
    public static EffectiveCubeProfile From(FounderCubeProfile cube, GearSupportProfile support)
    {
        ArgumentNullException.ThrowIfNull(cube);
        ArgumentNullException.ThrowIfNull(support);
        return new(
            cube.Body + support.Body,
            cube.Bond + support.Bond,
            cube.Stability + support.Stability,
            cube.Impulse + support.Impulse,
            cube.Domain + support.Domain,
            cube.Reach + support.Reach);
    }

    public double For(CubeFace face) => face switch
    {
        CubeFace.Body => Body,
        CubeFace.Bond => Bond,
        CubeFace.Stability => Stability,
        CubeFace.Impulse => Impulse,
        CubeFace.Domain => Domain,
        CubeFace.Reach => Reach,
        _ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
    };
}
