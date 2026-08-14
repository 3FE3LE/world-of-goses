namespace WorldofGoses.Domain;

/// <summary>
/// Result of the single deterministic encounter every expedition passes
/// through (docs/systems/expeditions.md). Deliberately has no
/// "total loss"/death tier: a wound or death consequence belongs to VS-3
/// (persistent conditions), not this slice. <see cref="Setback"/> is the
/// worst outcome here and only means "no reward this time, team returns
/// safely".
/// </summary>
public enum ExpeditionEncounterOutcome
{
    Setback = 0,
    PartialSuccess = 1,
    FullSuccess = 2,
}
