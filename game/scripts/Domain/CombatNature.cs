using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Immutable combat manifestation. The physical expression is canonically
/// derived from elemental affinity, so persisted state cannot disagree.
/// Neither value changes channel power numerically.
/// </summary>
public sealed record CombatNature
{
    public CombatNature(ElementalAffinity elementalAffinity)
    {
        if (!Enum.IsDefined(elementalAffinity))
        {
            throw new ArgumentOutOfRangeException(nameof(elementalAffinity));
        }

        ElementalAffinity = elementalAffinity;
        PhysicalExpression = PhysicalExpressionFor(elementalAffinity);
    }

    public ElementalAffinity ElementalAffinity { get; }
    public PhysicalExpression PhysicalExpression { get; }

    public static PhysicalExpression PhysicalExpressionFor(ElementalAffinity affinity) => affinity switch
    {
        ElementalAffinity.Earth => PhysicalExpression.Fracture,
        ElementalAffinity.Aether => PhysicalExpression.Poisoning,
        ElementalAffinity.Water => PhysicalExpression.Paralysis,
        ElementalAffinity.Fire => PhysicalExpression.Stunning,
        ElementalAffinity.Silence => PhysicalExpression.Bleeding,
        ElementalAffinity.Air => PhysicalExpression.Knockdown,
        _ => throw new ArgumentOutOfRangeException(nameof(affinity), affinity, null),
    };
}
