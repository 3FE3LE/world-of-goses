namespace WorldofGoses.Domain;

/// <summary>
/// docs/FIRST_PLAYABLE_LOOP_AUDIT.md §G4: an active expedition is a journey
/// with a shape, not an opaque timer. Each phase spans one quarter of the
/// expedition's duration; the terminal <see cref="Resolved"/> phase is set
/// together with <see cref="ExpeditionStatus"/> leaving Active. There is no
/// separate "Preparing" phase: team selection happens in the UI before
/// <see cref="CityWorld.StartExpedition"/> ever creates the expedition, so
/// the object only exists once the team has already departed.
/// </summary>
public enum ExpeditionPhase
{
    Outbound = 0,
    Encounter = 1,
    Objective = 2,
    Returning = 3,
    Resolved = 4,
}
