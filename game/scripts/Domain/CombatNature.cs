using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Immutable combat manifestation: two independent values.
/// </summary>
/// <remarks>
/// <para>
/// The elemental affinity comes from the onboarding's elemental signal. The
/// physical expression comes from the Kovari Cube, through
/// <see cref="CubeExpression.Derive"/>. They do not determine each other: an
/// Ardhen founder can be Fracture with Fire, Paralysis with Air or Bleeding
/// with Aether.
/// </para>
/// <para>
/// This type previously derived the expression from the affinity, which made
/// the two fields one datum with two names and collapsed the thirty-six
/// affinity × expression combinations into six. Neither value is a multiplier;
/// neither changes channel power numerically.
/// </para>
/// </remarks>
public sealed record CombatNature
{
    public CombatNature(ElementalAffinity elementalAffinity, PhysicalExpression physicalExpression)
    {
        if (!Enum.IsDefined(elementalAffinity))
        {
            throw new ArgumentOutOfRangeException(nameof(elementalAffinity));
        }

        if (!Enum.IsDefined(physicalExpression))
        {
            throw new ArgumentOutOfRangeException(nameof(physicalExpression));
        }

        ElementalAffinity = elementalAffinity;
        PhysicalExpression = physicalExpression;
    }

    public ElementalAffinity ElementalAffinity { get; }
    public PhysicalExpression PhysicalExpression { get; }

    /// <summary>
    /// The canonical construction path for a citizen: affinity from onboarding,
    /// expression derived from the persisted cube. Nothing new is stored, so a
    /// saved citizen can never carry an expression that disagrees with its cube.
    /// </summary>
    public static CombatNature FromCube(ElementalAffinity elementalAffinity, FounderCubeProfile cubeProfile)
    {
        ArgumentNullException.ThrowIfNull(cubeProfile);
        return new CombatNature(elementalAffinity, CubeExpression.Derive(cubeProfile));
    }
}
