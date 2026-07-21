namespace WorldofGoses.Domain;

/// <summary>
/// Identifier for a competency a citizen may develop. Wraps a string so the
/// domain stays free of presentation constants while keeping identifiers
/// strongly typed at call sites.
/// </summary>
public readonly record struct CompetencyId(string Value)
{
    public static CompetencyId Mining { get; } = new("mining");
    public static CompetencyId Farming { get; } = new("farming");
    public static CompetencyId Smithing { get; } = new("smithing");
    public static CompetencyId Construction { get; } = new("construction");
    /// <summary>
    /// Knowledge the hero gains while gathering wood from a Forest.
    /// Reserved for the future slice that lets the player spend hero
    /// experience here; today no worker is ever assigned to a Forest.
    /// </summary>
    public static CompetencyId Foraging { get; } = new("foraging");

    public override string ToString() => Value;
}
