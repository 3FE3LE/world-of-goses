namespace WorldofGoses.Domain;

/// <summary>
/// Immutable physical consequence naturally expressed by a citizen when a
/// future technique permits it. It has no level and is not a multiplier.
/// </summary>
public enum PhysicalExpression
{
    Fracture,
    Poisoning,
    Paralysis,
    Stunning,
    Bleeding,
    Knockdown,
}

internal static class PhysicalExpressionDisplay
{
    public static string DisplayName(PhysicalExpression expression) => expression switch
    {
        PhysicalExpression.Fracture => "Fracture",
        PhysicalExpression.Poisoning => "Poisoning",
        PhysicalExpression.Paralysis => "Paralysis",
        PhysicalExpression.Stunning => "Stunning",
        PhysicalExpression.Bleeding => "Bleeding",
        PhysicalExpression.Knockdown => "Knockdown",
        _ => expression.ToString(),
    };
}
