using System;

namespace WorldofGoses.Domain;

public sealed record GearSupportProfile
{
    public static GearSupportProfile None { get; } = new(0, 0, 0, 0, 0, 0);

    public GearSupportProfile(
        double body,
        double bond,
        double stability,
        double impulse,
        double domain,
        double reach)
    {
        Validate(body, nameof(body));
        Validate(bond, nameof(bond));
        Validate(stability, nameof(stability));
        Validate(impulse, nameof(impulse));
        Validate(domain, nameof(domain));
        Validate(reach, nameof(reach));
        Body = body;
        Bond = bond;
        Stability = stability;
        Impulse = impulse;
        Domain = domain;
        Reach = reach;
    }

    public double Body { get; }
    public double Bond { get; }
    public double Stability { get; }
    public double Impulse { get; }
    public double Domain { get; }
    public double Reach { get; }

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

    public static GearSupportProfile operator +(GearSupportProfile left, GearSupportProfile right) =>
        new(
            left.Body + right.Body,
            left.Bond + right.Bond,
            left.Stability + right.Stability,
            left.Impulse + right.Impulse,
            left.Domain + right.Domain,
            left.Reach + right.Reach);

    private static void Validate(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(name, value, "Gear support must be finite and non-negative.");
    }
}
