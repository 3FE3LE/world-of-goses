namespace WorldofGoses.Domain;

/// <summary>
/// The player's persisted instruction for what the team should do after the
/// minimal slice's deterministic encounter. This is part of the expedition
/// plan, not a command issued while the team is away.
/// </summary>
public enum ExpeditionRetreatPosture
{
    ContinueAfterSetback = 0,
    RetreatAfterSetback = 1,
}
